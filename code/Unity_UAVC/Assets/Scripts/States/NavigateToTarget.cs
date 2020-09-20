using UnityEngine;

internal class NavigateToTarget : Navigate
{
    public NavigateToTarget(Drone drone) : base(drone)
    {
    }

    public override void OnEnter()
    {
        _drone.EnterRLNavi(new Vector3(_drone.target.TargetPosition.x, Drone.TransHeight,
            _drone.target.TargetPosition.z));
    }
}