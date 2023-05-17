using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Step : MonoBehaviour
{
    [Header("Step Information")]
    public VideoClip[] videos;
    public AudioClip[] audioClips;
    public Sprite[] images;
    public GameObject anim;
    private Animation animation;
    public string text;
    public bool firstStepInModule;
    public string additionalInformation;

    [Header("Confirmation")]
    //public bool askForConfirmation;
    public string confirmationText;
    public string moduleStepYesConfirmation;
    public string moduleStepNoConfirmation;
    public int stepToLoadOnYes;
    public int stepToLoadOnNo;
    public int manualPageNumber;

    [Header("Warning")]
    //public bool displayWarning;
    public string warningText;

    [Header("Models")]
    public string[] modelsToShow;

    private void Start()
    {
        if ( anim != null ) animation = anim.GetComponent<Animation>();
    }

    public Sprite[] GetImages()
    {
        return images;
    }

    public bool HasImages()
    {
        return images.Length > 0;
    }

    public VideoClip[] GetVideos()
    {
        return videos;
    }

    public AudioClip[] GetAudioClips()
    {
        return audioClips;
    }

    public bool HasVideos()
    {
        return videos.Length > 0;
    }

    public bool HasAudioClips()
    {
        return audioClips.Length > 0;
    }

    public string GetStepDestription()
    {
        return text;
    }

    public bool ShouldAskForConfirmation()
    {
        return confirmationText != "";
    }

    public string GetConfirmationText()
    {
        return confirmationText;
    }

    public bool ShouldDisplayWarning()
    {
        return warningText != "";
    }

    public string GetWarningText()
    {
        return "Warning: " + warningText;
    }

    public void PlayAnimation()
    {
        if (anim)
        {
            anim.SetActive(true);
            StartCoroutine("HandlePlayAnimation");
            Debug.LogError("animate");
            //animation.Play();
        }
        
    }

    public void DisableAnimation()
    {
        if (anim)
        {
            anim.SetActive(false);
            StopCoroutine("HandlePlayAnimation");
        }
    }

    private IEnumerator HandlePlayAnimation()
    {
        animation.Play();

        yield return new WaitForSeconds(animation.clip.length);

//#if ! (UNITY_ANDROID || UNITY_IOS)
        // this signal the module manager to replay it a second time so dont comment out
        anim.SetActive(false);
//#endif

        yield return null;
    }

    public int GetStepToLoadOnYes()
    {
        return stepToLoadOnYes;
    }

    public int GetStepToLoadOnNo()
    {
        return stepToLoadOnNo;
    }

    public List<GameObject> GetModelsToDisplay()
    {
        List<GameObject> models = new List<GameObject>();

        Debug.Log(modelsToShow.Length);

        foreach (string str in modelsToShow)
        {
            AddModelToList(str, anim.transform, ref models);
        }

        return models;
    }

    private void AddModelToList(string str, Transform parent, ref List<GameObject> models)
    {
        if (!parent.gameObject.activeInHierarchy)
        {
            return;
        }

        Debug.Log("Comp " + parent.name + " to " + str);

        if (parent.name.EndsWith(str))
            models.Add(parent.gameObject);
        else
        {
            for (int i = 0; i < parent.childCount; i++) AddModelToList(str, parent.GetChild(i), ref models);
        }
    }
}
