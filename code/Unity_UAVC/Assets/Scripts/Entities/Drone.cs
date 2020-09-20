using System;
using Unity.MLAgents.Policies;
using UnityEngine;

public class Drone : MonoBehaviour
{
    [HideInInspector] public Brick target;
    [HideInInspector] public Supply supply;
    [HideInInspector] public Manager manager;
    [HideInInspector] public bool running;
    public PositionRotation dock;
    private float _battery = 100f;

    private StateMachine _stateMachine;
    private Rigidbody _rigidbody;
    private PIDController _controller;
    private PositionRotation _naviTarget;
    private RLModule _rlModule;
    private Transform _meshModel;

    public const float TransHeight = 2.5f;
    private const float BatteryConsume = 0.005f;
    private const float BatteryCharge = 0.01f;
    private const float BatteryThreshold = 30f;
    private const float NavTolerance = 0.2f;
    private const float PosTolerance = 0.01f;
    private const float RotTolerance = 0.05f;
    private Vector3 _tiltVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _stateMachine = new StateMachine();
        _controller = new PIDController(_rigidbody);
        dock.position = transform.position;
        dock.rotation = transform.rotation;
        _rlModule = GetComponent<RLModule>();
        _meshModel = GetComponentInChildren<MeshRenderer>().transform.parent;
    }

    private void Start()
    {
        var idle = new Idle(this);
        var moveToSupply = new MoveToSupply(this);
        var moveToSupplyWait = new MoveToSupplyWait(this);
        var moveToTarget = new MoveToTarget(this);
        var moveToDock = new MoveToDock(this);
        var requestTarget = new RequestTarget(this, manager);
        var descendToTarget = new DescendToTarget(this);
        var ascendFromTarget = new AscendFromTarget(this);
        var descendToSupply = new DescendToSupply(this);
        var descendToSupplyWait = new DescendToSupplyWait(this);
        var ascendFromSupply = new AscendFromSupply(this);
        var descendToDock = new DescendToDock(this);
        var ascendToTransHeight = new AscendToTransHeight(this);
        var waitAtSupply = new WaitAtSupply(this);
        var alignToSupply = new AlignToSupply(this);
        var alignToSupplyWait = new AlignToSupplyWait(this);
        var alignToDock = new AlignToDock(this);
        var alignToTarget = new AlignToTarget(this);
        var resupply = new Resupply(this);
        var buildBlock = new BuildBlock(this);
        var navigateToDock = new NavigateToDock(this);
        var navigateToSupply = new NavigateToSupply(this);
        var navigateToSupplyWait = new NavigateToSupplyWait(this);
        var navigateToTarget = new NavigateToTarget(this);

        _stateMachine.AddTransition(idle, requestTarget, IsRunning());
        _stateMachine.AddTransition(requestTarget, ascendToTransHeight, AssignedTarget());
        _stateMachine.AddTransition(requestTarget, navigateToDock, WaitForTarget());
        _stateMachine.AddTransition(navigateToDock, moveToDock, ApproxNaviTargetPos);
        _stateMachine.AddTransition(moveToDock, alignToDock, ReachedDockXZ());
        _stateMachine.AddTransition(alignToDock, descendToDock, ReachedNaviTargetRot);
        _stateMachine.AddTransition(descendToDock, idle, ReachedNaviTargetPos);
        _stateMachine.AddTransition(ascendToTransHeight, navigateToSupply, ReachedNaviPosSupplyIsCurrent());
        _stateMachine.AddTransition(ascendToTransHeight, navigateToSupplyWait, ReachedNaviPosSupplyIsWait());
        _stateMachine.AddTransition(navigateToSupply, moveToSupply, ApproxNaviTargetPos);
        _stateMachine.AddTransition(moveToSupply, alignToSupply, ReachedSupplyXZ());
        _stateMachine.AddTransition(navigateToSupplyWait, moveToSupplyWait, ApproxNaviTargetPos);
        _stateMachine.AddTransition(moveToSupplyWait, navigateToSupply, ReachedNaviPosSupplyIsCurrent());
        _stateMachine.AddTransition(moveToSupplyWait, alignToSupplyWait, ReachedNaviPosSupplyIsWait());
        _stateMachine.AddTransition(alignToSupplyWait, navigateToSupply, ReachedNaviRotSupplyIsCurrent());
        _stateMachine.AddTransition(alignToSupplyWait, descendToSupplyWait, ReachedNaviRotSupplyIsWait());
        _stateMachine.AddTransition(alignToSupply, descendToSupply, ReachedSupplyRot());
        _stateMachine.AddTransition(descendToSupplyWait, waitAtSupply, ReachedSupplyWait());
        _stateMachine.AddTransition(waitAtSupply, navigateToSupply, WaitForSupplyFinished());
        _stateMachine.AddTransition(descendToSupply, resupply, ReachedSupplyPosIsCurrent());
        _stateMachine.AddTransition(resupply, ascendFromSupply, () => true);
        _stateMachine.AddTransition(ascendFromSupply, navigateToTarget, ReachedTransHeight());
        _stateMachine.AddTransition(navigateToTarget, moveToTarget, ApproxNaviTargetPos);
        _stateMachine.AddTransition(moveToTarget, alignToTarget, ReachedTargetXZ());
        _stateMachine.AddTransition(alignToTarget, descendToTarget, ReachedTargetRot());
        _stateMachine.AddTransition(descendToTarget, buildBlock, ReachedTarget());
        _stateMachine.AddTransition(buildBlock, ascendFromTarget, () => true);
        _stateMachine.AddTransition(ascendFromTarget, requestTarget, ReachedTransHeight());
        _stateMachine.AddTransition(ascendFromTarget, navigateToDock, BatteryLow());
        _stateMachine.SetState(idle);

        Func<bool> IsRunning() => () => running && IsCharged();
        Func<bool> AssignedTarget() => () => target != null;

        Func<bool> WaitForTarget() => () =>
            target == null && Vector3.Distance(transform.position, dock.position) > PosTolerance;

        Func<bool> ReachedSupplyXZ() => () => target != null && supply != null
                                                             && ReachedNaviTargetPos();

        Func<bool> ReachedSupplyWait() => () => target != null && supply != null
                                                               && ReachedNaviTargetPos();

        Func<bool> WaitForSupplyFinished() => () => target != null && supply != null
                                                                   && supply.IsDroneCurrent(this);

        Func<bool> ReachedTargetXZ() => () => target != null && ReachedNaviTargetPos();
        Func<bool> ReachedTargetRot() => () => target != null && ReachedNaviTargetRot();
        Func<bool> ReachedDockXZ() => () => target == null && ReachedNaviTargetPos();
        Func<bool> ReachedTarget() => () => target != null && ReachedNaviTargetPos();

        Func<bool> ReachedSupplyPosIsCurrent() => () =>
            supply != null && ReachedNaviTargetPos() && supply.IsDroneCurrent(this) &&
            Vector3.Distance(transform.position, supply.GetDroneAssignedTransform(this).position) <= PosTolerance;

        Func<bool> ReachedSupplyRot() => () => supply != null && ReachedNaviTargetRot();
        Func<bool> ReachedTransHeight() => () => Mathf.Abs(transform.position.y - TransHeight) < PosTolerance;
        Func<bool> BatteryLow() => () => ReachedTransHeight()() && _battery <= BatteryThreshold;
        Func<bool> ReachedNaviPosSupplyIsCurrent() => () => supply.IsDroneCurrent(this) && ReachedNaviTargetPos();
        Func<bool> ReachedNaviPosSupplyIsWait() => () => supply.IsDroneWaiting(this) && ReachedNaviTargetPos();
        Func<bool> ReachedNaviRotSupplyIsCurrent() => () => supply.IsDroneCurrent(this) && ReachedNaviTargetRot();
        Func<bool> ReachedNaviRotSupplyIsWait() => () => supply.IsDroneWaiting(this) && ReachedNaviTargetRot();

        bool ApproxNaviTargetPos() => Vector3.Distance(transform.position, _naviTarget.position) <= NavTolerance;
        bool ReachedNaviTargetPos() => Vector3.Distance(transform.position, _naviTarget.position) <= PosTolerance;

        bool ReachedNaviTargetRot() =>
            Mathf.Abs(transform.eulerAngles.y - _naviTarget.rotation.eulerAngles.y) <= RotTolerance;
    }


    private void FixedUpdate()
    {
        _controller.Tick();
        _stateMachine.Tick();
        HandleTilt();
    }

    public void GoToPos(Vector3 targetPos)
    {
        _naviTarget.position = targetPos;
        _controller.GoToPos(targetPos, 1.5f);
    }

    public void TurnToRot(float targetRot)
    {
        _naviTarget.rotation = Quaternion.Euler(0, targetRot, 0);
        _controller.TurnToRot(targetRot, 0.6f);
    }

    public void Stop()
    {
        _controller.Stop();
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void ChargeBattery()
    {
        _battery = Mathf.Min(_battery + BatteryCharge, 100);
    }

    public void ConsumeBattery()
    {
        _battery = Mathf.Max(0, _battery - BatteryConsume);
    }

    private bool IsCharged()
    {
        return _battery > 99;
    }

    private void HandleTilt()
    {
        var pitch = -_rigidbody.velocity.x * 15;
        var roll = _rigidbody.velocity.z * 15;

        _tiltVelocity.x = Mathf.Lerp(_tiltVelocity.x, pitch, Time.fixedDeltaTime * 20);
        _tiltVelocity.z = Mathf.Lerp(_tiltVelocity.z, roll, Time.fixedDeltaTime * 20);

        Debug.Log(_meshModel.eulerAngles);
        _meshModel.eulerAngles = new Vector3(_tiltVelocity.magnitude, _meshModel.eulerAngles.y, 0);
    }

    public void EnterRLNavi(Vector3 targetPosition)
    {
        _naviTarget.position = targetPosition;
        _rlModule.Inference = true;
        _rlModule.SetTarget(targetPosition);
    }

    public void StepRLNavi()
    {
        Debug.Assert(_rlModule.Inference);
        _rlModule.Tick();
    }

    public void ExitRLNavi()
    {
        _rlModule.Inference = false;
    }

    public struct PositionRotation
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}