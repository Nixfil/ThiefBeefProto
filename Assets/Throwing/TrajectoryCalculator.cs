// FILE: TrajectoryCalculator.cs (Modified)
using UnityEngine;

public static class TrajectoryCalculator
{
    /// <summary>
    /// Computes the initial velocity required to hit a target point with an arc.
    /// </summary>
    /// <param name="start">The starting position of the projectile.</param>
    /// <param name="target">The desired target position.</param>
    /// <param name="minAngle">Minimum launch angle in degrees.</param>
    /// <param name="maxAngle">Maximum launch angle in degrees.</param>
    /// <param name="angleByDistance">Animation curve to determine angle based on horizontal distance.</param>
    /// <param name="gravity">The gravity value to use (e.g., Physics.gravity.y).</param>
    /// <param name="velocity">Output: The calculated initial velocity vector.</param>
    /// <returns>True if a valid arc was computed, false otherwise.</returns>
    public static bool ComputeVelocityArc(Vector3 start, Vector3 target, float minAngle, float maxAngle, AnimationCurve angleByDistance, float gravity, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 dir = target - start;
        float h = dir.y; // Vertical distance
        dir.y = 0; // Horizontal projection
        float distance = dir.magnitude; // Horizontal distance

        Debug.Log($"TrajectoryCalculator: ComputeVelocityArc called. Start: {start}, Target: {target}, Distance: {distance}, Height: {h}");

        // Handle cases where curve might be null or empty
        if (angleByDistance == null || angleByDistance.keys.Length == 0)
        {
            Debug.LogWarning("TrajectoryCalculator: AngleByDistance curve is not set or empty. Using default angle.");
            // Fallback to a mid-range angle if curve is missing
            float defaultAngle = Mathf.Lerp(minAngle, maxAngle, 0.5f);
            float defaultRadians = defaultAngle * Mathf.Deg2Rad;
            float defaultCos = Mathf.Cos(defaultRadians);
            float defaultTan = Mathf.Tan(defaultRadians);

            // Ensure distance is not zero to prevent division by zero
            if (distance < 0.01f) // Small epsilon to avoid division by zero
            {
                Debug.LogWarning("TrajectoryCalculator: Horizontal distance too small for arc calculation, returning false.");
                return false;
            }

            float denominator = (2 * defaultCos * defaultCos * (distance * defaultTan - h));
            if (Mathf.Abs(denominator) < 0.001f) // Avoid division by near-zero
            {
                Debug.LogWarning("TrajectoryCalculator: Denominator near zero, returning false.");
                return false;
            }

            float defaultV2 = (gravity * distance * distance) / denominator;
            if (defaultV2 < 0)
            {
                Debug.Log($"TrajectoryCalculator: Default V2 is negative ({defaultV2}), no valid arc. Returning false.");
                return false;
            }
            float defaultV = Mathf.Sqrt(defaultV2);

            Vector3 defaultDirNormalized = dir.normalized;
            velocity = defaultDirNormalized * defaultV * defaultCos + Vector3.up * defaultV * Mathf.Sin(defaultRadians);
            Debug.Log($"TrajectoryCalculator: Default arc computed. Velocity: {velocity}");
            return true;
        }

        // Normalize distance for curve evaluation (assuming curve is mapped from 0 to 1 for a max practical distance, e.g., 20 units)
        float maxEffectiveDistanceForCurve = 50f; // A reasonable max for curve normalization, adjust as needed
        float normalizedDistanceForCurve = Mathf.Clamp01(distance / maxEffectiveDistanceForCurve);
        Debug.Log($"TrajectoryCalculator: Normalized distance for curve: {normalizedDistanceForCurve}");

        float angle = Mathf.Lerp(minAngle, maxAngle, angleByDistance.Evaluate(normalizedDistanceForCurve));
        float radians = angle * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        float tan = Mathf.Tan(radians);

        float v2 = (gravity * distance * distance) / (2 * cos * cos * (distance * tan - h));

        if (v2 < 0)
        {
            Debug.Log($"TrajectoryCalculator: V2 is negative ({v2}), no valid arc exists (e.g., target too far for angle, or trying to throw downwards too steeply). Returning false.");
            return false; // No valid arc exists
        }

        float v = Mathf.Sqrt(v2);

        Vector3 dirNormalized = dir.normalized;
        velocity = dirNormalized * v * cos + Vector3.up * v * sin;
        Debug.Log($"TrajectoryCalculator: Arc computed. Velocity: {velocity}");
        return true;
    }


    /// <summary>
    /// Draws the trajectory path using a LineRenderer and checks for interruptions.
    /// </summary>
    /// <param name="lineRenderer">The LineRenderer component to draw the trajectory.</param>
    /// <param name="startPos">The starting position of the projectile.</param>
    /// <param name="velocity">The initial velocity of the projectile.</param>
    /// <param name="trajectorySteps">The number of points to use for the trajectory line.</param>
    /// <param name="stepDeltaTime">The time increment between each step in the trajectory calculation.</param>
    /// <param name="interruptMask">LayerMask for objects that can interrupt the trajectory (e.g., walls, tables).</param>
    /// <param name="triggerInterruptMask">Special LayerMask for "trigger" interruptions (e.g., tables).</param>
    /// <param name="groundMask">LayerMask for ground, used to distinguish interruption types.</param>
    /// <param name="wasInterrupted">Output: True if the trajectory hit something from the interruptMask.</param>
    /// <param name="interruptPoint">Output: The world position where the trajectory was interrupted.</param>
    /// <param name="interruptedByTable">Output: True if the interruption was by an object in the triggerInterruptMask.</param>
    /// <param name="lastHit">Output: The RaycastHit data for the first interruption.</param>
    public static void DrawTrajectory(
        LineRenderer lineRenderer,
        Vector3 startPos,
        Vector3 velocity,
        int trajectorySteps,
        float stepDeltaTime,
        LayerMask interruptMask,
        LayerMask triggerInterruptMask,
        LayerMask groundMask,
        out bool wasInterrupted,
        out Vector3 interruptPoint,
        out bool interruptedByTable,
        out RaycastHit lastHit)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = trajectorySteps;
        Vector3 prevPoint = startPos;
        lineRenderer.SetPosition(0, prevPoint);

        interruptPoint = Vector3.zero;
        wasInterrupted = false;
        interruptedByTable = false;
        lastHit = default;

        for (int i = 1; i < trajectorySteps; i++)
        {
            float t = i * stepDeltaTime;
            // Note: Uses Physics.gravity for trajectory calculation
            Vector3 point = startPos + velocity * t + 0.5f * Physics.gravity * t * t;

            // Check for collision between previous point and current point
            if (Physics.Raycast(prevPoint, point - prevPoint, out RaycastHit hit, (point - prevPoint).magnitude, interruptMask))
            {
                interruptPoint = hit.point;
                lineRenderer.SetPosition(i, interruptPoint);
                wasInterrupted = true;
                lastHit = hit;

                // Fill the rest of the line renderer positions to the interruption point
                for (int j = i + 1; j < trajectorySteps; j++)
                    lineRenderer.SetPosition(j, interruptPoint);

                // Check if the interrupted object is in the special 'triggerInterruptMask' (e.g., tables)
                if (((1 << hit.collider.gameObject.layer) & triggerInterruptMask) != 0)
                {
                    interruptedByTable = true;
                }
                break; // Stop drawing after hitting something
            }

            lineRenderer.SetPosition(i, point);
            prevPoint = point;
        }

        // If no interruption occurred during the loop, the interruptPoint is just the final point
        if (!wasInterrupted)
        {
            interruptPoint = lineRenderer.GetPosition(trajectorySteps - 1);
        }
    }

    /// <summary>
    /// Calculates the estimated duration of a projectile's flight until it hits something or reaches the end of its calculated path.
    /// </summary>
    /// <param name="startPos">The starting position of the projectile.</param>
    /// <param name="velocity">The initial velocity of the projectile.</param>
    /// <param name="trajectorySteps">The number of steps used for trajectory calculation.</param>
    /// <param name="stepDeltaTime">The time increment between each step.</param>
    /// <param name="interruptMask">LayerMask for objects that can interrupt the trajectory.</param>
    /// <returns>The estimated duration in seconds.</returns>
    public static float CalculateThrowDuration(Vector3 startPos, Vector3 velocity, int trajectorySteps, float stepDeltaTime, LayerMask interruptMask)
    {
        Vector3 prevPoint = startPos;

        for (int i = 1; i < trajectorySteps; i++)
        {
            float t = i * stepDeltaTime;
            Vector3 point = startPos + velocity * t + 0.5f * Physics.gravity * t * t;

            if (Physics.Raycast(prevPoint, point - prevPoint, out RaycastHit hit, (point - prevPoint).magnitude, interruptMask))
            {
                return t; // Duration until interruption
            }
            prevPoint = point;
        }

        return trajectorySteps * stepDeltaTime; // Max time if no interruption
    }
}
