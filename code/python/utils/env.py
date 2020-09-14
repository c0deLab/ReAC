from mlagents_envs.environment import UnityEnvironment


def make_env(args):
    if args.env_mode == "unity":
        env = UnityEnvironment(file_name=None)
        env.reset()

        args.behavior_name = list(env.behavior_specs)[0]
        spec = env.behavior_specs[args.behavior_name]

        # add to args
        args.act_dim = spec.action_size    # TODO : whether to -1 from act_dim
        args.obs_lidar_dim = int(
            spec.observation_shapes[1][0] / args.obs_lidar_frames / 3)
        args.obs_other_dim = spec.observation_shapes[2][0]

        decision_steps, terminal_steps = env.get_steps(args.behavior_name)
        agents = set([*decision_steps.agent_id, *terminal_steps.agent_id])
        args.num_agents = len(agents)

    elif args.env_mode == "gym":
        pass

    elif args.env_mode == "ros":
        pass

    elif args.env_mode == "real":
        pass

    else:
        raise ValueError("Invalid environment mode.")

    print(f"===> # of agents: {args.num_agents}")
    print(f"===> action dim: {args.act_dim}")
    print(f"===> observation lidar dim: {args.obs_lidar_dim}")
    print(f"===> observation lidar frames: {args.obs_lidar_frames}")
    print(f"===> observation other dim: {args.obs_other_dim}")

    return env, args
