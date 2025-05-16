using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeefScript : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);

    void Update()
    {
        // Rotate the object every frame
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
