using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;

public class RLDrone : Agent
{
    private Rigidbody _rigidbody;
    private Transform _targets;
    private Transform _target;
    private SphereCollider _collider;
    private RLConfig _envConfig;
    private Vector3 _lastObsPos;
    private bool _collided;
    private bool _arrival;

    private const string TargetsName = "Targets";
    private const float _targetDisplayRadius = 0.1f;

    // [Range(0.2f, 0.5f)] public float colliderRangeMin = 0.25f;
    // [Range(0.2f, 0.5f)] public float colliderRangeMax = 0.40f;
    public float maxTranslateVelocity = 1.0f;
    public float maxRotateVelocity = 1.0f;
    public float safeRotateVelocity = 0.7f;
    public float maxDistanceToTarget = 20f;
    public float rewardReach = 50.0f;
    public float rewardCollide = -15.0f;
    [Observable] public float ColliderRadius => _collider.radius;
    public float rewardDistScalar = 2.5f;
    public float rewardRotScalar = -0.1f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        var targets = GameObject.Find(TargetsName);
        if (!targets)
            targets = new GameObject(TargetsName);
        _targets = targets.transform;
        _target = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        _target.name = $"{name} Target";
        DestroyImmediate(_target.GetComponent<Collider>());
        _target.transform.localScale = Vector3.one * _targetDisplayRadius;
        _target.parent = _targets;
        _collider = GetComponent<SphereCollider>();
        _envConfig = transform.parent.GetComponent<RLConfig>();
    }

    private bool IsCollided(Collider collider)
    {
        foreach (Transform child in transform.parent)
        {
            if (child == transform) continue;
            if (collider.bounds.Intersects(child.GetComponent<Collider>().bounds)) return true;
        }

        return false;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, _target.position) <= _envConfig.ReachTargetTolerance)
        {
            _arrival = true;
        }
    }

    private void RespawnDrone()
    {
        // Debug.Log($"respawning {name}");
        Stop();

        var respawnRange = _envConfig.RespawnDistance;
        _rigidbody.velocity = new Vector3();
        // _collider.radius = Random.Range(colliderRangeMin, colliderRangeMax);

        bool CheckInit() => Vector3.Distance(transform.position, Vector3.zero) <= respawnRange &&
                            !IsCollided(_collider);

        do
        {
            transform.position = new Vector3(Random.Range(-respawnRange, respawnRange), 0,
                Random.Range(-respawnRange, respawnRange));
            Physics.SyncTransforms();
        } while (!CheckInit());

        _lastObsPos = transform.position;
        _collided = false;
        // Debug.Log($"respawned {name}");
    }

    private void RespawnTarget()
    {
        // Debug.Log($"respawning {name}'s target");
        var respawnRange = _envConfig.RespawnDistance;
        var targetDist = _envConfig.TargetDistance;

        bool CheckTargetInit()
        {
            foreach (Transform otherTarget in _targets)
            {
                if (otherTarget != _target &&
                    Vector3.Distance(_target.position, otherTarget.position) <
                    GetComponent<SphereCollider>().radius * 2)
                    return false;
            }

            var distOrigin = Vector3.Distance(Vector3.zero, _target.position);
            var distDrone = Vector3.Distance(transform.position, _target.position);
            return (distOrigin <= respawnRange) && (distDrone > targetDist - 1) && (distDrone < targetDist + 1);
        }

        do
        {
            _target.position = new Vector3(Random.Range(-respawnRange, respawnRange), 0,
                Random.Range(-respawnRange, respawnRange));
            Physics.SyncTransforms();
        } while (!CheckTargetInit());
        // Debug.Log($"respawned {name}'s target");
    }

    public override void OnEpisodeBegin()
    {
        ResetDrone();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CalcAward();
        // sensor.AddObservation(Vector3.Distance(_target.position, transform.position));
        // sensor.AddObservation(CalcTargetAngle() / 180f);
        var relativePosition = CalcRelativePosition();
        sensor.AddObservation(relativePosition.Item1);
        sensor.AddObservation(relativePosition.Item2);
        sensor.AddObservation(transform.InverseTransformDirection(_rigidbody.velocity).z);
        sensor.AddObservation(_rigidbody.angularVelocity.y);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        _rigidbody.velocity = transform.forward * Mathf.Clamp01(vectorAction[0]) * maxTranslateVelocity;
        _rigidbody.angularVelocity = new Vector3(0, Mathf.Clamp(vectorAction[1], -1.0f, 1.0f) * maxRotateVelocity, 0);
    }

    private void CalcAward()
    {
        if (_collided)
        {
            Debug.Log($"{name} terminated");
            SetReward(rewardCollide);
            ResetDrone();
            return;
        }

        
        var lastDist = Vector3.Distance(_lastObsPos, _target.position);
        var curDist = Vector3.Distance(transform.position, _target.position);
        if (curDist < lastDist) {
            // AddReward(3.0f * rewardDistScalar * (lastDist - curDist));
            AddReward(0.3f);
        } else {
            // AddReward(rewardDistScalar * (lastDist - curDist));
            AddReward(-0.1f);
        }
        // // Added by ICE-5
        if (curDist > maxDistanceToTarget) {
            AddReward(rewardCollide);
        }
        
        _lastObsPos = transform.position;

        if (Mathf.Abs(_rigidbody.angularVelocity.y) > safeRotateVelocity)
            AddReward(rewardRotScalar * Mathf.Abs(_rigidbody.angularVelocity.y));
        
        if (_arrival)
        {
            Debug.Log($"{name} arrived");
            _arrival = false;
            AddReward(rewardReach);
            RespawnTarget();
        } else {
            AddReward(-0.05f);
        }
    }

    private float CalcTargetAngle()
    {
        var targetLocal = transform.InverseTransformPoint(_target.position);
        return Mathf.Atan2(targetLocal.x, targetLocal.z) * Mathf.Rad2Deg;
    }

    private (float, float) CalcRelativePosition()
    {
        var targetLocal = transform.InverseTransformPoint(_target.position);
        return (targetLocal.x, targetLocal.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        _collided = true;
    }

    private void Stop()
    {
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    private void ResetDrone()
    {
        RespawnDrone();
        RespawnTarget();
    }
}