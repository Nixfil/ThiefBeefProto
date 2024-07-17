using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform playerCharacter;  // Reference to the player's transform
    public float cameraSpeed = 2.0f;   // Speed at which the camera moves
    public Vector2 screenBounds;       // Screen boundaries in world units

    private Camera cam;
    private float camHeight;
    private float camWidth;

    void Start()
    {
        cam = Camera.main;
        camHeight = 2f * cam.orthographicSize;
        camWidth = camHeight * cam.aspect;
    }

    void Update()
    {
        Vector3 playerPosition = playerCharacter.position;
        Vector3 camPosition = transform.position;

        // Calculate the current camera bounds
        float leftBound = camPosition.x - camWidth / 2 + screenBounds.x;
        float rightBound = camPosition.x + camWidth / 2 - screenBounds.x;
        float bottomBound = camPosition.y - camHeight / 2 + screenBounds.y;
        float topBound = camPosition.y + camHeight / 2 - screenBounds.y;

        // Check if the player is too close to the screen borders and adjust camera position
        if (playerPosition.x < leftBound)
        {
            camPosition.x -= leftBound - playerPosition.x;
        }
        else if (playerPosition.x > rightBound)
        {
            camPosition.x += playerPosition.x - rightBound;
        }

        if (playerPosition.y < bottomBound)
        {
            camPosition.y -= bottomBound - playerPosition.y;
        }
        else if (playerPosition.y > topBound)
        {
            camPosition.y += playerPosition.y - topBound;
        }

        // Smoothly move the camera to the new position
        transform.position = Vector3.Lerp(transform.position, camPosition, Time.deltaTime * cameraSpeed);
    }
}
