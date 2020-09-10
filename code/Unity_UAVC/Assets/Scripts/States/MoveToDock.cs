using UnityEngine;

internal class MoveToDock: IState
{
    private readonly Drone _drone;

    public MoveToDock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
    }

    public void OnEnter()
    {
        var pos = _drone.dock.position;
        _drone.GoToPos(new Vector3(pos.x, _drone.transHeight, pos.z));
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}