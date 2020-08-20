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
        _drone.GoToPos(_drone.supply.GetDroneAssignedTransform(_drone).position);
    }

    public void OnEnter()
    {
        Debug.Log("descend to supply");
    }

    public void OnExit()
    {
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}