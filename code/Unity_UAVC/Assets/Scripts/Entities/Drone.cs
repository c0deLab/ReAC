using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Drone : MonoBehaviour
{
    [HideInInspector] public Brick target;
    [HideInInspector] public Supply supply;
    [HideInInspector] public Transform dock;
    [HideInInspector] public Manager manager;
    [HideInInspector] public bool running;
    [HideInInspector] public float transHeight = 2.5f;

    private StateMachine _stateMachine;
    private Rigidbody _rigidbody;
    private PIDController _controller;
    private Vector3 _lastGoToPos; // used to compare and reset PIDController
    private Vector3 _lastGoAtVel; // used to compare and reset PIDController
    private Transform _naviTarget;

    private float _posTolerance = 0.01f;
    private float _tiltSmoothTime = 1.0f;
    private Vector2 _tiltVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _stateMachine = new StateMachine();
        _controller = new PIDController(_rigidbody, 1.5f, 0.0f, 0.1f);
        _naviTarget = new GameObject("NaviTarget").transform;
        _naviTarget.parent = transform;
        dock = new GameObject($"Dock {name}").transform;
        dock.position = transform.position;
        dock.rotation = transform.rotation;
        // dock.parent = transform;
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

        _stateMachine.AddTransition(idle, requestTarget, IsRunning());
        _stateMachine.AddTransition(requestTarget, ascendFromDock, AssignedTarget());
        _stateMachine.AddTransition(ascendFromDock, moveToSupply, ReachedTransHeight());
        _stateMachine.AddTransition(requestTarget, moveToDock, WaitForTarget());
        _stateMachine.AddTransition(moveToDock, descendToDock, ReachedDockXZ());
        _stateMachine.AddTransition(descendToDock, idle, ReachedDock());
        _stateMachine.AddTransition(moveToSupply, descendToSupply, ReachedSupplyXZ());
        _stateMachine.AddTransition(descendToSupply, waitAtSupply, ReachedSupplyWait());
        _stateMachine.AddTransition(waitAtSupply, moveToSupply, WaitForSupplyFinished());
        _stateMachine.AddTransition(descendToSupply, ascendFromSupply, ReachedSupply());
        _stateMachine.AddTransition(ascendFromSupply, moveToTarget, ReachedTransHeight());
        _stateMachine.AddTransition(moveToTarget, descendToTarget, ReachedTargetXZ());
        _stateMachine.AddTransition(descendToTarget, ascendFromTarget, ReachedTarget());
        _stateMachine.AddTransition(ascendFromTarget, requestTarget, ReachedTransHeight());
        _stateMachine.SetState(idle);

        Func<bool> IsRunning() => () => running;
        Func<bool> AssignedTarget() => () => target != null;
        Func<bool> WaitForTarget() => () => target == null && Vector3.Distance(transform.position, dock.position)>_posTolerance;
        Func<bool> ReachedSupplyXZ() => () => target != null && supply != null
                                                             && ReachedNaviTargetPos();
        Func<bool> ReachedSupplyWait() => () => target != null && supply != null
                                                                 && ReachedNaviTargetPos()
                                                                 && supply.IsDroneWaiting(this);
        Func<bool> WaitForSupplyFinished() => () => target != null && supply != null
                                                                   && supply.IsDroneCurrent(this);
        Func<bool> ReachedTargetXZ() => () => target != null && ReachedNaviTargetPos();
        Func<bool> ReachedDockXZ() => () => target == null && ReachedNaviTargetPos();
        Func<bool> ReachedTarget() => () => target != null && ReachedNaviTargetPos();
        Func<bool> ReachedSupply() => () => supply != null && ReachedNaviTargetPos();
        Func<bool> ReachedDock() => () => dock != null && ReachedNaviTargetPos();
        Func<bool> ReachedTransHeight() => () => Mathf.Abs(transform.position.y - transHeight) < _posTolerance;
    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        _stateMachine.Tick();
        HandleTilt();
    }

    public void GoToPos(Vector3 targetPos)
    {
        if (_lastGoToPos != targetPos)
        {
            _controller.ResetError();
            _lastGoToPos = targetPos;
        }

        _naviTarget.position = targetPos;
        _controller.GoToPos(targetPos, 1.5f);
    }

    public void GoAtVel(Vector3 targetVel)
    {
        if (_lastGoAtVel != targetVel)
        {
            _controller.ResetError();
            _lastGoAtVel = targetVel;
        }

        _controller.GoAtVel(targetVel);
    }

    public void Stop()
    {
        _rigidbody.velocity = new Vector3();
    }

    private void HandleTilt()
    {
        float pitch = -_rigidbody.velocity.x * 10;
        float roll = _rigidbody.velocity.z * 10;

        _tiltVelocity.x = Mathf.Lerp(_tiltVelocity.x, pitch, Time.deltaTime * 10);
        _tiltVelocity.y = Mathf.Lerp(_tiltVelocity.y, roll, Time.deltaTime * 10);

        // Quaternion rot = Quaternion.Euler(0, 0, tiltVelocity.x);
        Quaternion rot = Quaternion.Euler(_tiltVelocity.y, 0, _tiltVelocity.x);
        _rigidbody.MoveRotation(rot);
    }

    private bool ReachedNaviTargetPos()
    {
        return Vector3.Distance(transform.position, _naviTarget.position) <= _posTolerance;
    }

    public void AddRandomForce()
    {
        float f = 1000.0f;
        _rigidbody.AddForce(new Vector3(Random.Range(-f, f), Random.Range(-f, f), Random.Range(-f, f)),
            ForceMode.Acceleration);
    }
}