﻿internal class BuildBlock : IState
{
    private readonly Drone _drone;

    public BuildBlock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        _drone.target.Build();
        _drone.target = null;
    }

    public void OnExit()
    {
    }
}