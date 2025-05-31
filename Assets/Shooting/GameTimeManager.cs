// FILE: GameTimeManager.cs (Modified)
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the physics state of registered Rigidbodies by directly pausing/resuming their movement.
/// Designed as a singleton for easy access from other systems.
/// This version does NOT manipulate Time.timeScale.
/// </summary>
public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }

    // Struct to store the state of a Rigidbody before freezing
    private struct RigidbodyState
    {
        public Rigidbody rb;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public bool wasKinematic; // To restore original kinematic state

        public RigidbodyState(Rigidbody rigidbody)
        {
            rb = rigidbody;
            velocity = rigidbody.velocity;
            angularVelocity = rigidbody.angularVelocity;
            wasKinematic = rigidbody.isKinematic;
        }
    }

    // Dictionary to hold states of Rigidbodies that are paused by the manager
    // Using a Dictionary for quick lookup and removal by Rigidbody reference
    private Dictionary<Rigidbody, RigidbodyState> _registeredRigidbodies = new Dictionary<Rigidbody, RigidbodyState>();

    // _originalTimeScale is no longer directly used for pausing, but kept for reference if needed elsewhere.
    private float _originalTimeScale;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Optional: If you want this manager to persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }
        Debug.Log("GameTimeManager Awake.");
    }

    void Start()
    {
        _originalTimeScale = Time.timeScale; // Capture initial time scale (still useful for general game state)
        Debug.Log($"GameTimeManager Start: Original TimeScale = {_originalTimeScale}");
    }

    /// <summary>
    /// Registers a Rigidbody to be managed by the GameTimeManager.
    /// When this manager's FreezeTime is called, this Rigidbody's physics will be paused.
    /// </summary>
    /// <param name="rb">The Rigidbody to register.</param>
    public void RegisterRigidbody(Rigidbody rb)
    {
        if (rb != null && !_registeredRigidbodies.ContainsKey(rb))
        {
            _registeredRigidbodies.Add(rb, new RigidbodyState()); // Add with dummy state for now
            Debug.Log($"GameTimeManager: Rigidbody {rb.name} registered. Total registered: {_registeredRigidbodies.Count}");
        }
        else if (rb != null)
        {
            Debug.LogWarning($"GameTimeManager: Rigidbody {rb.name} already registered or is null.");
        }
    }

    /// <summary>
    /// Unregisters a Rigidbody from being managed by the GameTimeManager.
    /// </summary>
    /// <param name="rb">The Rigidbody to unregister.</param>
    public void UnregisterRigidbody(Rigidbody rb)
    {
        if (rb != null && _registeredRigidbodies.ContainsKey(rb))
        {
            _registeredRigidbodies.Remove(rb);
            Debug.Log($"GameTimeManager: Rigidbody {rb.name} unregistered. Total registered: {_registeredRigidbodies.Count}");
        }
    }

    /// <summary>
    /// Freezes the movement of all registered Rigidbodies by setting them kinematic.
    /// Does NOT manipulate Time.timeScale. Velocities are not explicitly zeroed here
    /// as setting isKinematic to true is sufficient to stop physics movement.
    /// </summary>
    public void FreezeTime()
    {
        Debug.Log("GameTimeManager: Freezing registered Rigidbodies.");

        // Create a temporary list of Rigidbodies to avoid modifying the collection while iterating
        List<Rigidbody> rigidbodiesToFreeze = new List<Rigidbody>(_registeredRigidbodies.Keys);

        foreach (Rigidbody rb in rigidbodiesToFreeze)
        {
            if (rb != null && rb.gameObject.activeInHierarchy) // Only process active RBs
            {
                // Store current state
                _registeredRigidbodies[rb] = new RigidbodyState(rb); // Update with actual current state
                Debug.Log($"GameTimeManager: Freezing Rigidbody {rb.name}. Current velocity: {rb.velocity}, Kinematic: {rb.isKinematic}");

                // Pause physics: Setting isKinematic to true stops all physics simulation for this Rigidbody.
                // Do NOT set velocity or angularVelocity here, as it will cause the "kinematic body" error.
                rb.isKinematic = true;
                Debug.Log($"GameTimeManager: Rigidbody {rb.name} now Kinematic: {rb.isKinematic}");
            }
            else if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"GameTimeManager: Attempted to freeze a null or inactive Rigidbody in registered list ({rb?.name ?? "NULL"}). Removing it.");
                _registeredRigidbodies.Remove(rb); // Clean up null/inactive entries
            }
        }
        Debug.Log("GameTimeManager: All registered Rigidbodies frozen.");
    }

    /// <summary>
    /// Resumes the movement of all registered Rigidbodies by restoring their original
    /// kinematic state and velocities. Does NOT manipulate Time.timeScale.
    /// </summary>
    public void ResumeTime()
    {
        Debug.Log("GameTimeManager: Resuming registered Rigidbodies.");

        // Restore state for paused Rigidbodies
        // Create a temporary list of RigidbodyStates to avoid modifying the collection while iterating
        List<RigidbodyState> statesToRestore = new List<RigidbodyState>(_registeredRigidbodies.Values);

        foreach (var state in statesToRestore)
        {
            Rigidbody rb = state.rb;
            if (rb != null && rb.gameObject.activeInHierarchy) // Check if the Rigidbody still exists and is active
            {
                Debug.Log($"GameTimeManager: Restoring Rigidbody {rb.name}. Original Kinematic: {state.wasKinematic}, Original Velocity: {state.velocity}");
                rb.isKinematic = state.wasKinematic; // Restore original kinematic state
                rb.velocity = state.velocity;
                rb.angularVelocity = state.angularVelocity;
                Debug.Log($"GameTimeManager: Rigidbody {rb.name} now Kinematic: {rb.isKinematic}, Velocity: {rb.velocity}");
            }
            else if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"GameTimeManager: Attempted to restore a null or inactive Rigidbody ({state.rb?.name ?? "NULL"}). It will not be restored.");
            }
        }
        Debug.Log("GameTimeManager: All registered Rigidbodies resumed.");
    }
}
