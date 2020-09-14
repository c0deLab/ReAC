using UnityEngine;

[ExecuteInEditMode]
public class BrickDisplay : MonoBehaviour
{
    public BrickManager _brickManager;
    private string _json;

    private void Update()
    {
        var json = BrickManager.GetJSON();
        if (json.Equals(_json))
            return;
        _json = json;
        BrickManager.InitBlocks(_json, transform, !Application.isPlaying);
    }
}