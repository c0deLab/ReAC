using UnityEngine;

internal class MoveToTarget: IState
{
    private readonly Drone _drone;

    public MoveToTarget(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
    }

    public void OnEnter()
    {
        _drone.GoToPos(new Vector3(_drone.target.TargetPosition.x, _drone.transHeight, _drone.target.TargetPosition.z));
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}