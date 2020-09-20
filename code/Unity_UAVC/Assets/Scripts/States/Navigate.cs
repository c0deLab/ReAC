using UnityEngine;

internal abstract class Navigate: IState
{
    protected readonly Drone _drone;

    protected Navigate(Drone drone)
    {
        _drone = drone;
    }

    public virtual void Tick()
    {
        _drone.ConsumeBattery();
        _drone.StepRLNavi();
    }

    public abstract void OnEnter();

    public virtual void OnExit()
    {
        _drone.ExitRLNavi();
        _drone.Stop();
    }
}