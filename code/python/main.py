from utils.args import get_args
from utils.env import make_env
from algos.ppo.model.ppo import PPO

from torch.utils.tensorboard import SummaryWriter

if __name__ == "__main__":
    args = get_args()
    env, args = make_env(args)

    writer = SummaryWriter('_log')
    model = PPO(env, args, writer)
    model.train()
