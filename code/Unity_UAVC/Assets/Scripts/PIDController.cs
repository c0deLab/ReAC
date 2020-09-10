using System;
using UnityEngine;

public class PIDController
{
    private Rigidbody rb;
    private PID _posPID;
    private PID _rotPID;

    // private Vector3 totalPosError;
    // private Vector3 lastPosError;
    // private Vector3 totalVelError;
    // private Vector3 lastVelError;
    // private Vector3 totalRotError;
    // private Vector3 lastRotError;
    private Vector3 totalError;
    private Vector3 lastError;

    private PIDParam _pidParam;
    private EventHandler<PIDParam> _pidHandler;


    private class PIDParam : EventArgs
    {
        public Vector3 target;
        public float maxVal;

        public PIDParam(Vector3 target, float maxVal)
        {
            this.target = target;
            this.maxVal = maxVal;
        }
    }
    
    internal struct PID
    {
        public float kp;
        public float ki;
        public float kd;

        public PID(float kp, float ki, float kd)
        {
            this.kp = kp;
            this.ki = ki;
            this.kd = kd;
        }
    }
    

    public PIDController(Rigidbody rb)
    {
        this.rb = rb;
        _posPID = new PID(1.5f, 0.0f,0.1f);
        _rotPID = new PID(0.08f, 0.0f,0.005f);
    }

    public void Tick()
    {
        _pidHandler?.Invoke(this, _pidParam);
    }

    public void GoToPos(Vector3 targetPos, float maxSpeed)
    {
        ResetError();
        _pidParam = new PIDParam(targetPos, maxSpeed);
        _pidHandler = GoToPosUpdate;
    }

    private void GoToPosUpdate(object sender, PIDParam param)
    {
        var error = param.target - rb.position;
        var targetVel = PIDUpdate(error, param.maxVal, _posPID, ref lastError, ref totalError);
        var force = (targetVel - rb.velocity) / Time.fixedDeltaTime;
        var gravity = rb.useGravity ? (Physics.gravity * -1) : new Vector3( 0, 0, 0 );
        rb.AddForce( force + gravity, ForceMode.Acceleration);
    }

    private void GoAtVelUpdate(Vector3 targetVel)
    {
        var error = targetVel - rb.velocity;
        var force = PIDUpdate(error, Mathf.Infinity, _posPID, ref lastError, ref totalError);
        var gravity = rb.useGravity ? (Physics.gravity * -1) : new Vector3( 0, 0, 0 );
        rb.AddForce( force + gravity, ForceMode.Acceleration);
    }

    public void TurnToRot(float targetRotY, float maxSpeed)
    {
        ResetError();
        _pidParam = new PIDParam(new Vector3(0, targetRotY, 0), maxSpeed);
        _pidHandler = TurnToRotUpdate;
    }

    private void TurnToRotUpdate(object sender, PIDParam param)
    {
        var error = param.target.y - rb.transform.rotation.eulerAngles.y;
        if (Mathf.Abs(error) > 360 - Mathf.Abs(error))
        {
            if (error > 0)
                error = Mathf.Abs(error) - 360;
            else
                error = 360 - Mathf.Abs(error);
        }

        var vel = PIDUpdate(new Vector3(0, error, 0), param.maxVal, _rotPID, ref lastError, ref totalError);
        rb.angularVelocity = vel;
    }

    private Vector3 PIDUpdate(Vector3 error, float clampVal, PID pid, ref Vector3 lastError, ref Vector3 totalError)
    {
        totalError += error * Time.deltaTime;
        var cp = error * pid.kp;
        var cd = pid.kd * (error - lastError) / Time.fixedDeltaTime;
        var ci = totalError * pid.ki;
        lastError = error;
        return Vector3.ClampMagnitude(cp+cd+ci, clampVal);
    }

    private void ResetError()
    {
        // totalPosError = new Vector3();
        // lastPosError = new Vector3();
        // totalVelError = new Vector3();
        // lastVelError = new Vector3();
        // totalRotError = new Vector3();
        // lastRotError = new Vector3();
        lastError = new Vector3();
        totalError = new Vector3();
    }
}
