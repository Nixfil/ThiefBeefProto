using UnityEngine;


public class BulletController : MonoBehaviour
{
    public ShootController controller;
    public Transform targetProjectile;
    public Rigidbody rb;
    public float speed = 20f;

    private void Update()
    {
        if (targetProjectile != null)
        {
            Vector3 direction = (targetProjectile.position - transform.position).normalized;
            rb.velocity = direction * speed;

            // Optional: look at target for better effect
            transform.LookAt(targetProjectile);
        }
        else
        {
            // No target: maybe destroy bullet after some timeout or immediately
            Destroy(gameObject);
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == targetProjectile)
        {
            // Tell ShootingController bullet hit target (assumes singleton or assigned reference)
            controller.OnBulletHitProjectile();
            Debug.Log("OnTriggerCalled");

            Destroy(gameObject);
        }
    }

}