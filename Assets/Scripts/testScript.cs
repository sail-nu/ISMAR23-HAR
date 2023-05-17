using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testScript : MonoBehaviour
{
    float startTime = -1;

    // Start is called before the first frame update
    //void Start()
    //{

    //}

    // Update is called once per frame
    void Update()
    {
        if (( Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended ) || Input.GetMouseButtonUp(0))
        {
            if ( startTime > 0 )
            {

                if ( Time.time - startTime < 0.5 )
                {
                    transform.localPosition = new Vector3 (( transform.localPosition.x > -0.45f ? -0.45f : -0.185f ), 0.075f, 0.52977f);
                }
                startTime = -1;
            }
            else
            {
                startTime = Time.time;
            }

        }
    }

    //public void OnEnable()
    //{
    //}
    //public void OnDisable()
    //{

    //}
}

