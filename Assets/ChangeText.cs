using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeText : MonoBehaviour
{
    public GameObject myGameObject;
    TextMesh textMesh;
    private void Start()
    {
        textMesh = myGameObject.GetComponent<TextMesh>();
        textMesh.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        float randomValue = Random.value; // Generate a random value between 0 and 1

        if (randomValue <= 0.90f) // 80% chance
        {
            textMesh.text = "Screwing";
        }
        else if (randomValue <= 0.96f) // 10% chance
        {
            textMesh.text = "Flip";
        }
      /**  else if (randomValue <= 0.95f) // 10% chance
        {
            textMesh.text = "Shake";
        }**/
        else // 10% chance
        {
            textMesh.text = "Flip";
        }
    }
}
