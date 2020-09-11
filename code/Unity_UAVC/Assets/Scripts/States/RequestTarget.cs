using UnityEngine;

internal class RequestTarget: IState
{
    private readonly Drone _drone;
    private readonly Manager _manager;

    public RequestTarget(Drone drone, Manager manager)
    {
        _drone = drone;
        _manager = manager;
    }

    public void Tick()
    {
        _manager.AssignTarget(_drone);
    }

    public void OnEnter()
    {
    }

    public void OnExit()
    {
        if (_drone.target != null)
        {
            _drone.GetComponent<Animator>().SetBool("isFlying",true);
        }
    }
}