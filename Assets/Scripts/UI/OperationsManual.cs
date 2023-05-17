using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Rendering;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class OperationsManual : MonoBehaviour
{
    public ScrollingObjectCollection pageCollection;
    private GameObject[] subchapterMenus;

    private void Start()
    {
        // Store references to submodule menus
        subchapterMenus = GameObject.FindGameObjectsWithTag("SubchapterMenu");
        ClearSubchapterMenuOptions();
        if ( subchapterMenus != null) Debug.Log("Num menus " + subchapterMenus.Length.ToString());
    }

    public void MoveToPage(int index)
    {
        if ( pageCollection != null ) pageCollection.MoveToIndex(index, true);
    }

    public void ClearSubchapterMenuOptions()
    {
        if (subchapterMenus == null) return;
        foreach (GameObject o in subchapterMenus)
        {
            o.SetActive(false);
        }
    }


    public void ToggleVisibility()
    {
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void OpenURL()
    {
        // This crashes 
        //Application.OpenURL("http://unity3d.com/");
    }
}
