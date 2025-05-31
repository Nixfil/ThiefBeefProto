// FILE: Projectile.cs (Modified)
using UnityEngine;
using System.Collections.Generic; // Required for List

[RequireComponent(typeof(Rigidbody))] // Ensures the projectile always has a Rigidbody
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Register this projectile's Rigidbody with the GameTimeManager
            GameTimeManager.Instance.RegisterRigidbody(rb);
            Debug.Log($"Projectile {name} Awake: Rigidbody registered with GameTimeManager. Initial kinematic state: {rb.isKinematic}");
        }
        else
        {
            Debug.LogError($"Projectile {name}: Rigidbody component not found in Awake!", this);
        }
    }

    /// <summary>
    /// Sets the new velocity of the projectile.
    /// This is now primarily called by ProjectileLauncher.RedirectExistingProjectile.
    /// </summary>
    /// <param name="newVelocity">The new velocity vector for the projectile.</param>
    public void SetVelocity(Vector3 newVelocity)
    {
        if (rb != null)
        {
            Debug.Log($"Projectile {name} SetVelocity called. Current kinematic state: {rb.isKinematic}");
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                Debug.Log($"Projectile {name} SetVelocity: Rigidbody was kinematic, setting to non-kinematic.");
            }
            rb.velocity = newVelocity;
            Debug.Log($"Projectile {name} velocity updated to: {newVelocity}. New kinematic state: {rb.isKinematic}");
        }
        else
        {
            Debug.LogWarning("Projectile Rigidbody is null, cannot set velocity.", this);
        }
    }

    // Removed InitiateRedirection as its logic is now handled by ProjectileLauncher.RedirectExistingProjectile

    void OnDestroy()
    {
        Debug.Log($"Projectile {name} OnDestroy called.");
        // Unregister this projectile's Rigidbody from the GameTimeManager
        if (rb != null && GameTimeManager.Instance != null) // Check for null instance if manager might be destroyed first
        {
            GameTimeManager.Instance.UnregisterRigidbody(rb);
            Debug.Log($"Projectile {name} Rigidbody unregistered from GameTimeManager.");
        }

        // Remove this projectile from the static list when it is destroyed
        if (ProjectileLauncher.ActiveProjectiles.Contains(this))
        {
            ProjectileLauncher.ActiveProjectiles.Remove(this);
            Debug.Log($"Projectile {name} removed from ActiveProjectiles list. Remaining: {ProjectileLauncher.ActiveProjectiles.Count}");
        }
    }
}
