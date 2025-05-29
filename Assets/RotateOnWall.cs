using UnityEngine;

public class RotateOnWall : MonoBehaviour
{
    public float rotationSpeed = 45f;
    public Transform rotatingChild;

    private Vector3 currentNormal = Vector3.up;

    public void SetPositionAndOrientation(Vector3 hitPoint, Vector3 hitNormal)
    {
        currentNormal = hitNormal.normalized;

        // Position & orient the parent
        transform.position = hitPoint + currentNormal * 0.01f;
        transform.rotation = Quaternion.LookRotation(-currentNormal);
    }

    void Update()
    {
        if (rotatingChild != null)
        {
            rotatingChild.Rotate(currentNormal, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}
