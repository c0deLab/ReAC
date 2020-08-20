using UnityEngine;

internal class AscendFromDock: IState
{
    private readonly Drone _drone;

    public AscendFromDock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(new Vector3(_drone.dock.transform.position.x, _drone.transHeight, _drone.dock.transform.position.z));
    }

    public void OnEnter()
    {
        Debug.Log("ascend from dock");
    }

    public void OnExit()
    {
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}