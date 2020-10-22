from typing import Tuple, List
import math

import numpy as np

import torch
import torch.nn as nn
from torch.nn import functional as F
from torch.distributions import MultivariateNormal

# add static method
class LSTMPolicy(nn.Module):
    def __init__(self,
                 obs_lidar_frames: int,
                 obs_lidar_dim: int,
                 obs_other_dim: int,
                 act_dim: int,
                 encode_dim: int):
        super(LSTMPolicy, self).__init__()

        self.obs_lidar_frames = obs_lidar_frames
        self.act_dim = act_dim
        self.logstd = nn.Parameter(torch.zeros(act_dim))

        # actor layers
        self.a_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        conv_out_dim = calc_conv_dim(obs_lidar_dim, kernel_size=5, stride=2, padding=1)
        self.a_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        conv_out_dim = calc_conv_dim(conv_out_dim, kernel_size=3, stride=2, padding=1)
        self.a_fc1 = nn.Linear(conv_out_dim * 32, 256)
        self.a_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.a_lstm = nn.LSTMCell(input_size=encode_dim, hidden_size=encode_dim)
        self.a_out_1 = nn.Linear(encode_dim, 1)
        self.a_out_2 = nn.Linear(encode_dim, 1)

        # critic layer
        self.c_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        self.c_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        self.c_fc1 = nn.Linear(conv_out_dim * 32, 256)
        self.c_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.c_lstm = nn.LSTMCell(input_size=encode_dim, hidden_size=encode_dim)
        self.c_out = nn.Linear(encode_dim, 1)

    def forward(self,
                obs: List[torch.tensor],
                **kwargs) -> Tuple[torch.tensor, ...]:
        """
            returns value estimation, action, log_action_prob
        """
        a_hc, c_hc = kwargs['a_hc'], kwargs['c_hc']
        obs_lidar = obs[0].view(obs[0].shape[0], self.obs_lidar_frames, -1)
        obs_other = obs[1]
        N = obs_lidar.shape[0]

        # action
        a = F.relu(self.a_fea_cv1(obs_lidar))
        a = F.relu(self.a_fea_cv2(a))
        a = a.view(N, -1)
        a = F.relu(self.a_fc1(a))
        a = torch.cat((a, obs_other), dim=-1)
        a = F.relu(self.a_fc2(a))
        a_hc[0], a_hc[1] = self.a_lstm(a, a_hc)
        mean_1 = torch.sigmoid(self.a_out_1(a_hc[0]))
        mean_2 = torch.tanh(self.a_out_2(a_hc[0]))
        mean = torch.cat((mean_1, mean_2), dim=-1)

        logstd = self.logstd.expand_as(mean)
        action = torch.normal(mean, torch.exp(logstd))

        # action prob on log scale
        logprob = log_normal_density(action, mean, log_std=logstd)

        # value
        v = F.relu(self.c_fea_cv1(obs_lidar))
        v = F.relu(self.c_fea_cv2(v))
        v = v.view(N, -1)
        v = F.relu(self.c_fc1(v))
        v = torch.cat((v, obs_other), dim=-1)
        v = F.relu(self.c_fc2(v))
        c_hc[0], c_hc[1] = self.c_lstm(v, c_hc)
        value = self.c_out(v).squeeze()

        return value, action, logprob, mean

    # https://github.com/Acmece/rl-collision-avoidance/blob/40bf4f22b4270074d549461ea56ca2490b2e5b1c/model/net.py#L72
    def evaluate_actions(self,
                         obs: List[torch.tensor],
                         action: torch.tensor,
                         **kwargs) -> Tuple[torch.tensor, ...]:
        a_hc, c_hc = kwargs['a_hc'], kwargs['c_hc']
        value, _, _, mean = self.forward(obs, a_hc=a_hc, c_hc=c_hc)
        logstd = self.logstd.expand_as(mean)
        pi = torch.tensor(math.pi)
        # evaluate
        logprob = log_normal_density(action, mean, logstd)
        dist_entropy = 0.5 + 0.5 * torch.log(2 * pi) + logstd
        dist_entropy = dist_entropy.sum(-1).mean()
        return value, logprob, dist_entropy



class FCPolicy(nn.Module):
    def __init__(self,
                 obs_lidar_frames: int,
                 obs_lidar_dim: int,
                 obs_other_dim: int,
                 act_dim: int,
                 encode_dim: int):
        super(FCPolicy, self).__init__()

        self.obs_lidar_frames = obs_lidar_frames
        self.act_dim = act_dim
        self.logstd = nn.Parameter(torch.zeros(act_dim))

        # actor layers
        self.a_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        conv_out_dim = calc_conv_dim(obs_lidar_dim, kernel_size=5, stride=2, padding=1)
        self.a_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        conv_out_dim = calc_conv_dim(conv_out_dim, kernel_size=3, stride=2, padding=1)
        self.a_fc1 = nn.Linear(conv_out_dim * 32, 256)
        self.a_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.a_out_1 = nn.Linear(encode_dim, 1)
        self.a_out_2 = nn.Linear(encode_dim, 1)

        # critic layer
        self.c_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        self.c_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        self.c_fc1 = nn.Linear(conv_out_dim * 32, 256)
        self.c_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.c_out = nn.Linear(encode_dim, 1)


    def forward(self,
                obs: List[torch.tensor],
                **kwargs) -> Tuple[torch.tensor, ...]:
        """
        returns value estimation, action, log_action_prob
        """

        obs_lidar = obs[0].view(obs[0].shape[0], self.obs_lidar_frames, -1)
        obs_other = obs[1]

        # action
        a = F.relu(self.a_fea_cv1(obs_lidar))
        a = F.relu(self.a_fea_cv2(a))
        a = a.view(a.shape[0], -1)
        a = F.relu(self.a_fc1(a))
        a = torch.cat((a, obs_other), dim=-1)
        a = F.relu(self.a_fc2(a))
        mean_1 = torch.sigmoid(self.a_out_1(a))
        mean_2 = torch.tanh(self.a_out_2(a))
        mean = torch.cat((mean_1, mean_2), dim=-1)

        # action prob on log scale
        logstd = self.logstd.expand_as(mean)
        std = torch.exp(logstd)
        action = torch.normal(mean, std)
        # action prob on log scale
        logprob = log_normal_density(action, mean, logstd=logstd)

        # value
        v = F.relu(self.c_fea_cv1(obs_lidar))
        v = F.relu(self.c_fea_cv2(v))
        v = v.view(v.shape[0], -1)
        v = F.relu(self.c_fc1(v))
        v = torch.cat((v, obs_other), dim=-1)
        v = F.relu(self.c_fc2(v))
        value = self.c_out(v).squeeze()

        return value, action, logprob, mean

    def evaluate_actions(self,
                         obs: List[torch.tensor],
                         action: torch.tensor,
                         **kwargs) -> Tuple[torch.tensor, ...]:
        value, _, _, mean = self.forward(obs)
        logstd = self.logstd.expand_as(mean)
        pi = torch.tensor(math.pi)
        # evaluate
        logprob = log_normal_density(action, mean=mean, logstd=logstd)
        dist_entropy = 0.5 + 0.5 * torch.log(2 * pi) + logstd
        dist_entropy = dist_entropy.sum(-1).mean()
        
        return value, logprob, dist_entropy
        # TITLE: my (10), original (10, 1)


def log_normal_density(x: torch.tensor,
                        mean: torch.tensor,
                        logstd: torch.tensor) -> Tuple[torch.tensor]:
    """returns guassian density given x on log scale"""
    var = torch.exp(logstd) ** 2 + 1e-10
    dist = MultivariateNormal(mean, torch.diag_embed(var))
    return dist.log_prob(x)



def calc_conv_dim(input_size: int, kernel_size: int, stride: int, padding: int) -> int:
    raw = (input_size - kernel_size + 2 * padding) / stride + 1
    return int(math.floor(raw))
