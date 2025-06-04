using UnityEngine;

public class BulletController : MonoBehaviour
{
    public ShootController controller;
    public Transform targetProjectile;
    public Rigidbody rb;
    public float speed = 20f;

    [Header("Wobble Settings")]
    public Transform visualModel; // Assign your "Model" GameObject here
    public float wobbleIntensity = 0.2f;
    public float wobbleSpeed = 5f;
    private Vector3 modelInitialLocalPos;

    void Start()
    {
        if (visualModel != null)
            modelInitialLocalPos = visualModel.localPosition;
    }

    void Update()
    {
        if (targetProjectile != null)
        {
            Vector3 direction = (targetProjectile.position - transform.position).normalized;
            rb.velocity = direction * speed;

            transform.LookAt(targetProjectile); // Rotate rocket toward target

            ApplyWobble();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void ApplyWobble()
    {
        if (visualModel == null) return;

        float xOffset = Mathf.Sin(Time.time * wobbleSpeed) * wobbleIntensity;
        float yOffset = Mathf.Cos(Time.time * wobbleSpeed * 1.3f) * wobbleIntensity;

        visualModel.localPosition = modelInitialLocalPos + new Vector3(xOffset, yOffset, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == targetProjectile)
        {
            controller.OnBulletHitProjectile();
            Debug.Log("OnTriggerCalled");

            Destroy(gameObject);
        }
    }
}
