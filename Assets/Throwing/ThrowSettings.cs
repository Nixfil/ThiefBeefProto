using UnityEngine;

[CreateAssetMenu(menuName = "Throwing/Throw Settings")]
public class ThrowSettings : ScriptableObject
{
    public float initialSpeed = 10f;
    public float throwAngleMultiplier = 0.5f;
    public AnimationCurve speedOverTimeCurve;
    public float minThrowRange = 1f;
    public float maxThrowRange = 10f;
    public GameObject projectilePrefab;
}
