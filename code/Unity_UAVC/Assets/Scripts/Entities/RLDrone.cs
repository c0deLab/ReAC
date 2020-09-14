using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;

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

    [Range(0.2f, 0.5f)] public float colliderRangeMin = 0.25f;
    [Range(0.2f, 0.5f)] public float colliderRangeMax = 0.40f;
    public float maxTranslateVelocity = 1.0f;
    public float maxRotateVelocity = 1.0f;
    public float rewardReach = 15.0f;
    public float rewardCollide = -15.0f;

    private Vector3 _lastObsPos;
    private bool _collided;

    [Observable] public float ColliderRadius => _collider.radius;
    public float rewardDistScalar = 2.5f;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        // _controller = new PIDController(_rigidbody, 1.5f, 0.0f, 0.1f);
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
                colliderRangeMax * 2) return true;
        }

        return false;
    }


    public override void OnEpisodeBegin()
    {
        var respawnRange = _envConfig.RespawnDistance;
        var targetDist = _envConfig.TargetDistance;
        _rigidbody.velocity = new Vector3();
        _collider.radius = Random.Range(colliderRangeMin, colliderRangeMax);

        bool CheckInit() => Vector3.Distance(transform.position, Vector3.zero) <= _envConfig.RespawnDistance &&
                            !IsCollided(_collider);

        do
        {
            transform.position = new Vector3(Random.Range(-respawnRange, respawnRange), 0,
                Random.Range(-respawnRange, respawnRange));
            Physics.SyncTransforms();
        } while (!CheckInit());

        var targetCollider = _target.gameObject.AddComponent<SphereCollider>();
        targetCollider.radius = colliderRangeMax;
        targetCollider.isTrigger = true;

        bool CheckTargetInit()
        {
            var distOrigin = Vector3.Distance(Vector3.zero, _target.position);
            var distDrone = Vector3.Distance(transform.position, _target.position);
            return (distOrigin <= respawnRange) && (distDrone > targetDist - 1) && (distDrone < targetDist + 1) &&
                   !IsCollided(targetCollider);
        }

        do
        {
            _target.position = new Vector3(Random.Range(-respawnRange, respawnRange), 0,
                Random.Range(-respawnRange, respawnRange));
            Physics.SyncTransforms();
        } while (!CheckTargetInit());

        DestroyImmediate(targetCollider);

        _lastObsPos = transform.position;
        _collided = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CalcAward();
        sensor.AddObservation(Vector3.Distance(_target.position, transform.position));
        sensor.AddObservation(CalcTargetAngle() / 180f);
        sensor.AddObservation(transform.InverseTransformDirection(_rigidbody.velocity).z);
        sensor.AddObservation(_rigidbody.angularVelocity.y);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        _rigidbody.velocity = transform.forward * vectorAction[0] * maxTranslateVelocity;
        _rigidbody.angularVelocity = new Vector3(0, vectorAction[1] * maxRotateVelocity, 0);
    }

    private void CalcAward()
    {
        if (_collided)
        {
            SetReward(rewardCollide);
            Debug.Log($"{name} terminated");
            EndEpisode();
            return;
        }

        var lastDist = Vector3.Distance(_lastObsPos, _target.position);
        var curDist = Vector3.Distance(transform.position, _target.position);
        AddReward(rewardDistScalar * (lastDist - curDist));
        _lastObsPos = transform.position;

        if (Vector3.Distance(transform.position, _target.position) <= _envConfig.ReachTargetTolerance)
            AddReward(rewardReach);

    }

    // public void GoAtVel(Vector3 targetVel)
    // {
    //     if (_lastGoAtVel != targetVel)
    //     {
    //         _controller.ResetError();
    //         _lastGoAtVel = targetVel;
    //     }
    //
    //     _controller.GoAtVel(targetVel);
    // }

    private float CalcTargetAngle()
    {
        var targetLocal = transform.InverseTransformPoint(_target.position);
        return Mathf.Atan2(targetLocal.x, targetLocal.z) * Mathf.Rad2Deg;
    }

    private void OnTriggerEnter(Collider other)
    {
        _collided = true;
    }
}