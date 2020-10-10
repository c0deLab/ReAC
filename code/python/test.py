import pathlib
from utils.args import get_args
from utils.env import make_env
from algos.ppo.ppo import PPO

import torch

from torch.utils.tensorboard import SummaryWriter

if __name__ == "__main__":
    args = get_args()
    env, args = make_env(args)

    writer = SummaryWriter('_log')

    step = 0
    while True:
        model = PPO(env, args, writer)
        _, terminal = model._step()
        step += 1
        if terminal > 0:
            print("terminal", terminal)
            print("step", step)






