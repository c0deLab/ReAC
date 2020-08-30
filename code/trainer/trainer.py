from mlagents_envs.environment import UnityEnvironment
import numpy as np

env= UnityEnvironment(file_name = None)
env.reset()
behavior_name = list(env.behavior_specs)[0]
spec = env.behavior_specs[behavior_name]

while True:
    decision_steps, terminal_steps = env.get_steps(behavior_name)
    collider_radius = decision_steps.obs[0]
    lidar_obs = decision_steps.obs[1][:,2::3]
    """
    Details on lidar sensor observation can be found here:
    https://github.com/Unity-Technologies/ml-agents/blob/release_6_docs/docs/Learning-Environment-Design-Agents.md#raycast-observations
    https://github.com/Unity-Technologies/ml-agents/blob/a08b5c4f1aa259f59fc9d186ce3fe18906c69a80/com.unity.ml-agents/Runtime/Sensors/RayPerceptionSensor.cs#L173
    """
    other_obs = decision_steps.obs[2]
    n_agents = len(decision_steps.agent_id) + len(terminal_steps.agent_id)
    action = np.random.uniform(-1,1,(n_agents,spec.action_shape))  # randomly generate actions
    action[:,1] = 0  # set y speed to 0 to make it 2D
    env.set_actions(behavior_name, action)
    env.step()

env.close()