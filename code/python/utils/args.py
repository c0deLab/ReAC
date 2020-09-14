import argparse


def get_args():
    parser = argparse.ArgumentParser(
        "Multi-drone construction with reinforcement learning")
    # Environment
    # parser.add_argument("--scenario-name", type=str, default="adversarial", help="name of the scenario script")
    # parser.add_argument("--map-size", type=int, default=2,
    #                     help="The size of the environment. 1 if normal and 2 otherwise. (default: normal)")
    parser.add_argument("--env-mode", type=str,
                        default="unity", help="mode of environment")
    parser.add_argument("--policy-type", type=str,
                        default="ppo-lstm", help="mode of policy")

    parser.add_argument("--num-episodes", type=int,
                        default=5000, help="(maximum) number of episodes")
    # parser.add_argument("--max-episode-len", type=int,
    #                     default=25, help="maximum episode length")
    parser.add_argument("--max-global-step", type=int,
                        default=5000, help="maximum training global step")
    parser.add_argument("--rollout-size", type=int,
                        default=50, help="rollout size of each agent")
    parser.add_argument("--encode-dim", type=int,
                        default=128, help="encode dimension of LSTM cell")

    # Network config
    parser.add_argument("--obs-lidar-frames", type=int,
                        default=3, help="number of lidar frames to cache for LSTM")

    # ppo stuff
    parser.add_argument("--gamma", type=float,
                        default=0.99, help="discount factor gamma")
    parser.add_argument("--lam", type=float,
                        default=0.95, help="lineage rate lambda")
    parser.add_argument("--lr", type=float,
                        default=1e-3, help="learning rate")
    parser.add_argument("--coeff-entropy", type=float,
                        default=5e-4, help="coefficient of entropy")
    parser.add_argument("--clip-value", type=float,
                        default=0.1, help="PPO clip value")

    # Core training parameters
    # parser.add_argument("--lr-actor", type=float,
    #                     default=1e-4, help="learning rate of actor")
    # parser.add_argument("--lr-critic", type=float,
    #                     default=1e-3, help="learning rate of critic")
    # parser.add_argument("--epsilon", type=float,
    #                     default=0.1, help="epsilon greedy")
    # parser.add_argument("--noise-rate", type=float, default=0.1,
    #                     help="noise rate for sampling from a standard normal distribution ")
    # parser.add_argument("--gamma", type=float,
    #                     default=0.95, help="discount factor")
    # parser.add_argument("--tau", type=float, default=0.01,
    #                     help="parameter for updating the target network")
    # parser.add_argument("--update-rate-maddpg", type=int, default=100,
    #                     help="num of steps between each network update")
    # parser.add_argument("--target-update-rate-maddpg", type=int,
    #                     default=500, help="num of steps between each target update")
    # parser.add_argument("--update-rate-consensus", type=int, default=50,
    #                     help="num of steps between each consensus netwrok update")
    # parser.add_argument("--target-update-rate-consensus", type=int, default=500,
    #                     help="num of steps between each consensus target update")

    # Replay buffer and sample
    # parser.add_argument("--buffer-size", type=int, default=int(5e5),
    #                     help="number of transitions can be stored in buffer")
    # parser.add_argument("--burnin-size", type=int,
    #                     default=int(5e2), help="number of transitions to burnin")
    parser.add_argument("--batch-size", type=int, default=1024,
                        help="number of transitions to sample at each train")
    parser.add_argument("--num-epochs", type=int, default=2,
                        help="number of transitions to sample at each train")

    # Checkpointing
    parser.add_argument("--save-dir", type=str, default="./model",
                        help="directory in which training state and model should be saved")
    parser.add_argument("--save-rate", type=int, default=2000,
                        help="save model once every time this many episodes are completed")
    parser.add_argument("--model-dir", type=str, default="",
                        help="directory in which training state and model are loaded")

    # Evaluate
    # parser.add_argument("--evaluate-num-episodes", type=int,
    #                     default=10, help="number of episodes for evaluating")
    # parser.add_argument("--evaluate-episode-len", type=int,
    #                     default=25, help="length of episodes for evaluating")
    # parser.add_argument("--evaluate", type=bool, default=True,
    #                     help="whether to evaluate the model")
    # parser.add_argument("--evaluate-rate", type=int,
    #                     default=1000, help="how often to evaluate model")
    # parser.add_argument("--render", type=bool, default=False,
    #                     help="Whether to render when evaluating")

    # Device
    parser.add_argument("--device", type=str, default="gpu",
                        help="Whether use GPU. Type 'gpu' or 'cpu'")

    args = parser.parse_args()

    return args
