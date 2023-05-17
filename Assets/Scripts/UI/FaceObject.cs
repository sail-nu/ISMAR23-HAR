using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceObject : MonoBehaviour
{
    [SerializeField] Transform objectToFace;

    void Update()
    {
        transform.LookAt(objectToFace);
    }
}
