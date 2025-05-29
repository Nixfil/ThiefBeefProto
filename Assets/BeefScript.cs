using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeefScript : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);
    public float graceDistance = 3f;
    public float upwardForce = 2f;
    public LayerMask SolidGroundForSFX;
    public Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        // Rotate the object every frame
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
    void FixedUpdate()
    {
        if (rb.velocity.y < 0) // Only while falling
        {
            Ray ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, graceDistance))
            {
                float factor = 1 - (hit.distance / graceDistance);
                rb.AddForce(Vector3.up * upwardForce * factor, ForceMode.Acceleration);
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & SolidGroundForSFX) != 0)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.Fall);
        }
    }
}
