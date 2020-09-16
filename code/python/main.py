import pathlib
from utils.args import get_args
from utils.env import make_env
from algos.ppo.ppo import PPO

from torch.utils.tensorboard import SummaryWriter

def guess_path(path: str) -> str:
    raw_path = pathlib.Path(path)
    abs_path = pathlib.Path(__file__).parent.absolute() / raw_path
    cwd_path = pathlib.Path.cwd() / raw_path

    if raw_path.exists():
        return abs_path
    elif abs_path.exists():
        return abs_path
    elif cwd_path.exists():
        return cwd_path
    else:
        raise ValueError("path doesn't exist.")


if __name__ == "__main__":
    args = get_args()
    env, args = make_env(args)

    args.root_dir = pathlib.Path(__file__).parent.absolute()

    # make default folder _log/, _model/, _video/, _plot/ if not exist
    defaults = ['_log', '_model', '_video', '_plot']
    for default in defaults:
        default_path = args.root_dir / default
        if not default_path.exists():
            default_path.mkdir(parents=True, exist_ok=False)


    args.log_save_path = guess_path(args.log_save_path)
    args.model_save_path = guess_path(args.model_save_path)

    writer = SummaryWriter('_log')

    if args.mode == "train":
        model = PPO(env, args, writer)
        model.train()

    elif args.mode == "infer":
        if args.model_load_path is None:
            raise ValueError("No available model for inference. Please use '--model-load-path' flag to specify model.")
        # guess possible path from user input
        model_path = guess_path(args.model_load_path)

        model = PPO(env, args, writer, model_path=model_path)
        model.eval()
        
    elif args.mode == "resume":
        if args.model_load_path is None:
            raise ValueError("No available model to resume training. Please use '--model-load-path' flag to specify model.")
        # guess possible path from user input
        model_path = guess_path(args.model_load_path)

        model = PPO(env, args, writer, model_path=model_path)
        model.train()

    else:
        raise ValueError("mode must be either 'train', 'resume' or 'infer'")
