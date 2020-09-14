import os
import logging
import sys

import numpy as np

import torch
import torch.nn as nn
from torch.optim import Adam
# from collections import deque                                     # TODO: overlap vs. not overlapped

# from model.net import MLPPolicy, CNNPolicy
# from stage_world1 import StageWorld
# from model.ppo import ppo_update_stage1, generate_train_data
# from model.ppo import generate_action
# from model.ppo import transform_buffer

from .model.ppo import PPO
from .model.net import LSTMPolicy
from .model.experience import Experience, Rollout, Buffer


def run(env, policy, action_bound=None, optimizer=None):
    buffer: Buffer = []
    print("done")
