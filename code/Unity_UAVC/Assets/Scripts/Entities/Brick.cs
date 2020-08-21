using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public Vector3 TargetPosition;
    public Vector3 TargetRotation;
    public int BrickID;
    public List<Brick> ParentBricks = new List<Brick>();
    public List<Brick> ChildBricks = new List<Brick>();
    public bool Built = false;
    public bool Assigned = false;
    private Transform _parent;

    public void InitAttribute(int brickID, Vector3 targetPosition, Vector3 targetRotation, Transform parent)
    {
        BrickID = brickID;
        TargetPosition = targetPosition;
        TargetRotation = targetRotation;
        _parent = parent;
    }

    public void ResetParent()
    {
        transform.parent = _parent;
    }
}
