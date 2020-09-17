import argparse

# TODO: group parameters
def get_args():
    parser = argparse.ArgumentParser(
        "Multi-drone construction with reinforcement learning")
    # Environment
    parser.add_argument("--mode", type=str, default="train", help="mode of running, 'train', 'resume' or 'infer'")

    parser.add_argument("--env-mode", type=str, default="unity", help="mode of environment, 'real', 'unity', 'gym' or 'ros'")
    parser.add_argument("--algo", type=str, default="ppo", help="name of RL algorithm, 'ppo', 'maddpg' or 'ddpg'")
    parser.add_argument("--policy-type", type=str, default="ppo-fc", help="mode of policy, 'ppo-fc' or 'ppo-lstm'")

    parser.add_argument("--num-episodes", type=int, default=1000000, help="(maximum) number of episodes")
    parser.add_argument("--rollout-size", type=int, default=128, help="rollout size")
    parser.add_argument("--encode-dim", type=int, default=128, help="encode dimension of LSTM cell")

    # Network config
    parser.add_argument("--obs-lidar-frames", type=int, default=3, help="number of lidar frames to cache for LSTM")

    # PPO stuff
    parser.add_argument("--gamma", type=float, default=0.99, help="discount factor gamma")
    parser.add_argument("--lam", type=float, default=0.95, help="lineage rate lambda")
    parser.add_argument("--lr", type=float, default=1e-5, help="learning rate")
    parser.add_argument("--coeff-entropy", type=float, default=5e-4, help="coefficient of entropy")
    parser.add_argument("--clip-value", type=float, default=0.1, help="PPO clip value")

    # Training
    parser.add_argument("--batch-size", type=int, default=1024, help="batch size at training")
    parser.add_argument("--num-epochs", type=int, default=2, help="training epochs")

    # Inference
    parser.add_argument("--inference-interval", type=int, default=100, help="inference evaluation interval")

    # Checkpointing
    parser.add_argument("--model-save-interval", type=int, default=100, help="model save interval")
    parser.add_argument("--model-save-path", type=str, default="_model", help="path for saving model")
    parser.add_argument("--model-load-path", type=str, default=None, help="path for loading model, model name must be included")

    # Logging
    parser.add_argument("--log-save-path", type=str, default="_log", help="path for saving log")

    # Device
    parser.add_argument("--device", type=str, default="gpu", help="Whether use GPU, 'gpu' or 'cpu'")

    args = parser.parse_args()

    return args
