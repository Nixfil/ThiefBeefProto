using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeCircleRenderer : MonoBehaviour
{
    
    public int segments = 60;
    public float radius;
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
    }
    private void Start()
    {
        DrawCircle();
    }

    public void DrawCircle()
    {
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 point = new Vector3(x, 0.01f, z); // Slightly above ground
            lineRenderer.SetPosition(i, point);
        }
    }

    // Optional: Call this if you want to update radius dynamically
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        DrawCircle();
    }

    public void ToggleCircle(bool toggle)
    {
        lineRenderer.enabled = toggle;
    }
}
