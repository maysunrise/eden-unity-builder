using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Centroid : MonoBehaviour
{
    public Transform Parent;
    void Start()
    {
        
    }

    void Update()
    {
        Vector3 centroid = transform.position;

        Transform[] allChildren = Parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            centroid += child.transform.position;
        }
        centroid /= (allChildren.Length);
        transform.position = centroid;
    }
}
