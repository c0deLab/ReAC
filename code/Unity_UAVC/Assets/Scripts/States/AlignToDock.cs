internal class AlignToDock : IState
{
    private readonly Drone _drone;

    public AlignToDock(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        _drone.TurnToRot(_drone.dock.rotation.eulerAngles.y);
    }

    public void OnExit()
    {
        _drone.Stop();
    }
}