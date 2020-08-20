using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

public class BlockManager : MonoBehaviour
{
    private void Awake()
    {
        using (StreamReader r = new StreamReader("Assets/Scripts/blueprint.json"))
        {
            string json = r.ReadToEnd();
            var d = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
            foreach (var dict in d)
            {
                var prefab = Resources.Load($"Prefabs/Blocks/{dict["tag"]}", typeof(GameObject)) as GameObject;
                prefab.transform.position = new Vector3(float.Parse(dict["transX"]), float.Parse(dict["transY"]), float.Parse(dict["transZ"]));
                prefab.transform.eulerAngles = new Vector3(0, float.Parse(dict["rotY"]), 0);
                prefab.tag = dict["tag"];
                Instantiate(prefab, transform);
            }
            
        }
    }
}
