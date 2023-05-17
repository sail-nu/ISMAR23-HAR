using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyVRParts : MonoBehaviour
{
    int index = 0;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 1; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
        RecursivelyDestroyVR_Children(transform.GetChild(0));
    }

    public static void RecursivelyDestroyVR_Children(Transform parent)
    {
        if (parent.name.StartsWith("VR_")) 
            Destroy(parent.gameObject);
        else
        {
            for (int i = 0; i < parent.childCount; i++) RecursivelyDestroyVR_Children(parent.GetChild(i));
        }
    }

}
