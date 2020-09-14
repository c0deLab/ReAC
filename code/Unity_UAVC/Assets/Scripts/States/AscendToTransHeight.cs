using UnityEngine;

internal class AscendToTransHeight: IState
{
    private readonly Drone _drone;

    public AscendToTransHeight(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        _drone.GoToPos(new Vector3(_drone.transform.position.x, Drone.TransHeight, _drone.transform.position.z));
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}