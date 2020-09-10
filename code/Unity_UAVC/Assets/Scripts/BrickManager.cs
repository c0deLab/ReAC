using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class BrickManager : MonoBehaviour
{
    private List<Brick> _bricks;
    private const string _jsonPath = "Models/blueprint";

    public List<Brick> UnAssigned
    {
        get { return _bricks.Where(b => !b.Assigned).ToList(); }
    }

    public string GetJSON()
    {
        return Resources.Load<TextAsset>(_jsonPath).text;
    }

    public List<Brick> InitBlocks(string json, Transform parent, bool setActive)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            DestroyImmediate(parent.transform.GetChild(i).gameObject);
        List<Brick> bricks = new List<Brick>();
        var d = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
        foreach (var dict in d)
        {
            var targetPos = new Vector3(float.Parse(dict["transX"]), float.Parse(dict["transY"]),
                float.Parse(dict["transZ"]));
            var targetRot = new Vector3(0, float.Parse(dict["rotY"]), 0);
            var prefab = Resources.Load($"Prefabs/Blocks/{dict["tag"]}", typeof(GameObject)) as GameObject;
            prefab.transform.position = targetPos;
            prefab.transform.eulerAngles = targetRot;
            prefab.tag = dict["tag"];
            var brickObj = Instantiate(prefab, parent);
            var brick = brickObj.AddComponent<Brick>();
            brick.InitAttribute(0, targetPos, targetRot, parent);
            brickObj.SetActive(setActive);
            bricks.Add(brick);
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
        return UnAssigned[0];
    }
}