from typing import NamedTuple, List, Tuple
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
    - (opt)a_hc: actor state    -- float    [ N x encode_dim, N x encode_dim ]
    - (opt)a_hc: critic state   -- float    [ N x encode_dim, N x encode_dim ]
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

@dataclass
class Memory:
    """
    A Memory contains the data of ALL agents' experience at MULTIPLE timesteps prepared for policy update. Denote number of agents as N, the number of timestep as T.
    - obs: Observation          -- float    [ T x N x (lidar_dim * lidar_frames), N x other_dim ]
    - action: Action            -- float    T x N x act_dim 
    - logprob: Log probability  -- float    T x N
    - target: Target for update -- float    T x N
    - adv: Advantage for update -- float    T x N
    - (opt)a_hc: actor state    -- float    [ T x N x encode_dim, T x N x encode_dim ]
    - (opt)a_hc: critic state   -- float    [ T x N x encode_dim, T x N x encode_dim ]
    """
    obs: Tuple[torch.tensor, torch.tensor] = None
    action: torch.tensor = None
    logprob: torch.tensor = None
    target: torch.tensor = None
    adv: torch.tensor = None
    a_hc: Tuple[torch.tensor, torch.tensor] = None
    c_hc: Tuple[torch.tensor, torch.tensor] = None

    def extend(self, other_memory):
        """Append another Memory object to the current memory.

        Args:
            other_memory (Memory): other Memory object

        Returns:
            Memory: The memory after append.
        """        
        
        L1 = ['action', 'logprob', 'target', 'adv']
        L2 = ['obs', 'a_hc', 'c_hc']

        for key in L1:
            if getattr(self, key) is not None and getattr(other_memory, key) is not None:
                setattr(self, key, torch.cat((getattr(self, key), getattr(other_memory, key)), dim=0))
            elif getattr(self, key) is None:
                setattr(self, key, getattr(other_memory, key))
            else:
                pass

        for key in L2:
            if getattr(self, key) is not None and getattr(other_memory, key) is not None:
                setattr(self, key, (torch.cat((getattr(self, key)[0], getattr(other_memory, key)[0]), dim=0),
                                    torch.cat((getattr(self, key)[1], getattr(other_memory, key)[1]), dim=0)))
            elif getattr(self, key) is None:
                setattr(self, key, getattr(other_memory, key))
            else:
                pass


    @property
    def num_timesteps(self) -> int:
        """Return number of timestep of this memory

        Returns:
            int: number of timesteps
        """
        if self.target is not None:
            return self.target.shape[0]
        else:
            return 0

    def empty(self):
        """Empty the memory

        Returns:
            Memory: an empty memory
        """
        L = ['action', 'logprob', 'value', 'target', 'adv', 'obs', 'a_hc', 'c_hc']
        for key in L:
            setattr(self, key, None)
