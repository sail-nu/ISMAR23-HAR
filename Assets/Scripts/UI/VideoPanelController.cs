using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoPanelController : MonoBehaviour
{

    [SerializeField] VideoPlayer videoPlayer;

    public void SetActive()
    {
        gameObject.SetActive(true);
    }

    public void SetInactive()
    {
        gameObject.SetActive(false);
    }

    public void SetVideoClip(VideoClip videoClip)
    {
        videoPlayer.clip = videoClip;
    }

    public void OnVideoPressed()
    {
        if (gameObject.activeInHierarchy)
        {
            SetInactive();
        }
        else
        {
            SetActive();
            videoPlayer.Prepare();
            videoPlayer.Play();
            //Invoke("StopVideo", 0.1f);
        }
    }

    private void StopVideo()
    {
        videoPlayer.Pause();
    }

    public void ToggleVideoPlayer()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
        }
    }

    public void SkipVideoForward()
    {
        videoPlayer.time = (double)Mathf.Clamp((float)(videoPlayer.time + 5f), 0f, (float)videoPlayer.length);
    }

    public void SkipVideoBack()
    {
        videoPlayer.time = (double)Mathf.Clamp((float)(videoPlayer.time - 5f), 0f, (float)videoPlayer.length);
    }
}
