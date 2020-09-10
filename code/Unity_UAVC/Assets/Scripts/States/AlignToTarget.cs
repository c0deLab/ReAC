using UnityEngine;

internal class AlignToTarget: IState
{
    private readonly Drone _drone;

    public AlignToTarget(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
    }

    public void OnEnter()
    {
        _drone.TurnToRot(_drone.target.TargetRotation.y);
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}