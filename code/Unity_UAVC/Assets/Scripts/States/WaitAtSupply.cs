internal class WaitAtSupply: IState
{
    private readonly Drone _drone;

    public WaitAtSupply(Drone drone)
    {
        _drone = drone;
    }

    public void Tick()
    {
        _drone.ConsumeBattery();
    }

    public void OnEnter()
    {
        _drone.Stop();
    }

    public void OnExit()
    {
    }
}