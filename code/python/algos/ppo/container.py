import copy
from typing import List, Tuple
from dataclasses import dataclass
import torch


@dataclass
class Transition:
    """
    A Transition contains the data of ALL agents' experience at ONE timestep. Denote number of agents as N.
    - obs: Observation          -- float    [ N x (lidar_dim * lidar_frames), N x other_dim ]
    - action: Action            -- float    N x act_dim 
    - reward: Reward            -- float    N
    - done: Done flag           -- bool     N
    - logprob: Log probability  -- float    N
    - value: Value              -- float    N
    """
    obs: List[torch.tensor]
    action: torch.tensor
    reward: torch.tensor
    done: torch.tensor
    logprob: torch.tensor
    value: torch.tensor

@dataclass
class Buffer:
    """A Transition contains the data of ALL agents' experience at multiple timesteps. Denote number of agents as N. The length of obs, action, reward, done, logprob, value (B) is largely by user's choice by specifying start and stop in mapreduce.
    - buffer                -- Transition   T
    - obs: Observation          -- float    [ B x N x (lidar_dim * lidar_frames), B x N x other_dim ]
    - action: Action            -- float    B x N x act_dim 
    - reward: Reward            -- float    B x N
    - done: Done flag           -- bool     B x N
    - logprob: Log probability  -- float    B x N
    - value: Value              -- float    B x N
    """
    obs: List[torch.tensor] = None
    action: torch.tensor = None
    reward: torch.tensor = None
    done: torch.tensor = None
    logprob: torch.tensor = None
    value: torch.tensor = None
    L:  Tuple[str] = ('obs', 'action', 'reward', 'done', 'logprob', 'value')
    L1: Tuple[str] = ('action', 'reward', 'done', 'logprob', 'value')
    L2: Tuple[str] = ('obs', )

    def __init__(self):
        self.buffer: List[Transition] = []

    @property
    def length(self) -> int:
        return len(self.buffer)

    def map_reduce(self, start=None, stop=None):
        try:
            for key in self.L1:
                setattr(self, key, getattr(self.buffer[start], key).unsqueeze(0))
            for key in self.L2:
                setattr(self, key, [getattr(self.buffer[start], key)[0].unsqueeze(0),
                                    getattr(self.buffer[start], key)[1].unsqueeze(0)])

            for item in self.buffer[start + 1 : stop]:
                for key in self.L1:
                    setattr(self, key, torch.cat((getattr(self, key), getattr(item, key).unsqueeze(0)), dim=0))
                for key in self.L2:
                    setattr(self, key, [torch.cat((getattr(self, key)[0], getattr(item, key)[0].unsqueeze(0)), dim=0),
                                        torch.cat((getattr(self, key)[1], getattr(item, key)[1].unsqueeze(0)), dim=0)])
        except IndexError:
            print("Make sure the start and stop are valid.")
    
    def empty(self):
        self.buffer = []
        for key in self.L:
            setattr(self, key, None)
    

@dataclass
class Memory:
    """
    A Memory contains the data of ALL agents' experience at MULTIPLE timesteps prepared for policy update. Denote number of agents as N, the number of timestep as T.
    - obs: Observation          -- float    [ T x N x (lidar_dim * lidar_frames), N x other_dim ]
    - action: Action            -- float    T x N x act_dim 
    - logprob: Log probability  -- float    T x N
    - target: Target for update -- float    T x N
    - adv: Advantage for update -- float    T x N
    """
    obs: List[torch.tensor] = None
    action: torch.tensor = None
    logprob: torch.tensor = None
    target: torch.tensor = None
    adv: torch.tensor = None
    is_flat: bool = False
    is_empty: bool = True
    L: Tuple[str] = ('obs', 'action', 'logprob', 'target', 'adv')
    L1: Tuple[str] = ('action', 'logprob', 'target', 'adv')
    L2: Tuple[str] = ('obs', )
    
    
    @property
    def length(self) -> int:
        """Return dimension 0 of (each) attribute as length of memory

        Returns:
            int: number of timesteps
        """
        if not self.is_empty:
            return self.target.shape[0]
        else:
            return 0
    
    def add(self, **kwargs):
        for key in self.L:
            setattr(self, key, kwargs[key])
        self.is_empty = False

    def empty(self):
        """
        Empty the memory
        """
        for key in self.L:
            setattr(self, key, None)
        self.is_flat = False
        self.is_empty = True
    
    def flatten(self):
        """
        Flatten the memory from T x N x dim to (T * N) x dim
        """
        if (not self.is_empty) and (not self.is_flat):
            T, N = self.target.shape

            for key in self.L1:
                setattr(self, key, getattr(self, key).view(T * N, -1).squeeze())
            for key in self.L2:
                setattr(self, key, [getattr(self, key)[0].view(T * N, -1),
                                    getattr(self, key)[1].view(T * N, -1)])
            self.is_flat = True


    def get_batch(self, idxs: List[int]):
        """Retrieve items from a flattened memory

        Args:
            idxs (List[int]): indices to retrieve
        """
        if self.is_empty: 
            return None
        if not self.is_flat: 
            self.flatten()
        
        if len(idxs) > self.length:
            raise IndexError("Too many indices to retrieve.")
        else:
            retrieved = Memory(is_flat=True)
            for key in self.L1:
                setattr(retrieved, key, getattr(self, key)[idxs].requires_grad_())
            for key in self.L2:
                try:
                    setattr(retrieved, key, [getattr(self, key)[0][idxs].requires_grad_(), getattr(self, key)[1][idxs].requires_grad_()])
                except TypeError:
                    pass
            return retrieved

