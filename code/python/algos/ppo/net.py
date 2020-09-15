from typing import Tuple, List
import torch
import torch.nn as nn
from torch.nn import functional as F
from torch.autograd import Variable
import numpy as np


class LSTMPolicy(nn.Module):
    def __init__(self,
                 obs_lidar_frames: int,
                 obs_other_dim: int,
                 act_dim: int,
                 encode_dim: int):
        super(LSTMPolicy, self).__init__()

        self.obs_lidar_frames = obs_lidar_frames
        self.act_dim = act_dim
        self.logstd = nn.Parameter(torch.zeros(act_dim))

        # actor layers
        self.a_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        self.a_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        self.a_fc1 = nn.Linear(32, 256)
        self.a_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.a_lstm = nn.LSTMCell(input_size=encode_dim, hidden_size=encode_dim)
        self.a_out = nn.Linear(encode_dim, act_dim)

        # critic layer
        self.c_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        self.c_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        self.c_fc1 = nn.Linear(32, 256)
        self.c_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.c_lstm = nn.LSTMCell(input_size=encode_dim, hidden_size=encode_dim)
        self.c_out = nn.Linear(encode_dim, 1)

    def forward(self,
                obs: Tuple[torch.tensor, torch.tensor],
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
        a_hc = self.a_lstm(a, a_hc)
        mean = torch.tanh(self.a_out(a_hc[0]))

        logstd = self.logstd.expand_as(mean)
        action = torch.normal(mean, torch.exp(logstd))

        # action prob on log scale
        logprob = self.log_normal_density(action, mean, log_std=logstd)

        # value
        v = F.relu(self.c_fea_cv1(obs_lidar))
        v = F.relu(self.c_fea_cv2(v))
        v = v.view(N, -1)
        v = F.relu(self.c_fc1(v))
        v = torch.cat((v, obs_other), dim=-1)
        v = F.relu(self.c_fc2(v))
        c_hc = self.c_lstm(v, c_hc)
        value = self.c_out(v).squeeze()

        return value, action, logprob, mean, a_hc, c_hc

    # https://github.com/Acmece/rl-collision-avoidance/blob/40bf4f22b4270074d549461ea56ca2490b2e5b1c/model/net.py#L72
    def evaluate_actions(self,
                         obs: Tuple[np.ndarray, np.ndarray],
                         action: torch.tensor,
                         **kwargs) -> Tuple[torch.tensor, ...]:
        a_hc, c_hc = kwargs['a_hc'], kwargs['c_hc']
        value, _, _, mean, _, _ = self.forward(obs, a_hc=a_hc, c_hc=c_hc)
        logstd = self.logstd.expand_as(mean)
        pi = torch.tensor(np.pi)
        # evaluate
        logprob = self.log_normal_density(action, mean, logstd)
        dist_entropy = 0.5 + 0.5 * torch.log(2 * pi) + logstd
        dist_entropy = dist_entropy.sum(-1).mean()
        return value, logprob, dist_entropy

    # https://github.com/Acmece/rl-collision-avoidance/blob/40bf4f22b4270074d549461ea56ca2490b2e5b1c/model/utils.py#L90
    # TODO: rewrite log density
    @staticmethod
    def log_normal_density(x: torch.tensor,
                           mean: torch.tensor,
                           log_std: torch.tensor) -> Tuple[torch.tensor]:
        """returns guassian density given x on log scale"""
        std = torch.exp(log_std)
        var = std.pow(2)
        pi = torch.tensor(np.pi)
        log_density = -(x - mean).pow(2) / (2 * var) - 0.5 * \
            torch.log(2 * pi) - log_std
        log_density = log_density.sum(dim=-1, keepdim=True)
        return log_density


class FCPolicy(nn.Module):
    def __init__(self,
                 obs_lidar_frames: int,
                 obs_other_dim: int,
                 act_dim: int,
                 encode_dim: int):
        super(FCPolicy, self).__init__()

        self.obs_lidar_frames = obs_lidar_frames
        self.act_dim = act_dim
        self.logstd = nn.Parameter(torch.zeros(act_dim))

        # actor layers
        self.a_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        self.a_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        self.a_fc1 = nn.Linear(32, 256)
        self.a_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.a_fc3 = nn.Linear(encode_dim, encode_dim)
        self.a_out = nn.Linear(encode_dim, act_dim)

        # critic layer
        self.c_fea_cv1 = nn.Conv1d(in_channels=self.obs_lidar_frames, out_channels=32, kernel_size=5, stride=2, padding=1)
        self.c_fea_cv2 = nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, stride=2, padding=1)
        self.c_fc1 = nn.Linear(32, 256)
        self.c_fc2 = nn.Linear(256 + obs_other_dim, encode_dim)
        self.c_fc3 = nn.Linear(encode_dim, encode_dim)
        self.c_out = nn.Linear(encode_dim, 1)

    def forward(self, 
                obs: Tuple[torch.tensor, torch.tensor],
                **kwargs) -> Tuple[torch.tensor, ...]:
        """
            returns value estimation, action, log_action_prob
        """

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
        a = F.relu(self.a_fc3(a))
        mean = torch.tanh(self.a_out(a))

        logstd = self.logstd.expand_as(mean)
        action = torch.normal(mean, torch.exp(logstd))

        # action prob on log scale
        logprob = self.log_normal_density(action, mean, log_std=logstd)

        # value
        v = F.relu(self.c_fea_cv1(obs_lidar))
        v = F.relu(self.c_fea_cv2(v))
        v = v.view(N, -1)
        v = F.relu(self.c_fc1(v))
        v = torch.cat((v, obs_other), dim=-1)
        v = F.relu(self.c_fc2(v))
        v = F.relu(self.c_fc3(v))
        value = self.c_out(v).squeeze()

        return value, action, logprob, mean, None, None

    # https://github.com/Acmece/rl-collision-avoidance/blob/40bf4f22b4270074d549461ea56ca2490b2e5b1c/model/net.py#L72
    def evaluate_actions(self,
                         obs: Tuple[np.ndarray, np.ndarray],
                         action: torch.tensor,
                         **kwargs) -> Tuple[torch.tensor, ...]:
        value, _, _, mean, _, _ = self.forward(obs)
        logstd = self.logstd.expand_as(mean)
        pi = torch.tensor(np.pi)
        # evaluate
        logprob = self.log_normal_density(action, mean, logstd)
        dist_entropy = 0.5 + 0.5 * torch.log(2 * pi) + logstd
        dist_entropy = dist_entropy.sum(-1).mean()
        return value, logprob, dist_entropy

    # https://github.com/Acmece/rl-collision-avoidance/blob/40bf4f22b4270074d549461ea56ca2490b2e5b1c/model/utils.py#L90
    # TODO: rewrite log density
    @staticmethod
    def log_normal_density(x: torch.tensor,
                           mean: torch.tensor,
                           log_std: torch.tensor) -> Tuple[torch.tensor]:
        """returns guassian density given x on log scale"""
        std = torch.exp(log_std)
        var = std.pow(2)
        pi = torch.tensor(np.pi)
        log_density = -(x - mean).pow(2) / (2 * var) - 0.5 * \
            torch.log(2 * pi) - log_std
        log_density = log_density.sum(dim=-1, keepdim=True)
        return log_density
