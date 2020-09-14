using UnityEngine;

internal class AscendFromSupply : IState
{
    private readonly Drone _drone;

    public AscendFromSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(new Vector3(_drone.supply.transform.position.x, Drone.TransHeight,
            _drone.supply.transform.position.z));
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
    }

    public void OnExit()
    {
        _drone.Stop();
        _drone.supply.ExitCurrentDrone();
        _drone.supply = null;
    }
}