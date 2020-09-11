using System;
using System.Collections.Generic;
using UnityEngine;

public class Brick : MonoBehaviour
{
    public Vector3 TargetPosition;
    public Vector3 TargetRotation;
    public int BrickID;
    private List<Brick> ParentBricks = new List<Brick>();
    private List<Brick> ChildBricks = new List<Brick>();
    public bool Built = false;
    public bool Assigned = false;
    private Transform _parentNode;

    public void InitAttribute(Transform parentNode, int brickID, Vector3 targetPosition, Vector3 targetRotation)
    {
        _parentNode = parentNode;
        BrickID = brickID;
        TargetPosition = targetPosition;
        TargetRotation = targetRotation;
    }

    public void Build()
    {
        transform.parent = _parentNode;
        transform.position = TargetPosition;
        transform.rotation = Quaternion.Euler(TargetRotation);
        Built = true;
        gameObject.SetActive(true);
        NotifyChildren();
    }

    public bool IsBuildable()
    {
        return ParentBricks.Count == 0;
    }

    private void NotifyChildren()
    {
        foreach (var child in ChildBricks)
        {
            child.ParentBricks.Remove(this);
        }
    }

    public void AddParent(Brick parent)
    {
        ParentBricks.Add(parent);
    }

    public void AddChild(Brick child)
    {
        ChildBricks.Add(child);
    }
}