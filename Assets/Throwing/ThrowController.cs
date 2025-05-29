using UnityEngine;

public class ThrowController : MonoBehaviour
{
    [Header("References")]
    public GameObject projectilePrefab;
    public Transform spawnPoint;
    public LineRenderer lineRenderer;
    public LayerMask groundMask;
    public LayerMask interruptThrowMask;
    public LayerMask validThrowMask;
    public GameObject ghostIndicatorPrefab;
    public GameObject ghostRangeIndicator;
    public RangeCircleRenderer MinThrowRangeCircle;
    public RangeCircleRenderer MaxThrowRangeCircle;
    public GameObject interruptMarkerPrefab;


    [Header("Throw Settings")]
    public float minThrowRange = 2f;
    public float maxThrowRange = 25f;
    public float AimOffset;
    public float gravity = 9.81f;
    public int trajectorySteps = 30;
    public float minThrowAngle = 25f;
    public float maxThrowAngle = 60f;
    public AnimationCurve angleByDistance; // Define this in the inspector!

    private Vector3? cachedTarget = null;
    private Vector3 cachedVelocity;
    private GameObject ghostInstance;
    private GameObject interruptMarkerInstance;
    private Camera cam;
    private bool isAiming;

    void Start()
    {
        cam = Camera.main;
        lineRenderer.positionCount = trajectorySteps;
        lineRenderer.enabled = false;
        MinThrowRangeCircle.SetRadius(minThrowRange - 2.55f);
        MaxThrowRangeCircle.SetRadius(maxThrowRange + 2);
    }

    void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        isAiming = true;
        MinThrowRangeCircle.ToggleCircle(true);
        MaxThrowRangeCircle.ToggleCircle(true);
    }

    if (Input.GetMouseButton(0) && isAiming)
    {
        Vector3? target = GetMouseTargetPoint();
        if (target.HasValue)
        {
            Vector3 velocity;
            if (ComputeVelocityArc(target.Value, out velocity))
            {
                cachedTarget = target.Value;
                cachedVelocity = velocity;

                DrawTrajectory(spawnPoint.position, velocity);
            }
        }
    }

    if (Input.GetMouseButtonUp(0) && isAiming)
    {
        isAiming = false;

            if (cachedTarget.HasValue)
        {
            ThrowProjectile(cachedVelocity);
        }

        lineRenderer.enabled = false;
        MinThrowRangeCircle.ToggleCircle(false);
        MaxThrowRangeCircle.ToggleCircle(false);

        if (interruptMarkerInstance != null) interruptMarkerInstance.SetActive(false);


            cachedTarget = null; // Reset cache
    }
}


    Vector3? GetMouseTargetPoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, validThrowMask))
        {
            Vector3 fromSpawn = hit.point - spawnPoint.position;
            fromSpawn.y = 0f; // Only consider horizontal distance

            float dist = fromSpawn.magnitude;

            // Clamp the distance between min and max range
            float clampedDist = Mathf.Clamp(dist, minThrowRange, maxThrowRange);

            Vector3 clampedPoint = spawnPoint.position + fromSpawn.normalized * clampedDist;
            clampedPoint.y = hit.point.y; // Preserve the original ground height

            return clampedPoint;
        }

        return null;
    }

    /* Vector3? GetMouseTargetPoint()
     {
         Ray ray = cam.ScreenPointToRay(Input.mousePosition);

         if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0)) // Cast against all layers
         {
             // Check if hit is ground
             if (((1 << hit.collider.gameObject.layer) & groundMask) != 0)
             {
                 // Check distance to spawnPoint
                 float dist = Vector3.Distance(spawnPoint.position, hit.point);
                 if (dist >= minThrowRange && dist <= maxThrowRange)
                 {
                     return hit.point; // Valid ground point within range
                 }
                 else
                 {
                     return null; // Ground but out of range
                 }
             }
             else
             {
                 // Not ground - try to find nearby ground by radial probe
                 float radius = 1.5f;
                 int steps = 16;
                 float heightOffset = 2f;

                 for (int i = 0; i < steps; i++)
                 {
                     float angle = (360f / steps) * i;
                     Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
                     Vector3 probeOrigin = hit.point + direction * radius + Vector3.up * heightOffset;
                     Debug.DrawRay(probeOrigin, Vector3.down * (heightOffset + 1f), Color.red, 0.1f);

                     if (Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit groundHit, heightOffset + 1f, groundMask))
                     {
                         // Check distance for snapped ground point
                         float dist = Vector3.Distance(spawnPoint.position, groundHit.point);
                         if (dist >= minThrowRange && dist <= maxThrowRange)
                         {
                             return groundHit.point; // Valid snapped ground point within range
                         }
                     }
                 }

                 // No nearby valid ground found within range
                 return null;
             }
         }

         // Nothing hit
         return null;
     }*/ //Targetting by snapping to the nearest valid target (groundMask)


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

            if (Physics.Raycast(prevPoint, point - prevPoint, out RaycastHit hit, (point - prevPoint).magnitude, interruptThrowMask))
            {
                hitPoint = hit.point;
                lineRenderer.SetPosition(i, hitPoint);
                hitSomething = true;

                for (int j = i + 1; j < trajectorySteps; j++)
                {
                    lineRenderer.SetPosition(j, hitPoint);
                }

                // === Spawn or reposition marker ===
                if (interruptMarkerInstance == null && interruptMarkerPrefab != null)
                {
                    interruptMarkerInstance = Instantiate(interruptMarkerPrefab);
                }

                if (interruptMarkerInstance != null)
                {
                    interruptMarkerInstance.SetActive(true);
                    var rotator = interruptMarkerInstance.GetComponent<RotateOnWall>();
                    if (rotator != null)
                    {
                        rotator.SetPositionAndOrientation(hit.point, hit.normal);
                    }
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

        if (ghostInstance != null && cachedTarget.HasValue)
        {
            ghostInstance.SetActive(true);
            var targetPoint = cachedTarget.Value;
            targetPoint.y += 0.3f;
            Vector3 closerPoint = Vector3.MoveTowards(targetPoint, startPos, AimOffset);
            ghostInstance.transform.position = closerPoint;
        }
        if (!hitSomething && interruptMarkerInstance != null)
        {
            interruptMarkerInstance.SetActive(false);
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
