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
    - (opt)a_hc: actor state    -- float    [ N x encode_dim, N x encode_dim ]
    - (opt)a_hc: critic state   -- float    [ N x encode_dim, N x encode_dim ]
    """
    obs: List[torch.tensor]
    action: torch.tensor
    reward: torch.tensor
    done: torch.tensor
    logprob: torch.tensor
    value: torch.tensor
    a_hc: List[torch.tensor] = None
    c_hc: List[torch.tensor] = None

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
    obs: List[torch.tensor] = None
    action: torch.tensor = None
    logprob: torch.tensor = None
    target: torch.tensor = None
    adv: torch.tensor = None
    a_hc: List[torch.tensor] = None
    c_hc: List[torch.tensor] = None

    @property
    def L(self) -> List[str]:
        return ['obs', 'a_hc', 'c_hc', 'action', 'logprob', 'target', 'adv']

    @property
    def L1(self) -> List[str]:
        return ['action', 'logprob', 'target', 'adv']
    
    @property
    def L2(self) -> List[str]:
        return ['obs', 'a_hc', 'c_hc']
    
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
    
    @property
    def is_empty(self) -> bool:        
        result = True
        for key in self.L1:
            result = result and (getattr(self, key) is None)
        return result
    
    @property
    def is_flat(self) -> bool:
        if (not self.is_empty) and (len(self.target.shape) == 1):
            return True
        else:
            return False


    def extend(self, other_memory):
        """Append another Memory object to the current memory.

        Args:
            other_memory (Memory): other Memory object

        Returns:
            Memory: The memory after append.
        """
        if (not self.is_empty) and (not other_memory.is_empty):
            for key in self.L1:
                setattr(self, key, torch.cat((getattr(self, key), getattr(other_memory, key)), dim=0))
            for key in self.L2:
                try:
                    setattr(self, key, [torch.cat((getattr(self, key)[0], getattr(other_memory, key)[0]), dim=0),
                                        torch.cat((getattr(self, key)[1], getattr(other_memory, key)[1]), dim=0)])
                except TypeError:
                    pass
        elif self.is_empty:
            for key in self.L:
                setattr(self, key, getattr(other_memory, key))
        else:
            pass


    def empty(self):
        """
        Empty the memory
        """
        for key in self.L:
            setattr(self, key, None)
    
    def flatten(self):
        """
        Flatten the memory from T x N x dim to (T * N) x dim
        """
        if (not self.is_empty) and (not self.is_flat):
            T, N = self.target.shape

            for key in self.L1:
                setattr(self, key, getattr(self, key).view(T * N, -1).squeeze())

            for key in self.L2:
                try:
                    setattr(self, key, [getattr(self, key)[0].view(T * N, -1).squeeze(),
                                        getattr(self, key)[1].view(T * N, -1).squeeze()])
                except TypeError:
                    pass


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
            retrieved = Memory()
            for key in self.L1:
                setattr(retrieved, key, getattr(self, key)[idxs].requires_grad_())
            for key in self.L2:
                try:
                    setattr(retrieved, key, [getattr(self, key)[0][idxs].requires_grad_(), getattr(self, key)[1][idxs].requires_grad_()])
                except TypeError:
                    pass
            return retrieved

