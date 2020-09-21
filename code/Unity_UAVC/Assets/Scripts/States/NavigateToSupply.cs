using UnityEngine;

internal class NavigateToSupply : Navigate
{
    public NavigateToSupply(Drone drone) : base(drone)
    {
    }

    public override void OnEnter()
    {
        var pos = _drone.supply.GetDroneAssignedTransform(_drone).position;
        _drone.EnterRLNavi(new Vector3(pos.x, _drone.transform.position.y, pos.z));
    }
}