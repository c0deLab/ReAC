using System;
using UnityEngine;

public class PIDController
{
    private Rigidbody rb;
    private float Kp;
    private float Ki;
    private float Kd;

    private Vector3 totalPosError;
    private Vector3 lastPosError;
    private Vector3 totalVelError;
    private Vector3 lastVelError;
    private Vector3 totalRotError;
    private Vector3 lastRotError;

    public PIDController(Rigidbody rb, float kp, float ki, float kd)
    {
        this.rb = rb;
        Kp = kp;
        Ki = ki;
        Kd = kd;
    }

    public void GoToPos(Vector3 targetPos,float maxSpeed)
    {
        var error = targetPos - rb.position;
        var targetVel = PIDUpdate(error, maxSpeed, ref lastPosError, ref totalPosError);
        var force = (targetVel - rb.velocity) / Time.fixedDeltaTime;
        var gravity = rb.useGravity ? (Physics.gravity * -1) : new Vector3( 0, 0, 0 );
        rb.AddForce( force + gravity, ForceMode.Acceleration);
    }

    public void GoAtVel(Vector3 targetVel)
    {
        var error = targetVel - rb.velocity;
        var force = PIDUpdate(error, Mathf.Infinity, ref lastVelError, ref totalVelError);
        var gravity = rb.useGravity ? (Physics.gravity * -1) : new Vector3( 0, 0, 0 );
        rb.AddForce( force + gravity, ForceMode.Acceleration);
    }

    public void TurnToRot(float targetRotY)
    {
        var dif = targetRotY - rb.transform.rotation.eulerAngles.y;
        if (Mathf.Abs(dif) > 360 - Mathf.Abs(dif))
        {
            if (dif > 0)
                dif = Mathf.Abs(dif) - 360;
            else
                dif = 360 - Mathf.Abs(dif);
        }
    }

    private Vector3 PIDUpdate(Vector3 error, float clampVal, ref Vector3 lastError, ref Vector3 totalError)
    {
        totalError += error * Time.deltaTime;
        var cp = error * Kp;
        var cd = Kd * (error - lastPosError) / Time.fixedDeltaTime;
        var ci = totalPosError * Ki;
        lastError = error;
        return Vector3.ClampMagnitude(cp+cd+ci, clampVal);
    }

    public void ResetError()
    {
        totalPosError = new Vector3();
        lastPosError = new Vector3();
        totalVelError = new Vector3();
        lastVelError = new Vector3();
        totalRotError = new Vector3();
        lastRotError = new Vector3();
    }
}
