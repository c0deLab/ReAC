﻿using UnityEngine;

internal class DescendToTarget : IState
{
    private readonly Drone _drone;

    public DescendToTarget(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.GoToPos(_drone.target.TargetPosition);
    }

    public void OnEnter()
    {
        Debug.Log("descend to target");
        // _drone.GetComponent<Rigidbody>().useGravity = true;
    }

    public void OnExit()
    {
        // _drone.GetComponent<Rigidbody>().useGravity = false;
        _drone.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _drone.target.ResetParent();
        _drone.target.gameObject.SetActive(true);
        _drone.target = null;
    }
}