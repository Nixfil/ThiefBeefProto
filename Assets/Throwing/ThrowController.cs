using UnityEngine;

public class ThrowController : MonoBehaviour
{
    [Header("References")]
    public GameObject projectilePrefab;
    public Transform spawnPoint;
    public LineRenderer lineRenderer;
    public LayerMask groundMask;
    public LayerMask throwMask;
    public GameObject ghostIndicatorPrefab;


    [Header("Throw Settings")]
    public float AimOffset;
    public float gravity = 9.81f;
    public int trajectorySteps = 30;
    public float minThrowAngle = 25f;
    public float maxThrowAngle = 60f;
    public AnimationCurve angleByDistance; // Define this in the inspector!

    private GameObject ghostInstance;
    private Camera cam;
    private bool isAiming;

    void Start()
    {
        cam = Camera.main;
        lineRenderer.positionCount = trajectorySteps;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isAiming = true;
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            Vector3? target = GetMouseTargetPoint();
            if (target.HasValue)
            {
                Vector3 velocity;
                if (ComputeVelocityArc(target.Value, out velocity))
                {
                    DrawTrajectory(spawnPoint.position, velocity);
                }
            }
        }

        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            isAiming = false;
            Vector3? target = GetMouseTargetPoint();
            if (target.HasValue)
            {
                Vector3 velocity;
                if (ComputeVelocityArc(target.Value, out velocity))
                {
                    ThrowProjectile(velocity);
                }
            }

            lineRenderer.enabled = false;
        }
    }

    Vector3? GetMouseTargetPoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            return hit.point;
        }
        return null;
    }

    bool ComputeVelocityArc(Vector3 target, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 dir = target - spawnPoint.position;
        float h = dir.y;
        dir.y = 0;
        float distance = dir.magnitude;
        float height = target.y - spawnPoint.position.y;

        float t = Mathf.Clamp01(distance / 20f); // Normalize for curve use
        float angle = Mathf.Lerp(minThrowAngle, maxThrowAngle, angleByDistance.Evaluate(t));
        float radians = angle * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        float v2 = (gravity * distance * distance) / (2 * cos * cos * (distance * Mathf.Tan(radians) - height));
        if (v2 < 0) return false; // No valid arc
        float v = Mathf.Sqrt(v2);

        Vector3 dirNormalized = dir.normalized;
        velocity = dirNormalized * v * cos + Vector3.up * v * sin;
        return true;
    }

    void DrawTrajectory(Vector3 startPos, Vector3 velocity)
    {
        lineRenderer.enabled = true;
        Vector3 prevPoint = startPos;
        lineRenderer.SetPosition(0, prevPoint);

        Vector3 hitPoint = prevPoint;
        bool hitSomething = false;

        for (int i = 1; i < trajectorySteps; i++)
        {
            float t = i * 0.1f;
            Vector3 point = startPos + velocity * t + 0.5f * Physics.gravity * t * t;

            if (Physics.Raycast(prevPoint, point - prevPoint, out RaycastHit hit, (point - prevPoint).magnitude, throwMask))
            {
                hitPoint = hit.point;
                lineRenderer.SetPosition(i, hitPoint);
                hitSomething = true;

                for (int j = i + 1; j < trajectorySteps; j++)
                {
                    lineRenderer.SetPosition(j, hitPoint);
                }

                break;
            }

            lineRenderer.SetPosition(i, point);
            prevPoint = point;
            hitPoint = point;
        }

        if (ghostInstance == null && ghostIndicatorPrefab != null)
        {
            ghostInstance = Instantiate(ghostIndicatorPrefab);
        }

        if (ghostInstance != null)
        {
            ghostInstance.SetActive(true);
            var higherHitPoint = hitPoint;
            higherHitPoint.y += 0.3f;
            Vector3 closerPoint = Vector3.MoveTowards(higherHitPoint, startPos, AimOffset);
            ghostInstance.transform.position = closerPoint;
        }
    }



    void ThrowProjectile(Vector3 velocity)
    {
        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = velocity;
        }
    }
}
