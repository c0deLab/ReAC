using UnityEngine;

internal class NavigateToDock: Navigate
{
    public NavigateToDock(Drone drone) : base(drone)
    {
    }

    public override void OnEnter()
    {
        var pos = _drone.dock.position;
        _drone.EnterRLNavi(new Vector3(pos.x, Drone.TransHeight, pos.z));
    }
}