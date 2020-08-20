using UnityEngine;

internal class MoveToDock: IState
{
    private readonly Drone _drone;

    public MoveToDock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        var pos = _drone.dock.transform.position;
        _drone.GoToPos(new Vector3(pos.x, _drone.transHeight, pos.z));
    }

    public void OnEnter()
    {
        Debug.Log("move to dock");
    }

    public void OnExit()
    {
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}