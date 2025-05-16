using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreviewer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void DrawTrajectory(Vector3 start, Vector3 end, float arcHeight, int resolution)
    {
        lineRenderer.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            lineRenderer.SetPosition(i, point);
        }
    }

    public void Clear() => lineRenderer.positionCount = 0;
}
