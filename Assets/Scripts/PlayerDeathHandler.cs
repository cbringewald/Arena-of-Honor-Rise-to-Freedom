using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;

    [Header("Scripts To Disable On Death")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Header("Death Settings")]
    [SerializeField] private float delayBeforeMainMenu = 4f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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

        if (animator != null && !string.IsNullOrEmpty(deathTrigger))
        {
            animator.ResetTrigger(deathTrigger);
            animator.SetTrigger(deathTrigger);
        }

        if (showCursorOnDeath)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(delayBeforeMainMenu);

        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
