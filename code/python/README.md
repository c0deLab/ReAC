# Multi-Drone Algorithm Library

Package requirements 
```
python=3.8.5
pytorch=1.6.0
torchvision=0.7.0
mlagents=0.19.0
numpy=1.19.1
```

Easy access through terminal
```
python main.py
```

Available arguments to pass in terminal
```
"--env-mode", type=str, default="unity", help="mode of environment"
"--policy-type", type=str, default="ppo-lstm", help="mode of policy"

"--num-episodes", type=int, default=5000, help="(maximum) number of episodes"
"--max-global-step", type=int, default=5000, help="maximum training global step"
"--rollout-size", type=int, default=50, help="rollout size of each agent"
"--encode-dim", type=int, default=128, help="encode dimension of LSTM cell"

# External env config
"--obs-lidar-frames", type=int, default=3, help="number of lidar frames to cache for LSTM"

# ppo stuff
"--gamma", type=float, default=0.99, help="discount factor gamma"
"--lam", type=float, default=0.95, help="lineage rate lambda"
"--lr", type=float, default=1e-3, help="learning rate"
"--coeff-entropy", type=float, default=5e-4, help="coefficient of entropy"
"--clip-value", type=float, default=0.1, help="PPO clip value"

# Training specs
"--batch-size", type=int, default=1024, help="number of transitions to sample at each train"
"--num-epochs", type=int, default=2, help="number of transitions to sample at each train"

# Checkpointing
"--save-dir", type=str, default="./model", help="directory in which training state and model should be saved"
"--save-rate", type=int, default=2000, help="save model once every time this many episodes are completed"
"--model-dir", type=str, default="", help="directory in which training state and model are loaded"

# Device
"--device", type=str, default="cpu", help="Whether use GPU. Currently not available with GPU.
```

Example
* Set lidar frames (by default 3 frames) to 4 frames.
    ```
    python main.py --obs-lidar-frames 4
    ```