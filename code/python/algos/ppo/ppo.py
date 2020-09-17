import pathlib
from typing import Tuple, Union
import torch
from torch.optim import Adam
from torch.autograd import Variable
from torch.nn import functional as F
from torch.utils.data.sampler import BatchSampler, SubsetRandomSampler
import numpy as np


from .net import LSTMPolicy, FCPolicy
from .container import Transition, Buffer, Memory

class PPO(object):
    def __init__(self, env, args, writer, model_path=None):
        self.num_epochs = args.num_epochs
        self.num_episodes = args.num_episodes
        self.rollout_size = args.rollout_size
        self.num_agents = args.num_agents
        self.encode_dim = args.encode_dim
        self.gamma = args.gamma
        self.lam = args.lam
        self.lr = args.lr
        self.coeff_entropy = args.coeff_entropy
        self.clip_value = args.clip_value
        self.batch_size = args.batch_size

        # model save
        self.model_save_path = args.model_save_path
        self.model_save_interval = args.model_save_interval
        self.model_name = args.policy_type
        # model inference
        self.inference_interval = args.inference_interval
        # model load
        self.prev_update = 0
        self.prev_episode = 0
        # model log
        self.log_save_path = args.log_save_path
        # writer
        self.writer = writer
        # policy
        self.policy_type = args.policy_type
        init_dict = {"obs_lidar_frames": args.obs_lidar_frames,
                     "obs_lidar_dim": args.obs_lidar_dim,
                     "obs_other_dim": args.obs_other_dim,
                     "act_dim": args.act_dim,
                     "encode_dim": args.encode_dim}
        if self.policy_type == "ppo-lstm":
            self.policy = LSTMPolicy(**init_dict)
            self.optim = Adam(self.policy.parameters(), lr=self.lr)
        elif self.policy_type == "ppo-fc":
            self.policy = FCPolicy(**init_dict)
            self.optim = Adam(self.policy.parameters(), lr=self.lr)
        else:
            raise ValueError("Policy type not supported.")
        
        if model_path is not None:
            self.load_model(model_path)

        # env
        self.env = env
        self.env_mode = args.env_mode
        if self.env_mode == "unity":
            self.behavior_name = args.behavior_name

  
    def train(self):
        buffer: Buffer = []
        memory = Memory()
        global_update = 0
        global_step = 0
        step = 0
        last_episode = 0

        for episode in range(self.num_episodes):
            self.env.reset()
            mean_reward = 0.
            terminal = False
            if self.policy_type == "ppo-lstm":
                prev_a_hc = [torch.zeros(self.num_agents, self.encode_dim), torch.zeros(self.num_agents, self.encode_dim)]
                prev_c_hc = [torch.zeros(self.num_agents, self.encode_dim), torch.zeros(self.num_agents, self.encode_dim)]
            else:
                prev_a_hc, prev_c_hc = None, None

            while (not terminal) and (step < self.rollout_size):
                global_step += 1
                step += 1
                transition = self._step(a_hc=prev_a_hc, c_hc=prev_c_hc)
                buffer.append(transition)

                # BEST: terminal means at least one agent is done (collided)
                terminal = torch.sum(transition.done) > 0

                if step == self.rollout_size:
                    global_update += 1
                    step = 0

                    next_transition = self._step(a_hc=prev_a_hc, c_hc=prev_c_hc)
                    next_value = next_transition.value

                    obs_arr, action_arr, reward_arr, done_arr, logprob_arr, value_arr, a_hc_arr, c_hc_arr = self._transform_buffer(buffer)
                    target_arr, adv_arr = self._get_advantage(reward_arr, value_arr, next_value, done_arr)
                    tmp_memory = Memory(obs=obs_arr, action=action_arr, logprob=logprob_arr, target=target_arr, adv=adv_arr,
                                        a_hc=a_hc_arr, c_hc=c_hc_arr)
                    
                    memory.extend(tmp_memory)
                    buffer = []

                    loss, _, _, _ = self._update(memory)
                    mean_reward = torch.mean(reward_arr)

                    log_episode = episode + self.prev_episode
                    log_update = global_update + self.prev_update
                    log_endure = episode - last_episode

                    self.writer.add_scalar('Reward/Reward vs. update', mean_reward, log_update)
                    self.writer.add_scalar('Reward/Reward vs. episode', mean_reward, log_episode)
                    self.writer.add_scalar('Loss/Loss vs. update', loss, log_update)
                    self.writer.add_scalar('Loss/Loss vs. episode', loss, log_episode)
                    self.writer.add_scalar('Persistence/Num of Collision vs. update', log_endure, log_update)
                    print(f"-----> update {log_update}\t episode {log_episode}\t collision {log_endure}\t reward {mean_reward}\t loss {loss}")

                    memory.empty()
                    last_episode = episode


            if global_update % self.model_save_interval == 0:
                self.save_model(extra=str(global_update), prev_update=global_update, prev_episode=episode)

    def eval(self):
        episode = 0
        rewards = []
        while True:
            self.env.reset()
            reward = 0.
            step = 0
            terminal = False
            if self.policy_type == "ppo-lstm":
                prev_a_hc = (torch.zeros(self.num_agents, self.encode_dim), torch.zeros(self.num_agents, self.encode_dim))
                prev_c_hc = (torch.zeros(self.num_agents, self.encode_dim), torch.zeros(self.num_agents, self.encode_dim))
            else:
                prev_a_hc, prev_c_hc = None, None

            while not terminal:
                step += 1
                transition = self._step(a_hc=prev_a_hc, c_hc=prev_c_hc)
                prev_a_hc = transition.a_hc
                prev_c_hc = transition.c_hc
                reward += transition.reward
                terminal = torch.sum(transition.done) > 0

            rewards.append(reward)
            episode += 1

            if episode % self.inference_interval == 0:
                print(f">>>>>> avg. reward {np.mean(reward)}\t step {step}\t episode {episode}")
                rewards = []

    def save_model(self,
                   save_path: Union[str, pathlib.Path] = None,
                   extra: str = None,
                   prev_update: int = 0,
                   prev_episode: int = 0):
        if save_path is None:
            save_path = self.model_save_path
        else:
            save_path = pathlib.Path(save_path)

        if not save_path.exists():
            save_path.mkdir(parents=True, exist_ok=False)

        name = self.model_name
        if extra is not None:
            name = self.model_name + "_" + extra + ".pt"

        torch.save({
            'model_state_dict': self.policy.state_dict(),
            'optimizer_state_dict': self.optim.state_dict(),
            'prev_update': prev_update,
            'prev_episode': prev_episode
        }, save_path / name)

    def load_model(self, load_path: Union[str, pathlib.Path]):
        if not pathlib.Path(load_path).exists():
            raise ValueError("model doesn't exist")
        
        checkpoint = torch.load(load_path)
        self.policy.load_state_dict(checkpoint['model_state_dict'])
        # TODO: uncomment if learning rate is properly tuned.
        # self.optim.load_state_dict(checkpoint['optimizer_state_dict'])
        self.prev_update = int(checkpoint['prev_update'])
        self.prev_episode = int(checkpoint['prev_episode'])


    # NOTE: DEBUGGED, need recheck
    def _step(self, **kwargs) -> Transition:     
        """Step the env with action.

        Args:
            prev_a_hc (Tuple[torch.tensor, torch.tensor], optional): actor state (hidden, cell) for LSTMCell. Defaults to None.
            prev_c_hc (Tuple[torch.tensor, torch.tensor], optional): critic state (hidden, cell) for LSTMCell. Defaults to None.

        Raises:
            ValueError: Wrong env mode

        Returns:
            Transition: Transition object with obs, action, reward, done, logprob, value, a_hc, c_hc
        """     
        if self.env_mode == "unity":
            # TODO: see MLAgents release 6 for use of terminal steps
            decision_steps, terminal_steps = self.env.get_steps(self.behavior_name)
            # obs
            obs_lidar: np.ndarray = decision_steps.obs[1][:, 2::3]                      # -> N x (obs_lidar_dim * obs_lidar_frames)
            obs_lidar: torch.tensor = torch.from_numpy(obs_lidar).float()
            obs_other: np.ndarray = decision_steps.obs[2]                               # -> N x obs_other_dim
            obs_other: torch.tensor = torch.from_numpy(obs_other).float()
            obs = (obs_lidar, obs_other)
            # reward
            reward: np.ndarray = decision_steps.reward                                  # -> N,
            reward: torch.tensor = torch.from_numpy(reward).float()
            # done, handle terminated (collided, or exceeds max step) agents
            terminated_agents = terminal_steps.agent_id
            done: list = [True if i in terminated_agents else False for i in range(self.num_agents)]
            done: torch.tensor = torch.tensor(done)

            with torch.no_grad():
                value, action, logprob, _ = self._get_clipped_action(obs, [[0., -1.], [1., 1.]], **kwargs)
                    
            transition = Transition(obs=obs, action=action, reward=reward, done=done, logprob=logprob, value=value, **kwargs)
            # TODO: is the done here necessary? Avoid irregular respawn behavior
            self.env.set_actions(self.behavior_name, action.detach().numpy())
            self.env.step()
            return transition
        else:
            raise ValueError("Unsupported environment")

    # NOTE: DEBUGGED
    def _get_clipped_action(self,
                            obs: Tuple[np.ndarray, np.ndarray],
                            action_bound: Tuple[Tuple[int, int], Tuple[int, int]],
                            **kwargs) -> Tuple[torch.tensor, ...]:
        """Get *clipped* action by step through policy network. 

        Args:
            obs (Tuple[np.ndarray, np.ndarray]): observation
            action_bound (Tuple[int, int]): clipping bound
            a_hc (Tuple[torch.tensor, torch.tensor], optional): actor state (hidden, cell) for LSTMCell. Defaults to None.
            c_hc (Tuple[torch.tensor, torch.tensor], optional): critic state (hidden, cell) for LSTMCell. Defaults to None.

        Returns:
            Tuple[torch.tensor, ...]: same as forward output
        """
        value, action, logprob, mean = self.policy(obs, **kwargs)

        min_bound = torch.nn.Parameter(torch.tensor(action_bound[0])).expand_as(action)
        max_bound = torch.nn.Parameter(torch.tensor(action_bound[1])).expand_as(action)

        action = torch.where(action < min_bound, min_bound, action)
        action = torch.where(action > max_bound, max_bound, action)

        return value, action, logprob, mean

    # NOTE: DEBUGGED
    def _get_advantage(self, reward_arr, value_arr, next_value, done_arr):
        T, N = reward_arr.shape
        value_arr = torch.cat((value_arr, next_value.unsqueeze(0)), dim=0)
        done_arr = done_arr.float()

        target_arr = torch.zeros(T, N)
        gae = torch.zeros(N)

        for t in reversed(range(T)):
            delta = reward_arr[t, :] + self.gamma * value_arr[t + 1, :] * (1 - done_arr[t, :]) - value_arr[t, :]
            gae = delta + self.gamma * self.lam * (1 - done_arr[t, :]) * gae

            target_arr[t, :] = gae + value_arr[t, :]

        adv_arr = target_arr - value_arr[:-1, :]
        return target_arr, adv_arr

    def _update(self, memory: Memory) -> Tuple[float, float, float, float]:
        memory.flatten()

        info_p_loss, info_v_loss, info_entropy = 0., 0., 0.
        info_loss = 0.
        for _ in range(self.num_epochs):
            sampler = BatchSampler(SubsetRandomSampler(list(range(memory.length))),
                                   batch_size=self.batch_size,
                                   drop_last=False)
            for idxs in sampler:
                batch = memory.get_batch(idxs)
                # loss
                new_value, new_logprob, entropy = self.policy.evaluate_actions(batch.obs, batch.action, a_hc=batch.a_hc, c_hc=batch.c_hc)
                ratio = torch.exp(new_logprob - batch.logprob)
                surrogate_1 = ratio * batch.adv
                surrogate_2 = torch.clamp(ratio, 1-self.clip_value, 1+self.clip_value) * batch.adv
                p_loss = - torch.min(surrogate_1, surrogate_2).mean()
                v_loss = F.mse_loss(new_value, batch.target)
                loss = p_loss + 20 * v_loss - self.coeff_entropy * entropy

                self.optim.zero_grad()
                loss.backward()
                self.optim.step()

                info_p_loss += p_loss.detach()
                info_v_loss += v_loss.detach()
                info_entropy += entropy.detach()
                info_loss += loss.detach()

        return tuple(map(lambda x: x / (self.num_epochs * len(sampler)), [info_loss, info_p_loss, info_v_loss, info_entropy]))

    # NOTE: DEBUGGED, double-check if time allowed
    def _transform_buffer(self, buffer: Buffer) -> Tuple[torch.tensor]:
        """Map reduce collected transition buffer.

        Args:
            buffer (Buffer): A list of transitions

        Returns:
            Tuple[torch.tensor]: Grouped categories, obs, action, reward, done, logprob, value, a_hc, c_hc
        """        
        L1 = ['action', 'reward', 'done', 'logprob', 'value']
        L2 = ['obs', ]
        L3 = ['a_hc', 'c_hc']

        if buffer[0].a_hc is not None:
            L2 = L2 + L3

        cpnt_1 = {key: getattr(buffer[0], key).unsqueeze(0) for key in L1}
        cpnt_2 = {key: [getattr(buffer[0], key)[0].unsqueeze(0),
                        getattr(buffer[0], key)[1].unsqueeze(0)] for key in L2}

        if len(buffer) > 1:
            for idx in range(1, len(buffer)):
                cpnt_1 = {key: torch.cat((cpnt_1[key], getattr(buffer[idx], key).unsqueeze(0)), dim=0) for key in L1}
                cpnt_2 = {key: [torch.cat((cpnt_2[key][0], getattr(buffer[idx], key)[0].unsqueeze(0)), dim=0),
                                torch.cat((cpnt_2[key][1], getattr(buffer[idx], key)[1].unsqueeze(0)), dim=0)] for key in L2}
        
        # sanity check of output dim
        assert cpnt_1['action'].shape[1] == self.num_agents

        try:
            return cpnt_2['obs'], cpnt_1['action'], cpnt_1['reward'], cpnt_1['done'], cpnt_1['logprob'], cpnt_1['value'], cpnt_2['a_hc'], cpnt_2['c_hc']
        except KeyError:
            return cpnt_2['obs'], cpnt_1['action'], cpnt_1['reward'], cpnt_1['done'], cpnt_1['logprob'], cpnt_1['value'], None, None
