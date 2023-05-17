using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelCard : MonoBehaviour
{
    public TMPro.TMP_Text description;
    public Transform modelParent;

    public void SetText(string str)
    {
        description.text = str;
    }

}
