internal class AlignToSupply : IState
{
    private readonly Drone _drone;

    public AlignToSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        _drone.TurnToRot(_drone.supply.GetDroneAssignedTransform(_drone).rotation.eulerAngles.y);
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}