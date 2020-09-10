using UnityEngine;

internal class AscendFromSupply: IState
{
    private readonly Drone _drone;

    public AscendFromSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(new Vector3(_drone.supply.transform.position.x, _drone.transHeight, _drone.supply.transform.position.z));
    }

    public void OnEnter()
    {
        var brick = _drone.target;
        brick.transform.parent = _drone.transform;
        brick.gameObject.SetActive(true);
    }

    public void OnExit()
    {
        _drone.Stop();
        _drone.supply.ExitCurrentDrone();
        _drone.supply = null;
    }
}