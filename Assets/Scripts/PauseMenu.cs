using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Containers")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject optionsMenuUI;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip openPauseSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Options UI")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorWhenPlaying = true;

    private const string MusicVolumeKey = "MusicVolume";
    private const string AmbienceVolumeKey = "AmbienceVolume";
    private const string FullscreenKey = "Fullscreen";

    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0f;
    [SerializeField, Range(0f, 1f)] private float defaultAmbienceVolume = 0.7f;

    private bool isPaused = false;

    private void Awake()
    {
        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.ignoreListenerPause = true;
        }

        if (musicSource != null)
            musicSource.playOnAwake = true;

        if (ambienceSource != null)
            ambienceSource.playOnAwake = true;
    }

    private void Start()
    {
        if (pauseMenuUI == null)
            Debug.LogError("PauseMenu: Asigna PauseContent a pauseMenuUI en el Inspector.");

        if (optionsMenuUI == null)
            Debug.LogError("PauseMenu: Asigna OptionsMenu a optionsMenuUI en el Inspector.");

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (optionsMenuUI != null)
            optionsMenuUI.SetActive(false);

        LoadAudioOptions();
        LoadFullscreenOption();

        ResumeGame(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

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

            ResumeGame(true);
        }
    }

    public void PauseGame()
    {
        isPaused = true;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        if (optionsMenuUI != null)
            optionsMenuUI.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlaySound(openPauseSound);
    }

    public void ResumeGame()
    {
        ResumeGame(true);
    }

    private void ResumeGame(bool playSound)
    {
        if (playSound)
            PlayClick();

        isPaused = false;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (optionsMenuUI != null)
            optionsMenuUI.SetActive(false);

        Time.timeScale = 1f;

        if (lockCursorWhenPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OpenOptions()
    {
        PlayClick();

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (optionsMenuUI != null)
            optionsMenuUI.SetActive(true);
    }

    public void CloseOptions()
    {
        PlayClick();

        if (optionsMenuUI != null)
            optionsMenuUI.SetActive(false);

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
    }

    public void BackToMainMenu()
    {
        PlayClick();

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        PlayClick();

        Time.timeScale = 1f;

        BackToMainMenu();
    }

    public void SetFullscreen(bool value)
    {
        PlayClick();
        ApplyFullscreen(value, true);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = value;

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
    }

    public void SetAmbienceVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (ambienceSource != null)
            ambienceSource.volume = value;

        PlayerPrefs.SetFloat(AmbienceVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void LoadAudioOptions()
    {
        float savedMusicVolume = PlayerPrefs.HasKey(MusicVolumeKey)
            ? PlayerPrefs.GetFloat(MusicVolumeKey)
            : defaultMusicVolume;

        float savedAmbienceVolume = PlayerPrefs.HasKey(AmbienceVolumeKey)
            ? PlayerPrefs.GetFloat(AmbienceVolumeKey)
            : defaultAmbienceVolume;

        savedMusicVolume = Mathf.Clamp01(savedMusicVolume);
        savedAmbienceVolume = Mathf.Clamp01(savedAmbienceVolume);

        if (musicSource != null)
            musicSource.volume = savedMusicVolume;

        if (ambienceSource != null)
            ambienceSource.volume = savedAmbienceVolume;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(savedMusicVolume);
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (ambienceVolumeSlider != null)
        {
            ambienceVolumeSlider.SetValueWithoutNotify(savedAmbienceVolume);
            ambienceVolumeSlider.onValueChanged.RemoveListener(SetAmbienceVolume);
            ambienceVolumeSlider.onValueChanged.AddListener(SetAmbienceVolume);
        }
    }

    private void LoadFullscreenOption()
    {
        bool fullscreen = PlayerPrefs.HasKey(FullscreenKey)
            ? PlayerPrefs.GetInt(FullscreenKey) == 1
            : Screen.fullScreen;

        ApplyFullscreen(fullscreen, false);

        if (fullscreenToggle == null && optionsMenuUI != null)
            fullscreenToggle = optionsMenuUI.GetComponentInChildren<Toggle>(true);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void ApplyFullscreen(bool fullscreen, bool save)
    {
        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, mode);

        if (!save)
            return;

        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void PlayClick()
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (sfxSource == null)
            return;

        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
