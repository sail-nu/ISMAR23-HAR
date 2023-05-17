using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepositionOrbital : MonoBehaviour
{
    private Orbital orbital;

    void Start()
    {
        orbital = GetComponent<Orbital>();
    }

    private void OnEnable()
    {
        if (orbital == null || orbital.WorldOffset == null) return;
        orbital.WorldOffset = new Vector3(orbital.WorldOffset.x, Camera.main.transform.position.y, orbital.WorldOffset.z);
    }

    public void DisableOrbital()
    {
        // Set orbital inactive
        orbital.enabled = false;
    }

    public void SetOrbitalPosition()
    {
        gameObject.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
    }

}
