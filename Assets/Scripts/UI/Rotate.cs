using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{

    private bool rotating;
    public float rotationSpeed = 90;
 
    // Update is called once per frame
    void Update()
    {
/*      if (rotating)
        {
            transform.Rotate(Vector3.up, 90 * Time.deltaTime);
        }*/
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

    }

    public void StartRotating()
    {
        rotating = true;
    }

    public void StopRotating()
    {
        rotating = false;
    }
}
