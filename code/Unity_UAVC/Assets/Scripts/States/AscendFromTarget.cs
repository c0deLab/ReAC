using UnityEngine;

internal class AscendFromTarget: IState
{
    private readonly Drone _drone;

    public AscendFromTarget(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(new Vector3(_drone.transform.position.x, Drone.TransHeight, _drone.transform.position.z));
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}