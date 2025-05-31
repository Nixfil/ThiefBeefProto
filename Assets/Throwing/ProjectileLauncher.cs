// FILE: ProjectileLauncher.cs (Modified - No ResumeTime)
using UnityEngine;
using System.Collections.Generic; // Required for List

public class ProjectileLauncher : MonoBehaviour
{
    // Static list to keep track of all currently active projectiles launched by this system
    public static List<Projectile> ActiveProjectiles = new List<Projectile>();

    [Tooltip("The projectile prefab to instantiate.")]
    public GameObject projectilePrefab;
    [Tooltip("The exact transform from which the projectile will be launched.")]
    public Transform launchPoint;

    /// <summary>
    /// Instantiates the projectile prefab at the launch point and sets its initial velocity.
    /// </summary>
    /// <param name="initialVelocity">The velocity to apply to the launched projectile.</param>
    /// <returns>The GameObject of the instantiated projectile, or null if setup is invalid.</returns>
    public GameObject LaunchProjectile(Vector3 initialVelocity)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile Prefab is not assigned to ProjectileLauncher!", this);
            return null;
        }
        if (launchPoint == null)
        {
            Debug.LogError("Launch Point is not assigned to ProjectileLauncher!", this);
            return null;
        }

        GameObject projectileInstance = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);
        Rigidbody rb = projectileInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = initialVelocity;
        }
        else
        {
            Debug.LogWarning("Projectile prefab does not have a Rigidbody component! Cannot set initial velocity.", projectileInstance);
        }

        // Get the Projectile component and add it to our static list
        Projectile projectileComponent = projectileInstance.GetComponent<Projectile>();
        if (projectileComponent != null)
        {
            ActiveProjectiles.Add(projectileComponent);
        }
        else
        {
            Debug.LogWarning($"Launched projectile {projectileInstance.name} does not have a Projectile script!", projectileInstance);
        }

        return projectileInstance;
    }

    /// <summary>
    /// Redirects an existing projectile by setting its new velocity.
    /// This reuses the "launching" concept without instantiating a new object.
    /// This method no longer calls GameTimeManager.Instance.ResumeTime().
    /// </summary>
    /// <param name="projectileToRedirect">The existing Projectile instance to redirect.</param>
    /// <param name="newVelocity">The new velocity to apply to the projectile.</param>
    public void RedirectExistingProjectile(Projectile projectileToRedirect, Vector3 newVelocity)
    {
        if (projectileToRedirect == null)
        {
            Debug.LogWarning("Attempted to redirect a null projectile.", this);
            return;
        }

        Rigidbody rb = projectileToRedirect.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"Projectile {projectileToRedirect.name} does not have a Rigidbody component! Cannot redirect.", projectileToRedirect);
            return;
        }

        // Ensure the Rigidbody is not kinematic before setting velocity
        if (rb.isKinematic)
        {
            rb.isKinematic = false;
        }
        rb.velocity = newVelocity;

        // You might want to reset angular velocity too if it's not desired after redirection
        rb.angularVelocity = Vector3.zero;

        // REMOVED: GameTimeManager.Instance.ResumeTime();
        Debug.Log($"Projectile {projectileToRedirect.name} redirected by Launcher with new velocity: {newVelocity}.");

        // Play redirection specific sound effect
        // AudioManager.Instance.PlaySFX(AudioManager.Instance.RedirectionSuccess); // Example SFX
    }
}
