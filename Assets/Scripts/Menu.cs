using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsMenu;
    public GameObject mainMenu;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSound;

    [Header("Options")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private const string FullscreenKey = "Fullscreen";

    private void Start()
    {
        if (optionsMenu != null)
            optionsMenu.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(true);

        if (musicSource != null && !musicSource.isPlaying)
            musicSource.Play();

        if (musicSlider != null && musicSource != null)
        {
            musicSlider.value = musicSource.volume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        LoadFullscreenOption();
    }

    public void OpenOptionsPanel()
    {
        PlayClick();

        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void OpenMainMenuPanel()
    {
        PlayClick();

        mainMenu.SetActive(true);
        optionsMenu.SetActive(false);
    }

    public void PlayGame()
    {
        PlayClick();

        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        PlayClick();

        Application.Quit();
    }

    public void SetMusicVolume(float value)
    {
        if (musicSource != null)
            musicSource.volume = value;
    }

    public void SetFullscreen(bool value)
    {
        PlayClick();
        ApplyFullscreen(value, true);
    }

    private void LoadFullscreenOption()
    {
        bool fullscreen = PlayerPrefs.HasKey(FullscreenKey)
            ? PlayerPrefs.GetInt(FullscreenKey) == 1
            : Screen.fullScreen;

        ApplyFullscreen(fullscreen, false);

        if (fullscreenToggle == null && optionsMenu != null)
            fullscreenToggle = optionsMenu.GetComponentInChildren<Toggle>(true);

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
        if (sfxSource != null && clickSound != null)
            sfxSource.PlayOneShot(clickSound);
    }
}
