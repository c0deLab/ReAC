using UnityEngine;

internal class DescendToDock: IState
{
    private readonly Drone _drone;

    public DescendToDock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(_drone.dock.transform.position);
    }

    public void OnEnter()
    {
        Debug.Log("descend to dock");
    }

    public void OnExit()
    {
        // _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}