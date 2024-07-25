using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetectionScript : MonoBehaviour
{
    public bool PlayerFound;

    private void Start()
    {
        PlayerFound = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && !PlayerFound)
        {
            PlayerFound = true;
        }
    }

private IEnumerator ResetPlayerFound()
    {
        yield return new WaitForSeconds(30f);
        PlayerFound = false;
    }
}
