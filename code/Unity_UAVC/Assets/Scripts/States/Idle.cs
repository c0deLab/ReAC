using System;
using UnityEngine;

internal class Idle : IState
{
    private readonly Drone _drone;

    public Idle(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ChargeBattery();
    }

    public void OnEnter()
    {
        _drone.Stop();
        _drone.GetComponent<Animator>().SetBool("isFlying", false);
    }

    public void OnExit()
    {
    }
}