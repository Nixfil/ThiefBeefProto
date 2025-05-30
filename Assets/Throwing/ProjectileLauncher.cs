// FILE: ProjectileLauncher.cs
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
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
        return projectileInstance;
    }
}