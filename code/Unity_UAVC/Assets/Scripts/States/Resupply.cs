using UnityEngine;

internal class Resupply : IState
{
    private readonly Drone _drone;

    public Resupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        var brick = _drone.target;
        brick.transform.parent = _drone.transform;
        brick.gameObject.SetActive(true);
    }

    public void OnExit()
    {
    }
}