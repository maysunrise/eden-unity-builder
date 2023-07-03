using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float Seconds = 5;
    void Start()
    {
        Destroy(gameObject, Seconds);
    }
}
