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
        Debug.Log("ascend from supply");
    }

    public void OnExit()
    {
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _drone.supply.ExitCurrentDrone();
        _drone.supply = null;
    }
}