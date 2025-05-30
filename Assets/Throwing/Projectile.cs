// FILE: Projectile.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Ensures the projectile always has a Rigidbody
public class Projectile : MonoBehaviour
{
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Sets the new velocity of the projectile.
    /// This is the method the bullet redirection mechanic will call.
    /// </summary>
    /// <param name="newVelocity">The new velocity vector for the projectile.</param>
    public void SetVelocity(Vector3 newVelocity)
    {
        if (rb != null)
        {
            rb.velocity = newVelocity;
            Debug.Log($"Projectile velocity updated to: {newVelocity}");
            // You can add effects here, e.g., particle effects for redirection
        }
    }

    // You can add other projectile-specific logic here, e.g.:
    // void OnCollisionEnter(Collision collision) { ... }
    // void OnDestroy() { ... }
    // public float GetCurrentSpeed() { return rb.velocity.magnitude; }
}