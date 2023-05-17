using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyChildObjects : MonoBehaviour
{

    void Start()
    {

    }

    private void Update()
    {
        // Loop through all child objects of this parent object
        foreach (Transform child in gameObject.transform)
        {
            // Deactivate the child object
            child.gameObject.SetActive(false);
        }
    }
}
