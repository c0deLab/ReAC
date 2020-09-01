using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RLConfig : MonoBehaviour
{
    [Range(1, 20)]
    public float RespawnDistance = 7.0f;
    [Range(1, 20)]
    public float TargetDistance = 7.0f;
    [Range(0, 0.4f)]
    public float ReachTargetTolerance = 0.2f;
}
