using UnityEngine;

public class RangeCircleRenderer : MonoBehaviour
{
    
    public float radius;
    public MeshRenderer Renderer;

    void Awake()
    {

        /*lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;*/
    }

    // Optional: Call this if you want to update radius dynamically
    public void SetRadius(float newRadius)
    {
        radius = newRadius/4.3f;
        transform.localScale = new Vector3(radius, radius, radius); 
    }

    public void ToggleCircle(bool toggle)
    {
        Renderer.enabled = toggle;
    }
    public void SetCenter(Vector3 center)
    {
        this.transform.position = center;
        // No need to call DrawCircle() here if it's handled by SetRadius or ToggleCircle
        // as you described in your ThrowVisualsManager pattern.
    }
}
