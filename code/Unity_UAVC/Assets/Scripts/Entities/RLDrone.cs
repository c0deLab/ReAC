using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using UnityEngine.Assertions;

public class RLDrone : Agent
{
    private Rigidbody _rigidbody;
    private PIDController _controller;
    private Vector3 _lastGoToPos; // used to compare and reset PIDController
    private Vector3 _lastGoAtVel; // used to compare and reset PIDController
    private Transform _naviTarget;
    private Transform _target;
    private SphereCollider _collider;
    private RLConfig _envConfig;

    private float _tiltSmoothTime = 1.0f;
    private Vector2 _tiltVelocity;
    
    public float ColliderRangeMin = 0.25f;
    public float ColliderRangeMax = 0.40f;

    
    [Observable]
    public float ColliderRadius => _collider.radius;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _controller = new PIDController(_rigidbody, 1.5f, 0.0f, 0.1f);
        _naviTarget = new GameObject("NaviTarget").transform;
        _naviTarget.parent = transform;
        _target = new GameObject("Target").transform;
        _target.parent = transform;
        _collider = GetComponent<SphereCollider>();
        _envConfig = transform.parent.GetComponent<RLConfig>();
    }

    private bool IsCollided(Collider collider)
    {
        foreach (Transform child in transform.parent)
        {
            if (child == transform) continue;
            if (collider.bounds.Intersects(child.GetComponent<Collider>().bounds)) return true;
            var childTarget = child.Find("Target");
            if (childTarget != null && Vector3.Distance(collider.transform.position, childTarget.position) <=
                ColliderRangeMax * 2) return true;
        }
        
        return false;
    }


    public override void OnEpisodeBegin()
    {
        var respawnRange = _envConfig.RespawnDistance;
        var targetDist = _envConfig.TargetDistance;
        _rigidbody.velocity = new Vector3();
        _collider.radius = Random.Range(ColliderRangeMin, ColliderRangeMax);

        bool CheckInit() => Vector3.Distance(transform.position, Vector3.zero) <= _envConfig.RespawnDistance && !IsCollided(_collider);
        do
        {
            transform.position = new Vector3(Random.Range(-respawnRange, respawnRange), 0, Random.Range(-respawnRange, respawnRange));
            Physics.SyncTransforms();
        } while (!CheckInit());

        var targetCollider = _target.gameObject.AddComponent<SphereCollider>();
        targetCollider.radius = ColliderRangeMax;
        targetCollider.isTrigger = true;

        bool CheckTargetInit()
        {
            var distOrigin = Vector3.Distance(Vector3.zero, _target.position);
            var distDrone = Vector3.Distance(transform.position, _target.position);
            return (distOrigin <= respawnRange) && (distDrone > targetDist - 1) && (distDrone < targetDist + 1) && !IsCollided(targetCollider);
        }

        do
        {
            _target.position = new Vector3(Random.Range(-respawnRange, respawnRange), 0, Random.Range(-respawnRange, respawnRange));
            Physics.SyncTransforms();
        } while (!CheckTargetInit());
        DestroyImmediate(targetCollider);

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(_rigidbody.velocity);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        GoAtVel(new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]));
    }

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

    private void HandleTilt()
    {
        var pitch = -_rigidbody.velocity.x * 10;
        var roll = _rigidbody.velocity.z * 10;

        _tiltVelocity.x = Mathf.Lerp(_tiltVelocity.x, pitch, Time.deltaTime * 10);
        _tiltVelocity.y = Mathf.Lerp(_tiltVelocity.y, roll, Time.deltaTime * 10);

        var rot = Quaternion.Euler(_tiltVelocity.y, 0, _tiltVelocity.x);
        _rigidbody.MoveRotation(rot);
    }

}