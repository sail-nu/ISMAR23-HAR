using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleGameObject : MonoBehaviour
{
    public GameObject objectToToggle;

    public void Toggle()
    {
        if (objectToToggle == null) return;
        objectToToggle.SetActive(!objectToToggle.activeSelf);
    }
}
