from typing import NamedTuple, List, Tuple
from dataclasses import dataclass
import torch


@dataclass
class Transition:
    """
    A Transition contains the data of *all* agents' transitions. Denote number of agents as N.
    - obs: Observation          -- float    [ N x (lidar_dim * lidar_frames), N x other_dim ]
    - action: Action            -- float    N x act_dim 
    - reward: Reward            -- float    N x 1
    - done: Done flag           -- bool     N x 1
    - logprob: Log probability  -- float    N x 1
    - value: Value              -- float    N x 1
    - (opt)a_hc: actor hidden   -- float    [ N x encode_dim, N x encode_dim ]
    - (opt)a_hc: critic hidden  -- float    [ N x encode_dim, N x encode_dim ]
    """
    obs: Tuple[torch.tensor, torch.tensor]
    action: torch.tensor
    reward: torch.tensor
    done: torch.tensor
    logprob: torch.tensor
    value: torch.tensor
    a_hc: Tuple[torch.tensor, torch.tensor] = None
    c_hc: Tuple[torch.tensor, torch.tensor] = None


# A Buffer is an unordered list of transitions
Buffer = List[Transition]
