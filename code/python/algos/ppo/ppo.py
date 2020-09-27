import pathlib
from typing import Tuple, Union, List
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

        self.num_agents = args.num_agents
        self.obs_lidar_dim = args.obs_lidar_dim
        self.obs_lidar_frames = args.obs_lidar_frames

        self.encode_dim = args.encode_dim
        self.gamma = args.gamma
        self.lam = args.lam
        self.lr = args.lr
        self.coeff_entropy = args.coeff_entropy
        self.coeff_v = args.coeff_v
        self.clip_value = args.clip_value
        self.batch_size = args.batch_size

        # model save
        self.model_save_path = args.model_save_path
        self.model_save_interval = args.model_save_interval
        self.model_name = args.policy_type
        # model infervals
        self.update_interval = args.update_interval
        self.inference_interval = args.inference_interval
        # model load
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

        if self.policy_type == "ppo-fc":
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
        buffer = Buffer()
        memory = Memory()
        step = 0
        episode = 0 + self.prev_episode
        collision = 0
        arrival = 0

        self.env.reset()
        mean_reward = 0.
        terminal = False

        while True:
            step += 1

            transition, terminal_1, terminal_2, cols, arrs = self._step()
            terminal = terminal_1 or terminal_2

            buffer.buffer.append(transition)
            collision += cols
            arrival += arrs

            if terminal or (step % self.update_interval == 0):
                if terminal_2:
                    episode += 1

                next_transition, _, _, _, _ = self._step()
                next_value = torch.tensor(next_transition.value).squeeze()

                buffer.map_reduce()

                target, adv = self._get_advantage(reward=buffer.reward, value=buffer.value, next_value=next_value, done=buffer.done)
                memory.add(obs=buffer.obs, action=buffer.action, logprob=buffer.logprob, target=target, adv=adv)

                loss, p_loss, v_loss, entropy = self._update(memory)
                mean_reward = torch.mean(buffer.reward)
                
                self.writer.add_scalar('Reward/Reward vs. episode', mean_reward, episode)
                self.writer.add_scalar('Loss/Loss vs. episode', loss, episode)
                self.writer.add_scalar('Loss/Policy loss vs. episode', p_loss, episode)
                self.writer.add_scalar('Loss/Value loss vs. episode', v_loss, episode)
                self.writer.add_scalar('Loss/Entropy vs. episode', entropy, episode)
                self.writer.add_scalar('Result/Num of collision vs. episode', collision, episode)
                self.writer.add_scalar('Result/Num of arrival vs. episode', arrival, episode)
                print(f"-----> episode {episode}\t collision {collision}\t arrival {arrival}\t reward {mean_reward}\t loss {loss}")

                step = 0
                collision = 0
                arrival = 0
                buffer.empty()
                memory.empty()

            if episode % self.model_save_interval == 0:
                self.save_model(extra=str(episode), prev_episode=episode)
            
            if episode == self.num_episodes:
                break


    def eval(self):
        episode = 0
        self.env.reset()
        reward = 0.
        reward_list = []
        while True:
            transition, _, terminal, _, _ = self._step()
            reward += torch.sum(transition.reward)

            if terminal:
                reward_list.append(reward)
                episode += 1
                reward = 0

            if episode % self.inference_interval == 0:
                print(f">>>>>> episode {episode}\t avg. reward {np.mean(reward_list)}")
                reward_list = []
            
            if episode >= self.num_episodes:
                break


    def save_model(self,
                   save_path: Union[str, pathlib.Path] = None,
                   extra: str = None,
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
            'prev_episode': prev_episode
        }, save_path / name)

    def load_model(self, load_path: Union[str, pathlib.Path]):
        if not pathlib.Path(load_path).exists():
            raise ValueError("model doesn't exist")
        
        checkpoint = torch.load(load_path)
        self.policy.load_state_dict(checkpoint['model_state_dict'])
        # TODO: uncomment if learning rate is properly tuned.
        self.optim.load_state_dict(checkpoint['optimizer_state_dict'])
        self.prev_episode = int(checkpoint['prev_episode'])


    # NOTE: DEBUGGED, 2nd round
    def _step(self, **kwargs) -> Transition:     
        """Step the env with action.

        Raises:
            ValueError: Wrong env mode

        Returns:
            transition: Transition object
            terminal: termination status
        """         
        if self.env_mode == "unity":
            decision_steps, terminal_steps = self.env.get_steps(self.behavior_name)
            # obs
            obs_lidar: np.ndarray = decision_steps.obs[1][:, 2::3]                      # -> N x (obs_lidar_dim * obs_lidar_frames)
            obs_lidar: torch.tensor = torch.from_numpy(obs_lidar).float()
            obs_lidar = self._transform_lidar(obs_lidar=obs_lidar,
                                              num_agents=self.num_agents,
                                              obs_lidar_dim=self.obs_lidar_dim,
                                              obs_lidar_frames=self.obs_lidar_frames)
            obs_other: np.ndarray = decision_steps.obs[2]                               # -> N x obs_other_dim
            obs_other: torch.tensor = torch.from_numpy(obs_other).float()
            obs = [obs_lidar, obs_other]

            # reward
            # TODO: hardcoding
            reward = decision_steps.reward
            collided_agents = np.where(reward == -15)[0].tolist()
            arrived_agents = np.where(reward >= 10)[0].tolist()
            reward: torch.tensor = torch.from_numpy(reward).float()
            
            # done
            # crash terminal
            terminal_1 = len(collided_agents) > 0
            done: list = [True if i in collided_agents else False for i in range(self.num_agents)]
            done: torch.tensor = torch.tensor(done)
            # max_step terminal
            terminal_2 = len(terminal_steps) == self.num_agents
            if terminal_2:
                done: torch.tensor = torch.ones(self.num_agents, dtype=bool)

            value, action, logprob, _ = self._get_clipped_action(obs, ((0., -1.), (1., 1.)), **kwargs)
                    
            transition = Transition(obs=obs, action=action, reward=reward, done=done, logprob=logprob, value=value, **kwargs)
            self.env.set_actions(self.behavior_name, action.numpy())
            self.env.step()
            return transition, terminal_1, terminal_2, len(collided_agents), len(arrived_agents), 
        else:
            raise ValueError("Unsupported environment")

    # NOTE: DEBUGGED
    def _get_clipped_action(self,
                            obs: List[np.ndarray],
                            action_bound: Tuple[Tuple[float, float], Tuple[float, float]],
                            **kwargs) -> Tuple[torch.tensor, ...]:
        """Get *clipped* action by step through policy network. 

        Args:
            obs (List[np.ndarray]): observation
            action_bound (List[List[int]]): clipping bound

        Returns:
            Tuple[torch.tensor, ...]: same as forward output
        """
        value, action, logprob, mean = self.policy(obs, **kwargs)
        value, action, logprob, mean = value.detach(), action.detach(), logprob.detach(), mean.detach()

        min_bound = torch.tensor(action_bound[0]).expand_as(action)
        max_bound = torch.tensor(action_bound[1]).expand_as(action)

        action = torch.where(action < min_bound, min_bound, action)
        action = torch.where(action > max_bound, max_bound, action)

        return value, action, logprob, mean


    # NOTE: DEBUGGED
    def _get_advantage(self, reward, value, next_value, done):
        T, N = reward.shape
        value = torch.cat((value, next_value.unsqueeze(0)), dim=0)
        done = done.float()

        target = torch.zeros(T, N)
        gae = torch.zeros(N)

        for t in range(T - 1, -1, -1):
            delta = reward[t, :] + self.gamma * value[t + 1, :] * (1 - done[t, :]) - value[t, :]
            gae = delta + self.gamma * self.lam * (1 - done[t, :]) * gae
            target[t, :] = gae + value[t, :]
        adv = target - value[:-1, :]
        return target, adv

    def _update(self, memory: Memory) -> Tuple[float, float, float, float]:
        memory.flatten()
        memory.adv = (memory.adv - memory.adv.mean()) / memory.adv.std()

        info_p_loss, info_v_loss, info_entropy = 0., 0., 0.
        info_loss = 0.
        for _ in range(self.num_epochs):
            sampler = BatchSampler(SubsetRandomSampler(list(range(memory.length))),
                                   batch_size=self.batch_size,
                                   drop_last=False)
            for idxs in sampler:
                batch = memory.get_batch(idxs)
                new_value, new_logprob, entropy = self.policy.evaluate_actions(batch.obs, batch.action)

                # surrogates
                ratio = torch.exp(new_logprob - batch.logprob)
                surrogate_1 = ratio * batch.adv
                surrogate_2 = torch.clamp(ratio, 1 - self.clip_value, 1 + self.clip_value) * batch.adv

                # loss
                p_loss = - torch.min(surrogate_1, surrogate_2).mean()
                v_loss = F.mse_loss(new_value, batch.target)
                loss = p_loss + self.coeff_v * v_loss - self.coeff_entropy * entropy

                self.optim.zero_grad()
                loss.backward()
                self.optim.step()

                info_p_loss += p_loss.detach()
                info_v_loss += v_loss.detach()
                info_entropy += entropy.detach()
                info_loss += loss.detach()

        return tuple(map(lambda x: x / (self.num_epochs * len(sampler)), [info_loss, info_p_loss, info_v_loss, info_entropy]))


    @staticmethod
    def _transform_lidar(obs_lidar: torch.tensor, num_agents: int, obs_lidar_dim: int, obs_lidar_frames: int):
        # TODO: questions about view
        obs_lidar = obs_lidar.view(num_agents, obs_lidar_frames, obs_lidar_dim)

        obs_mid = obs_lidar[:, :, 0].unsqueeze(-1)
        obs_r = obs_lidar[:, :, 1::2]
        obs_l = obs_lidar[:, :, 2::2]

        reversed_obs_l = torch.flip(obs_l, dims=[-1])
        return torch.cat((reversed_obs_l, obs_mid, obs_r), dim=-1).view(num_agents, obs_lidar_frames * obs_lidar_dim)