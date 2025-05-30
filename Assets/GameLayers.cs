// FILE: GameLayers.cs
using UnityEngine;

// This script should be attached to a GameObject in your scene
// (e.g., a "GameSettings" or "Managers" GameObject)
// and its fields populated in the Inspector.
public class GameLayers : MonoBehaviour
{
    private static GameLayers _instance;

    [Header("Projectile Layers")]
    [Tooltip("Layers that a thrown projectile can hit to be considered a valid target.")]
    public LayerMask validThrowMask;
    [Tooltip("Layers that will interrupt the projectile's trajectory (e.g., walls, tables, ground).")]
    public LayerMask interruptThrowMask;
    [Tooltip("Special layers that interrupt a throw and make it 'invalid' (e.g., tables).")]
    public LayerMask triggerInterruptLayerMask;
    [Tooltip("The layer(s) considered as ground.")]
    public LayerMask groundMask;

    public static LayerMask ValidThrowMask => _instance.validThrowMask;
    public static LayerMask InterruptThrowMask => _instance.interruptThrowMask;
    public static LayerMask TriggerInterruptLayerMask => _instance.triggerInterruptLayerMask;
    public static LayerMask GroundMask => _instance.groundMask;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if you want these settings to persist across scenes
        }
    }

    // You could also add methods to get individual layer indices if needed:
    // public static int GetGroundLayerIndex() => (int)Mathf.Log(GroundMask.value, 2);
}