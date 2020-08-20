using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class Supply : MonoBehaviour
{
    [HideInInspector] public string type;

    private List<Transform> waitList = new List<Transform>();
    private Queue<Tuple<Drone, Transform>> waitQueue = new Queue<Tuple<Drone, Transform>>();
    private Drone currentDrone;

    private void Awake()
    {
        foreach (Transform supplyChild in transform)
        {
            if (!supplyChild.CompareTag("Waiting")) continue;
            foreach (Transform waitingPos in supplyChild)
                waitList.Add(waitingPos);
        }
    }

    private void Start()
    {
        if (type == null)
            throw new Exception($"Tag not defined for supply: {this}");
    }

    private void FixedUpdate()
    {
        if (currentDrone != null || waitQueue.Count == 0) return;
        var tuple = waitQueue.Dequeue();
        currentDrone = tuple.Item1;
    }

    private bool IsEmpty()
    {
        return currentDrone == null & waitQueue.Count == 0;
    }

    private bool IsFull()
    {
        return waitQueue.Count == waitList.Count;
    }

    public int CountAvailable()
    {
        var waitings = waitList.Count - waitQueue.Count;
        return IsEmpty() ? waitings + 1 : waitings;
    }

    private void AssignDrone(Drone drone)
    {
        if (IsEmpty())
            currentDrone = drone;
        else if (!IsFull())
        {
            var waiting =
                waitList.Where(x => waitQueue.All(t => t.Item2 != x))
                    .OrderBy(x => Vector3.Distance(x.position, drone.transform.position))
                    .First();
            waitQueue.Enqueue(new Tuple<Drone, Transform>(drone, waiting));
        }
        else
            throw new Exception("Can't assign drone to a full supply station");
    }

    public static void AssignSupply(Drone drone, List<Supply> supplies)
    {
        if (drone.supply != null)
            throw new Exception("Drone's supply isn't null");
        var supply = supplies.Where(x => x.IsEmpty())
            .OrderBy(x => Vector3.Distance(drone.transform.position, x.transform.position))
            .FirstOrDefault();

        if (supply == null)
        {
            supply = supplies.Where(x => !x.IsFull())
                .OrderBy(x => x.CountAvailable())
                .ThenBy(x => Vector3.Distance(drone.transform.position, x.transform.position))
                .FirstOrDefault();
        }

        if (supply != null)
        {
            supply.AssignDrone(drone);
            drone.supply = supply;
        }
        else
        {
            // All supplies are full
        }
    }

    public bool IsDroneCurrent(Drone drone)
    {
        return drone == currentDrone;
    }

    public bool IsDroneWaiting(Drone drone)
    {
        return waitQueue.Any(x => x.Item1 == drone);
    }

    public Transform GetDroneAssignedTransform(Drone drone)
    {
        if (IsDroneCurrent(drone))
            return transform;
        if (IsDroneWaiting(drone))
            return waitQueue.Where(t => t.Item1 == drone).Select(t => t.Item2).First();
        // return waitList.Where(x => x.Value == drone).Select(x => x.Key).First();
        throw new Exception("Drone is not assigned to this supply");
    }

    public void ExitCurrentDrone()
    {
        currentDrone = null;
    }
}