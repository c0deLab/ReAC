using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;

public class RLModule: Agent
{
    private Rigidbody _rigidbody;
    private SphereCollider _collider;
    private Vector3 _targetPos;
    private float[] _vectorAction;
    [Observable] public bool Inference;

    public float maxTranslateVelocity = 1.0f;
    public float maxRotateVelocity = 1.0f;
    
    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _targetPos = Vector3.zero;
    }

    public override void OnEpisodeBegin()
    {
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Vector3.Distance(_targetPos, transform.position));
        sensor.AddObservation(CalcTargetAngle() / 180f);
        sensor.AddObservation(transform.InverseTransformDirection(_rigidbody.velocity).z);
        sensor.AddObservation(_rigidbody.angularVelocity.y);
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (Inference)
        {
            _vectorAction = vectorAction;
        }
    }

    public void Tick()
    {
        if (_vectorAction == null) return;
        Debug.Assert(_vectorAction.Length == 2);
        Debug.Assert(Inference);
        _rigidbody.velocity = transform.forward * Mathf.Clamp01(_vectorAction[0]) * maxTranslateVelocity;
        _rigidbody.angularVelocity = new Vector3(0, Mathf.Clamp(_vectorAction[1], -1.0f, 1.0f) * maxRotateVelocity, 0);
        // _rigidbody.velocity = Vector3.ClampMagnitude(_targetPos- transform.position, 1);
        _vectorAction = null;
    }

    public void SetTarget(Vector3 targetPosition)
    {
        _targetPos = targetPosition;
    }

    private float CalcTargetAngle()
    {
        var targetLocal = transform.InverseTransformPoint(_targetPos);
        return Mathf.Atan2(targetLocal.x, targetLocal.z) * Mathf.Rad2Deg;
    }
    
}
