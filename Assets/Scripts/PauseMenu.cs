using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject optionsMenuUI;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        if (pauseMenuUI == null) Debug.LogError("PauseMenu: Asigna PauseContent a pauseMenuUI en el Inspector.");
        if (optionsMenuUI == null) Debug.LogError("PauseMenu: Asigna OptionsMenu a optionsMenuUI en el Inspector.");

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);

        ResumeGame();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPaused)
            {
                PauseGame();
                return;
            }

            if (optionsMenuUI != null && optionsMenuUI.activeSelf)
            {
                CloseOptions();
                return;
            }

            ResumeGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (optionsMenuUI != null) optionsMenuUI.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenOptions()
    {
        if (pauseMenuUI == null || optionsMenuUI == null) return;

        pauseMenuUI.SetActive(false);
        optionsMenuUI.SetActive(true);
    }

    public void CloseOptions()
    {
        if (pauseMenuUI == null || optionsMenuUI == null) return;

        optionsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    public void SetFullscreen(bool value)
    {
        Debug.Log("Fullscreen: " + value);
        Screen.fullScreen = value;
    }

    public void SetMusicVolume(float value)
    {
        Debug.Log("Music volume: " + value);
    }
}

