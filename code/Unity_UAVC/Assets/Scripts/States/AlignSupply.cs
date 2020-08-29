using UnityEngine;

internal class AlignSupply: IState
{
    private readonly Drone _drone;

    public AlignSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
    }

    public void OnEnter()
    {
        Debug.Log("align supply");
    }

    public void OnExit()
    {
    }
}