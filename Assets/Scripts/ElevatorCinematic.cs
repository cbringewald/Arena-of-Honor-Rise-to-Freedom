using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class ElevatorCinematic : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private Component[] cinematicActorsToHide;
    [SerializeField] private bool showCinematicActorsOnPlay = true;
    [SerializeField] private bool hideCinematicActorsOnEnd = true;

    private void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        if (director != null)
        {
            director.playOnAwake = false;
            director.Stop();
            director.time = 0;
        }

        if (hideCinematicActorsOnEnd)
            SetCinematicActorsActive(false);
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= HandleDirectorStopped;
    }

    public void PlayCinematic()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureDirector();

        if (director == null)
            return;

        if (showCinematicActorsOnPlay)
            SetCinematicActorsActive(true);

        director.stopped -= HandleDirectorStopped;
        director.stopped += HandleDirectorStopped;
        director.time = 0;
        director.Play();

        Debug.Log("Cinemática ascensor iniciada");
    }

    public IEnumerator PlayAndWait(float fallbackDuration)
    {
        PlayCinematic();

        if (director == null)
            yield break;

        if (director.duration > 0 && !double.IsInfinity(director.duration))
        {
            while (director.state == PlayState.Playing)
                yield return null;

            HideCinematicActorsAfterPlayback();
            yield break;
        }

        if (fallbackDuration > 0f)
            yield return new WaitForSeconds(fallbackDuration);

        HideCinematicActorsAfterPlayback();
    }

    private void HideCinematicActorsAfterPlayback()
    {
        if (hideCinematicActorsOnEnd)
            SetCinematicActorsActive(false);
    }

    private void EnsureDirector()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();

        if (director == null)
            return;

        director.playOnAwake = false;
    }

    private void HandleDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != director)
            return;

        HideCinematicActorsAfterPlayback();
    }

    private void SetCinematicActorsActive(bool active)
    {
        if (cinematicActorsToHide == null)
            return;

        foreach (Component actor in cinematicActorsToHide)
        {
            if (actor == null)
                continue;

            actor.gameObject.SetActive(active);
        }
    }
}
