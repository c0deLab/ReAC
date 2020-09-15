# Multi-Drone Algorithm Library

## Package requirements
```
python=3.8.5
pytorch=1.6.0
torchvision=0.7.0
tensorboard=2.3.0
mlagents=0.19.0
numpy=1.19.1
```

## Usage
Training, inference and resume training is easy through shell, or terminal. To train the model. **Attention**, if you have changed the number of Lidar ray frames collected each step (e.g. 5 frames), please change through the following flag. By default, we use 3 frames.
```
python main.py --mode train --obs-lidar-frames 5
```

### Training
Simply run either of the following commands. If you are using a Unity environment, remember to press the `Play` button.
```
python main.py
python main.py --mode train
```

### Inference
In order to conduct inference, you need to specify a PyTorch model file. An example model file is provided in `_model/ppo_10.pt`. In training process, these models are automatically saved. If not specified otherwise (**see available arguments below**), they are stored in `_model` folder.
```
python main.py --mode infer --model-load-path _model/ppo_10.pt
```

### Resume training
You can resume training by providing a previous PyTorch model file.
```
python main.py --mode resume --model-load-path _model/ppo_10.pt
```

## Logging and inspection
We use Tensorboard for logging the training process. After running the above training command, open up a new terminal window, go back to this directory (through `cd` commands) and type the following command. Please be patient and refresh your browser window if the plots do not show up.
```
tensorboard --logdir=_log
```

You can then go to `http://localhost:6006` to inspect the training. Logging data files (e.g. CSV files) can also be downloaded from Tensorboard.


## Available parameters
Here are all possible options for tuning your usage.
```
--mode", type=str, default="train", help="mode of running, 'train', 'resume' or 'infer'"

--env-mode", type=str, default="unity", help="mode of environment, 'real', 'unity', 'gym' or 'ros'"
--algo", type=str, default="ppo", help="name of RL algorithm, 'ppo', 'maddpg' or 'ddpg'"
--policy-type", type=str, default="ppo-lstm", help="mode of policy"

--num-episodes", type=int, default=5000, help="(maximum) number of episodes"
--rollout-size", type=int, default=128, help="rollout size"
--encode-dim", type=int, default=128, help="encode dimension of LSTM cell"

# Network config
--obs-lidar-frames", type=int, default=3, help="number of lidar frames to cache for LSTM"

# PPO stuff
--gamma", type=float, default=0.99, help="discount factor gamma"
--lam", type=float, default=0.95, help="lineage rate lambda"
--lr", type=float, default=1e-3, help="learning rate"
--coeff-entropy", type=float, default=5e-4, help="coefficient of entropy"
--clip-value", type=float, default=0.1, help="PPO clip value"

# Training
--batch-size", type=int, default=128, help="batch size at training"
--num-epochs", type=int, default=2, help="training epochs"

# Inference
--inference-interval", type=int, default=100, help="inference evaluation interval"

# Checkpointing
--model-save-interval", type=int, default=10, help="model save interval"
--model-save-path", type=str, default="_model", help="path for saving model"
--model-load-path", type=str, default=None, help="path for loading model, model name must be included"

# Logging
--log-save-path", type=str, default="_log", help="path for saving log"

# Device
--device", type=str, default="gpu", help="Whether use GPU, 'gpu' or 'cpu'"
```