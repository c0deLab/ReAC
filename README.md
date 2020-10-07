# Rethinking Automation in Construction
Research Team: [Zhihao Fang](https://github.com/SakuraiSatoru), [Yuning Wu](https://github.com/ICE-5), [Ammar Hassonjee](https://github.com/ahassonj), [Ardavan Bidgoli](https://www.ardavan.io/), Michael Hasey, Willa Yang, Yanwen Dong, Prof. [Daniel Cardoso-Llach](https://soa.cmu.edu/daniel-cardoso-llach). 

## An Architectural Framework for Distributed Semi-Autonomous Construction 
### Using Reinforcement Learning to Support Scalable Multi-Drone Construction in Dynamic Environments

#### [Presentation Link](https://docs.google.com/presentation/d/12oNLmjrZbbdthgC6_SSjJvmG4bwvcLlHvFmcy8rDtmo/edit?usp=sharing)

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
- [RL Algorithm Details](#Reinforcement_Learning_Algorithm_Details)
- [Training Model and Results](#Training_Model_and_Results)
- [Drone Hardware and Building Components](#Drone_Hardware_and_Building_Components)
- [Simulation](#Simulation)
- [Next Steps](#Next_Steps)
- [Acknowledgments](#Acknowledgments)

## Status

This project is currently under development in three phases:

1. Code development and RL Algorithm Training Advancement
    - [ ] Training more drone agents to be able to autonomously detect collisions
2. Developing the system to run in Unity
3. Experimenting with the physical fabrication side of crafting drone-compatible building components as well as preparing the drone hardware.

## Reinforcement_Learning_Algorithm_Details

We use a particular kind of reinforcement learning algorithm called  Proximal Policy Optimization, or PPO for short, to learn the policy of how drones can 
approach a target while avoiding collision with other drones. Essentially, the PPO algorithm is a policy gradient-based optimization that uses a neural network 
to resemble the policy. The network is updated using a composite loss that takes into account of generalized advantage estimation, GAE,  in a clipped manner. 
Our neural network architecture uses convolutional layers to combine encodings of different kinds of input, namely lidar, goal position, 
and agent velocity. For better generalization, we also added Gaussian sampling to the output.


### Training_Model_and_Results

We use a two-stage training method to learn the policy in a curriculum learning fashion. In the first stage we trained on 5 agents 
while in the second stage we trained on 10 agents and introduced some threat areas. We use 20 agents for evaluation and the result demonstrates the 
scalability of the algorithm.

## Drone_Hardware_and_Building_Components

We opted to make a build a custom-made drone to the required specs. It relies on a Pixhawk to control its flight, a Raspberry Pi for on the edge computations 
and communication with the centralized computer. It is also equipped with electromagnets to pick and place foam blocks. 
At its final setup, it can use a lidar or depth camera to scan the environment. Our next steps are to figure out the flight control and tracking system. The current proposed
method for this is to use Aruco markers on each drone.

Drones usually don’t stay in a fixed position when flying due to external factors like wind, and this difference between a drone’s simulated location versus its physical
locations can cause building components to be placed in incorrect locations. So to account for these discrepancies, we experimented with different 
brick designs to be used in our pick and placement procedure with key additions, which are shown in the images below. 

### Simulation

Below you can click to see a video simulation demo of the framework in action. The simulation below runs in the Rhino model space and shows a sample bricklaying procedure completed by 10 drone agents.

[![Video thumbnail of multi-drone simulation](https://img.youtube.com/vi/oe1T1j5nVqM/0.jpg)](https://youtu.be/oe1T1j5nVqM)

### Next_Steps

For future steps, we are pushing the current pipeline into a more universal, scalable platform that further integrates simulation with real-time RL training and inference, 
and a seamless communication API for different environments. We are using Unity as our first testbed. With the help of MLagents as middleware, we 
are able to establish a fluent workflow between the environment and PyTorch model. We are also expanding our library of algorithm to prepare for enhanced 
performance in diverse scenarios. Some candidate multi-agent reinforcement learning algorithms include MADDPG, DDPG, Central-Q, Central-V, etc.


### Acknowledgments

We would like to thank [Computational Design Lab](http://code.arc.cmu.edu/) (CoDe Lab) for its generous support. 
We would also like to express our gratitude towards the [Design Fabrication Lab](https://soa.cmu.edu/dfab) (DFab) at the School of Architecture, CMU for their help with fabrication. 

### Citations

Please check back later for citations as we are in the process of writing our paper.


