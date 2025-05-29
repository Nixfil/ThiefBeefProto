using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectorScript : MonoBehaviour
{
    public LayerMask ThingsThatICantStandStraightUnder;
    public List<GameObject> ObjectsAboveMyHead;
    private void OnTriggerEnter(Collider other)
    {
        if ((ThingsThatICantStandStraightUnder.value & (1 << other.gameObject.layer)) != 0)
        {
            ObjectsAboveMyHead.Add(other.gameObject);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if ((ThingsThatICantStandStraightUnder.value & (1 << other.gameObject.layer)) != 0)
        {
            ObjectsAboveMyHead.Remove(other.gameObject);
        }
    }
}
