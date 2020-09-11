using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BrickManager : MonoBehaviour
{
    private Bricks _bricks;
    private const string _jsonPath = "Models/blueprint";

    public List<Brick> UnAssigned
    {
        get { return _bricks.Where(b => !b.Assigned).ToList(); }
    }

    public static string GetJSON()
    {
        return Resources.Load<TextAsset>(_jsonPath).text;
    }

    public static Bricks InitBlocks(string json, Transform parentNode, bool setActive)
    {
        for (var i = parentNode.childCount - 1; i >= 0; i--)
            DestroyImmediate(parentNode.transform.GetChild(i).gameObject);
        var bricks = new Bricks();
        var parentIDList = new List<List<int>>();
        var d = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
        foreach (var dict in d)
        {
            var brickID = (int) (long) dict["id"];
            var targetPos = new Vector3(float.Parse((string) dict["transX"]), float.Parse((string) dict["transY"]),
                float.Parse((string) dict["transZ"]));
            var targetRot = new Vector3(0, float.Parse((string) dict["rotY"]), 0);
            var parent_ids = ((JArray) dict["parent_ids"]).Select(p => (int) p).ToList();
            var prefab = Resources.Load($"Prefabs/Blocks/{(string) dict["tag"]}", typeof(GameObject)) as GameObject;
            prefab.transform.position = targetPos;
            prefab.transform.eulerAngles = targetRot;
            prefab.tag = (string) dict["tag"];
            var brickObj = Instantiate(prefab, parentNode);
            var brick = brickObj.AddComponent<Brick>();
            brick.InitAttribute(parentNode, brickID, targetPos, targetRot);
            brickObj.SetActive(setActive);
            bricks.Add(brick);
            parentIDList.Add(parent_ids);
        }

        for (var i = 0; i < bricks.Count; i++)
        {
            foreach (var parentID in parentIDList[i])
            {
                bricks[i].AddParent(bricks[parentID]);
                bricks[parentID].AddChild(bricks[i]);
            }
        }

        return bricks;
    }

    private void Awake()
    {
        _bricks = InitBlocks(GetJSON(), transform, false);
    }

    public List<string> GetTags()
    {
        var tags = new HashSet<string>();
        foreach (var brick in _bricks)
        {
            tags.Add(brick.tag);
        }

        return tags.ToList();
    }

    public Brick GetNextBrick()
    {
        return UnAssigned.FirstOrDefault(b=>b.IsBuildable());
    }
}

public class Bricks : List<Brick>
{
    public new Brick this[int id] => this.FirstOrDefault(b => b.BrickID == id);
}