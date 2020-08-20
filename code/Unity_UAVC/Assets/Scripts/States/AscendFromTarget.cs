using UnityEngine;

internal class AscendFromTarget: IState
{
    private readonly Drone _drone;

    public AscendFromTarget(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(new Vector3(_drone.transform.position.x, _drone.transHeight, _drone.transform.position.z));
    }

    public void OnEnter()
    {
        Debug.Log("ascend from target");
    }

    public void OnExit()
    {
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}