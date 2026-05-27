using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroTypewriterSceneController : MonoBehaviour
{
    [Header("Text Sequence")]
    [SerializeField] private TMP_Text[] textSequence;

    [Header("Typewriter")]
    [SerializeField, Min(0.001f)] private float characterDelay = 0.025f;
    [SerializeField, Min(0f)] private float delayBetweenSequenceTexts = 0.35f;
    [SerializeField] private bool startOnEnable = true;
    [SerializeField] private bool clickCompletesCurrentText = true;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Arena";

    private Coroutine introRoutine;
    private bool isTyping;
    private bool introFinished;
    private bool skipCurrentText;
    private bool loadingScene;

    private void OnEnable()
    {
        if (startOnEnable)
            PlayIntro();
    }

    private void Update()
    {
        if (!WasMouseClicked())
            return;

        if (isTyping && clickCompletesCurrentText)
        {
            skipCurrentText = true;
            return;
        }

        if (introFinished)
            LoadNextScene();
    }

    public void PlayIntro()
    {
        if (introRoutine != null)
            StopCoroutine(introRoutine);

        introRoutine = StartCoroutine(PlayIntroRoutine());
    }

    public void LoadNextScene()
    {
        if (loadingScene || string.IsNullOrWhiteSpace(nextSceneName))
            return;

        loadingScene = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator PlayIntroRoutine()
    {
        introFinished = false;
        loadingScene = false;
        isTyping = true;
        skipCurrentText = false;

        PrepareSequenceTexts();

        if (textSequence != null)
        {
            foreach (TMP_Text text in textSequence)
            {
                if (text == null)
                    continue;

                yield return TypeText(text);
                yield return new WaitForSeconds(delayBetweenSequenceTexts);

                text.maxVisibleCharacters = 0;
                text.gameObject.SetActive(false);
            }
        }

        isTyping = false;
        introFinished = true;
        yield return new WaitForSeconds(delayBetweenSequenceTexts);
        LoadNextScene();
    }

    private void PrepareSequenceTexts()
    {
        if (textSequence == null)
            return;

        foreach (TMP_Text text in textSequence)
        {
            if (text == null)
                continue;

            text.gameObject.SetActive(false);
            text.maxVisibleCharacters = 0;
        }
    }

    private IEnumerator TypeText(TMP_Text text)
    {
        text.gameObject.SetActive(true);
        text.ForceMeshUpdate();

        int characterCount = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;
        skipCurrentText = false;

        for (int i = 0; i <= characterCount; i++)
        {
            if (skipCurrentText)
            {
                text.maxVisibleCharacters = int.MaxValue;
                skipCurrentText = false;
                yield break;
            }

            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(characterDelay);
        }
    }

    private bool WasMouseClicked()
    {
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
}
