using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine.Assertions;

public class RLDrone : Agent
{
    private Rigidbody _rigidbody;
    private PIDController _controller;
    private Vector3 _lastGoToPos; // used to compare and reset PIDController
    private Vector3 _lastGoAtVel; // used to compare and reset PIDController
    private Transform _naviTarget;
    private Transform dock;

    private float _posTolerance = 0.01f;
    private float _tiltSmoothTime = 1.0f;
    private Vector2 _tiltVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _controller = new PIDController(_rigidbody, 1.5f, 0.0f, 0.1f);
        _naviTarget = new GameObject("NaviTarget").transform;
        _naviTarget.parent = transform;
        dock = new GameObject($"Dock {name}").transform;
        dock.position = transform.position;
        dock.rotation = transform.rotation;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = dock.position;
        transform.rotation = dock.rotation;
        _rigidbody.velocity = new Vector3();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(_rigidbody.velocity);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // _rigidbody.velocity = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        GoAtVel(new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]));
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
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