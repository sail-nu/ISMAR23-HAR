using UnityEngine.Playables;
using UnityEngine;

public class PlayableDirectorLoop : MonoBehaviour
{
    public PlayableDirector director;

    private void Start()
    {
        director = GetComponent<PlayableDirector>();
    }

    void OnEnable()
    {
        director.stopped += OnPlayableDirectorStopped;
    }

    void OnPlayableDirectorStopped(PlayableDirector playableDirector)
    {
        if (playableDirector == director)
            director.Play();
    }

    void OnDisable()
    {
        director.stopped -= OnPlayableDirectorStopped;
    }
}
