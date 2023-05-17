using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animatable : MonoBehaviour
{
    Animation anim;

    void Start()
    {
        anim = GetComponent<Animation>();
    }

    public void Play()
    {
        anim.Play();
    }
}
