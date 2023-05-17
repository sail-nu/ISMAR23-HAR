using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveVRParts : MonoBehaviour
{
    int index = 0;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 1; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);

        RecursivelyDisableVR_Children(transform.GetChild(0));
    }

    public static void RecursivelyDisableVR_Children ( Transform parent )
    {
        if (parent.name.StartsWith("VR_")) parent.gameObject.SetActive(false);
        else
        {
            for (int i = 0; i < parent.childCount; i++) RecursivelyDisableVR_Children(parent.GetChild(i));
        }
    }

    private void Update()
    {
        if ( Input.GetMouseButtonDown(0))
        {
            transform.GetChild(index).gameObject.SetActive(false);
            index++;
            if (index == transform.childCount) index = 0;
            transform.GetChild(index).gameObject.SetActive(true);
            RecursivelyDisableVR_Children(transform.GetChild(index));
        }
    }
}
