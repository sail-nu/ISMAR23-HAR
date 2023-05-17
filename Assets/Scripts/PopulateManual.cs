using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopulateManual : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Material[] pageMaterials = Resources.LoadAll<Material>("Powder_Feeder/Manual/Materials");

        Debug.Log("Materials: " + pageMaterials.Length);

        for (int i = 0; i < this.gameObject.transform.childCount; i++)
        {
            this.gameObject.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material = pageMaterials[i];
        }
    }

    
}
