using UnityEngine;

internal class MoveToSupply : IState
{
    private readonly Drone _drone;

    public MoveToSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        // var pos = _drone.supply.transform.position;
        var pos = _drone.supply.GetDroneAssignedTransform(_drone).position;
        _drone.GoToPos(new Vector3(pos.x, _drone.transform.position.y, pos.z));
    }

    public void OnEnter()
    {
        Debug.Log("move to supply");
    }

    public void OnExit()
    {
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}