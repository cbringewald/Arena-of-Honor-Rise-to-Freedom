using System.Collections;
using UnityEngine;

public class HitstopManager : MonoBehaviour
{
    public static HitstopManager Instance { get; private set; }

    private Coroutine hitstopRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void DoHitstop(float duration, float timeScale = 0.05f)
    {
        if (hitstopRoutine != null)
            StopCoroutine(hitstopRoutine);

        hitstopRoutine = StartCoroutine(HitstopRoutine(duration, timeScale));
    }

    private IEnumerator HitstopRoutine(float duration, float timeScale)
    {
        float originalTimeScale = Time.timeScale;
        float originalFixedDelta = Time.fixedDeltaTime;

        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDelta;

        hitstopRoutine = null;
    }
}
