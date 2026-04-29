using System.Collections;
using TMPro;
using UnityEngine;

public class RoundMessageUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private RectTransform panelTransform;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float defaultVisibleTime = 2.2f;
    [SerializeField] private float fadeOutTime = 0.45f;

    [Header("Scale Effect")]
    [SerializeField] private bool useScaleEffect = true;
    [SerializeField] private Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
    [SerializeField] private Vector3 endScale = Vector3.one;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (panelTransform == null)
            panelTransform = GetComponent<RectTransform>();

        HideInstant();
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultVisibleTime);
    }

    public void ShowMessage(string message, float visibleTime)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message, visibleTime));
    }

    public void HideInstant()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (panelTransform != null)
            panelTransform.localScale = endScale;
    }

    private IEnumerator ShowRoutine(string message, float visibleTime)
    {
        if (messageText != null)
            messageText.text = message;

        if (canvasGroup == null)
            yield break;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0f;

        if (useScaleEffect && panelTransform != null)
            panelTransform.localScale = startScale;

        float timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeInTime);

            canvasGroup.alpha = t;

            if (useScaleEffect && panelTransform != null)
                panelTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (panelTransform != null)
            panelTransform.localScale = endScale;

        yield return new WaitForSecondsRealtime(visibleTime);

        timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeOutTime);

            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        HideInstant();
    }
}