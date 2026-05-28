using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private RoundMessageUI defeatMessageUI;

    [Header("Scripts To Disable On Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Death Settings")]
    [SerializeField] private float delayBeforeMainMenu = 4f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string defeatMessage = "DERROTA\nHas caido en la arena";
    [SerializeField] private PlayableDirector deathCinematic;
    [SerializeField] private bool waitDeathCinematic = true;
    [SerializeField, Min(0f)] private float deathCinematicDuration = 5f;

    [Header("Animator Parameters")]
    [SerializeField] private string deathTrigger = "Death";

    [Header("Cursor")]
    [SerializeField] private bool showCursorOnDeath = true;

    private bool deathHandled;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (defeatMessageUI == null)
            defeatMessageUI = FindFirstObjectByType<RoundMessageUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (deathHandled)
            return;

        if (health != null && health.IsDead)
            HandleDeath();
    }

    private void HandleDeath()
    {
        deathHandled = true;

        Time.timeScale = 1f;

        if (scriptsToDisable != null)
        {
            foreach (MonoBehaviour script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        if (characterController != null)
            characterController.enabled = false;

        if (animator != null && HasAnimatorTrigger(deathTrigger))
        {
            animator.ResetTrigger(deathTrigger);
            animator.SetTrigger(deathTrigger);
        }

        if (showCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ShowDefeatMessage();
        StartCoroutine(DeathRoutine());
    }

    private void ShowDefeatMessage()
    {
        if (defeatMessageUI != null)
        {
            defeatMessageUI.ShowMessage(defeatMessage, delayBeforeMainMenu);
            return;
        }

        Debug.Log(defeatMessage);
    }

    private IEnumerator DeathRoutine()
    {
        if (deathCinematic != null)
        {
            deathCinematic.gameObject.SetActive(true);
            deathCinematic.time = 0d;
            deathCinematic.Play();

            if (waitDeathCinematic)
                yield return new WaitForSecondsRealtime(GetPlayableDuration(deathCinematic, deathCinematicDuration));
        }

        yield return ReturnToMainMenuAfterDelay();
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delayBeforeMainMenu);

        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static float GetPlayableDuration(PlayableDirector director, float fallbackDuration)
    {
        if (director == null || director.duration <= 0d || double.IsInfinity(director.duration))
            return Mathf.Max(0f, fallbackDuration);

        return Mathf.Max(0f, (float)director.duration);
    }

    private bool HasAnimatorTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
                return true;
        }

        return false;
    }
}
