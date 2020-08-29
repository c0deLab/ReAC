using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class Manager : MonoBehaviour
{
    [SerializeField] private Transform Supplies;
    [SerializeField] private Transform Drones;
    [SerializeField] private Transform Bricks;

    private BrickManager _brickManager;
    private List<Drone> _drones = new List<Drone>();
    private Dictionary<string, List<Supply>> supplyTags = new Dictionary<string, List<Supply>>();

    private void Awake()
    {
        _brickManager = Bricks.GetComponent<BrickManager>();

        foreach (Transform drone in Drones)
        {
            var droneScript = drone.gameObject.GetComponent<Drone>();
            droneScript.manager = this;
            _drones.Add(droneScript);
        }

        Debug.Log($"Initialized {_drones.Count} drone(s)");

        foreach (Transform supplyTrans in Supplies)
        {
            var t = (from Transform supplyChild in supplyTrans where _brickManager.GetTags().Contains(supplyChild.tag) select supplyChild.tag).FirstOrDefault();
            if (t == null)
            {
                Debug.LogWarning($"There is no corresponding brick tag in Supply: {supplyTrans.name}");
                continue;
            }
            var supply = supplyTrans.GetComponent<Supply>();
            supply.type = t;
            if (!supplyTags.ContainsKey(t))
                supplyTags.Add(t, new List<Supply>());
            supplyTags[t].Add(supply);
            Debug.Log($"Initialized {supply.name}, type: {supply.type}, waitings: {supply.CountAvailable()-1}");
        }

        if (supplyTags.Any(kvp => kvp.Value.Count == 0))
            throw new Exception("No supply for certain brick type");
            
    }

    private void Start()
    {
        foreach (var brick in _brickManager.UnAssigned)
        {
            brick.gameObject.SetActive(false);
        }
        foreach (var drone in _drones)
        {
            drone.running = true;
        }
    }

    public void AssignTarget(Drone drone)
    {
        if (_brickManager.UnAssigned.Count == 0)
        {
            drone.target = null;
            return;
        }

        var brick = _brickManager.GetNextBrick();
        Debug.Assert(drone.target == null);
        Supply.AssignSupply(drone, supplyTags[brick.tag]);
        if (drone.supply != null)
        {
            brick.Assigned = true;
            drone.target = brick;
        }
    }
}