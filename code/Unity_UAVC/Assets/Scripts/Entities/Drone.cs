using System;
using UnityEngine;

public class Drone : MonoBehaviour
{
    [HideInInspector] public Brick target;
    [HideInInspector] public Supply supply;
    [HideInInspector] public Manager manager;
    [HideInInspector] public bool running;
    [HideInInspector] public float transHeight = 2.5f;
    public PositionRotation dock;

    private StateMachine _stateMachine;
    private Rigidbody _rigidbody;
    private PIDController _controller;
    private PositionRotation _naviTarget;

    private float _posTolerance = 0.01f;
    private float _rotTolerance = 0.05f;
    private float _tiltSmoothTime = 1.0f;
    private Vector3 _tiltVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _stateMachine = new StateMachine();
        _controller = new PIDController(_rigidbody);
        dock.position = transform.position;
        dock.rotation = transform.rotation;
    }

    private void Start()
    {
        var idle = new Idle(this);
        var moveToSupply = new MoveToSupply(this);
        var moveToTarget = new MoveToTarget(this);
        var moveToDock = new MoveToDock(this);
        var requestTarget = new RequestTarget(this, manager);
        var descendToTarget = new DescendToTarget(this);
        var ascendFromTarget = new AscendFromTarget(this);
        var descendToSupply = new DescendToSupply(this);
        var ascendFromSupply = new AscendFromSupply(this);
        var descendToDock = new DescendToDock(this);
        var ascendFromDock = new AscendFromDock(this);
        var waitAtSupply = new WaitAtSupply(this);
        var alignToSupply = new AlignToSupply(this);
        var alignToTarget = new AlignToTarget(this);

        _stateMachine.AddTransition(idle, requestTarget, IsRunning());
        _stateMachine.AddTransition(requestTarget, ascendFromDock, AssignedTarget());
        _stateMachine.AddTransition(ascendFromDock, moveToSupply, ReachedTransHeight());
        _stateMachine.AddTransition(requestTarget, moveToDock, WaitForTarget());
        _stateMachine.AddTransition(moveToDock, descendToDock, ReachedDockXZ());
        _stateMachine.AddTransition(descendToDock, idle, ReachedDock());
        _stateMachine.AddTransition(moveToSupply, alignToSupply, ReachedSupplyXZ());
        _stateMachine.AddTransition(alignToSupply, descendToSupply, ReachedSupplyRot());
        _stateMachine.AddTransition(descendToSupply, waitAtSupply, ReachedSupplyWait());
        _stateMachine.AddTransition(waitAtSupply, moveToSupply, WaitForSupplyFinished());
        _stateMachine.AddTransition(descendToSupply, ascendFromSupply, ReachedSupplyPos());
        _stateMachine.AddTransition(ascendFromSupply, moveToTarget, ReachedTransHeight());
        _stateMachine.AddTransition(moveToTarget, alignToTarget, ReachedTargetXZ());
        _stateMachine.AddTransition(alignToTarget, descendToTarget, ReachedTargetRot());
        _stateMachine.AddTransition(descendToTarget, ascendFromTarget, ReachedTarget());
        _stateMachine.AddTransition(ascendFromTarget, requestTarget, ReachedTransHeight());
        _stateMachine.SetState(idle);

        Func<bool> IsRunning() => () => running;
        Func<bool> AssignedTarget() => () => target != null;

        Func<bool> WaitForTarget() => () =>
            target == null && Vector3.Distance(transform.position, dock.position) > _posTolerance;

        Func<bool> ReachedSupplyXZ() => () => target != null && supply != null
                                                             && ReachedNaviTargetPos();

        Func<bool> ReachedSupplyWait() => () => target != null && supply != null
                                                               && ReachedNaviTargetPos()
                                                               && supply.IsDroneWaiting(this);

        Func<bool> WaitForSupplyFinished() => () => target != null && supply != null
                                                                   && supply.IsDroneCurrent(this);

        Func<bool> ReachedTargetXZ() => () => target != null && ReachedNaviTargetPos();
        Func<bool> ReachedTargetRot() => () => target != null && ReachedNaviTargetRot();
        Func<bool> ReachedDockXZ() => () => target == null && ReachedNaviTargetPos();
        Func<bool> ReachedTarget() => () => target != null && ReachedNaviTargetPos();
        Func<bool> ReachedSupplyPos() => () => supply != null && ReachedNaviTargetPos();
        Func<bool> ReachedSupplyRot() => () => supply != null && ReachedNaviTargetRot();
        Func<bool> ReachedDock() => () => ReachedNaviTargetPos();
        Func<bool> ReachedTransHeight() => () => Mathf.Abs(transform.position.y - transHeight) < _posTolerance;
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
        _rigidbody.velocity = new Vector3();
    }

    private void HandleTilt()
    {
        var pitch = -_rigidbody.velocity.x * 10;
        var roll = _rigidbody.velocity.z * 10;

        _tiltVelocity.x = Mathf.Lerp(_tiltVelocity.x, pitch, Time.deltaTime * 10);
        _tiltVelocity.z = Mathf.Lerp(_tiltVelocity.z, roll, Time.deltaTime * 10);

        var convert = transform.InverseTransformDirection(_tiltVelocity);
        var origin = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up);
        var rot = Quaternion.Euler(convert.z, 0, convert.x);
        transform.rotation = origin * rot;
    }

    private bool ReachedNaviTargetPos()
    {
        return Vector3.Distance(transform.position, _naviTarget.position) <= _posTolerance;
    }

    private bool ReachedNaviTargetRot()
    {
        return Mathf.Abs(transform.eulerAngles.y - _naviTarget.rotation.eulerAngles.y) <= _rotTolerance;
    }

    public struct PositionRotation
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}