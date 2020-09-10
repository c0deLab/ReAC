using UnityEngine;

internal class AscendFromDock: IState
{
    private readonly Drone _drone;

    public AscendFromDock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
    }

    public void OnEnter()
    {
        _drone.GoToPos(new Vector3(_drone.dock.position.x, _drone.transHeight, _drone.dock.position.z));
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}