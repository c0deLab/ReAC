using UnityEngine;

internal class DescendToSupply: IState
{
    private readonly Drone _drone;

    public DescendToSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        _drone.GoToPos(_drone.supply.GetDroneAssignedTransform(_drone).position);
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}