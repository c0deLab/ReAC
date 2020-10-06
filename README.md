# Rethinking Automation in Construction
by: Zhihao Fang, Yuning Wu, Ardavan Bidgoli, Daniel Cardoso-Llach, Ammar Hassonjee

## An Architectural Framework for Distributed Semi-Autonomous Construction 
### Using Reinforcement Learning to Support Scalable Multi-Drone Construction in Dynamic Environments

Autonomous construction has been an active research topic for engineers and designers 
for many years. Meanwhile, technological advancements made in the drone industry are
continuously pushing the drone’s capability boundary. The probability of drones actively
participating in additive construction is large enough to be realized in the near
future. However, currently there is no system that can control a scalable number of drones for
autonomous construction in a dynamic environment. A question that rises from this is how can a scalable
number of drones be coordinated to achieve tasks in real world construction sites as part of an
integrated human and machine construction workflow.

This project aims to develop a decentralized reinforcement learning control framework based on sensory input with a central server 
for dispatching tasks that therefore can support multi-drone coordination for architectural construction, and thus enable more complex human-machine construction processes.
The technical framework consists of a central server for task scheduling, progress monitoring, and drone management. Each individual drone is equipped with an RL-based navigation algorithms for collision avoidance, and macro-actions, which are sequences of action steps geared to accomplishing tasks including building, resupplying, and charging. 
After the server assigns a drone its related construction task, it operates in a decentralized fashion to make the system more scalable. The drone goes to the supply station for resupply, navigates to the designated target position to build, then depending on its remaining battery, it will issue another task request, or go to the charge station for battery charging.


**Table of Contents**

- [Status](#Status)
- [RL Algorithm Details](#ReinforcementLearningAlgorithmDetails)
- [Training Model and Results](#TrainingandResults)
- [Drone Hardware and Building Components](#DroneHardwareandBuildingComponents)
- [Simulation](#Simulation)
- [Next Steps](#Next Steps)
- [Acknowledgments](#Acknowledgments)

## Status

This project is currently under development. 

## Reinforcement Learning Algorithm Details

### Training Model and Results

## Drone Hardware and Building Components

### Simulation

### Next Steps

For future steps, we are pushing the current pipeline into a more universal, scalable platform that further integrates simulation with real-time RL training and inference, 
and a seamless communication API for different environments. We are using Unity as our first testbed. With the help of MLagents as middleware, we 
are able to establish a fluent workflow between the environment and PyTorch model. We are also expanding our library of algorithm to prepare for enhanced 
performance in diverse scenarios. Some candidate multi-agent reinforcement learning algorithms include MADDPG, DDPG, Central-Q, Central-V, etc.


### Acknowledgments

We would like to thank [Computational Design Lab](http://code.arc.cmu.edu/) (CoDe Lab) for its generous support. 
We would also like to express our gratitude towards the [Design Fabrication Lab](https://soa.cmu.edu/dfab) (DFab) at the School of Architecture, CMU for their help with fabrication. 
We would also like to thank Michael Hasey, Willa Yang, and Yanwen Dong for their continued help with the project.


