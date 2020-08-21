using UnityEngine;

[ExecuteInEditMode]
public class BrickDisplay : MonoBehaviour
{
    public BrickManager _brickManager;

    private void Awake()
    {
        _brickManager.InitBlocks(transform, !Application.isPlaying);
    }
}