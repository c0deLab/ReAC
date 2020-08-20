using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Manager : MonoBehaviour
{
    // public GameObject prefabDrone;
    // public GameObject supply;
    // public GameObject target;
    [SerializeField] private Transform Supplies;
    [SerializeField] private Transform Drones;
    [SerializeField] private Transform Blocks;

    private List<Transform> unbuilt = new List<Transform>();
    private List<Drone> drones = new List<Drone>();
    private Dictionary<string, List<Supply>> supplyTags = new Dictionary<string, List<Supply>>();

    private void Awake()
    {
        foreach (Transform block in Blocks)
        {
            unbuilt.Add(block);
            block.gameObject.SetActive(false);
            if (!supplyTags.ContainsKey(block.tag))
                supplyTags.Add(block.tag, new List<Supply>());
        }

        Debug.Log($"Initialized {unbuilt.Count} block(s) in {supplyTags.Count} type(s)");

        foreach (Transform drone in Drones)
        {
            var droneScript = drone.gameObject.GetComponent<Drone>();
            droneScript.manager = this;
            drones.Add(droneScript);
        }

        Debug.Log($"Initialized {drones.Count} drone(s)");

        foreach (Transform supplyTrans in Supplies)
        {
            var t = (from Transform supplyChild in supplyTrans where supplyTags.ContainsKey(supplyChild.tag) select supplyChild.tag).FirstOrDefault();
            if (t == null)
            {
                Debug.LogWarning($"There is no corresponding brick tag in Supply: {supplyTrans.gameObject}");
                continue;
            }
            var supply = supplyTrans.GetComponent<Supply>();
            supply.type = t;
            supplyTags[t].Add(supply);
            Debug.Log($"Initialized {supply.name}, type: {supply.type}, waitings: {supply.CountAvailable()-1}");
        }

        if (supplyTags.Any(kvp => kvp.Value.Count == 0))
            throw new Exception("No supply for certain brick type");
            
    }

    private void Start()
    {
        foreach (var drone in drones)
        {
            drone.running = true;
        }
    }

    public void AssignTarget(Drone drone)
    {
        if (unbuilt.Count == 0)
        {
            drone.target = null;
            return;
        }

        drone.target = unbuilt[0].gameObject;
        Supply.AssignSupply(drone, supplyTags[drone.target.tag]);
        if (drone.supply != null)
            unbuilt.RemoveAt(0);
        else
            drone.target = null;
    }
}