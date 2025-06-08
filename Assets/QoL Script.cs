using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class QoLScript
{
public static void DestroyGameObjectIntime(GameObject gameObject, float time)
    {
        Object.Destroy(gameObject, time);
    }
}