using UnityEngine;

[CreateAssetMenu(fileName = "ThrowTrajectoryData", menuName = "Game/Throw Trajectory")]
public class ThrowTrajectoryData : ScriptableObject
{
    public float throwDuration = 0.75f;
    public float arcHeight = 2.5f;
    public int previewResolution = 15;
    public float maxRange = 7f;
}
