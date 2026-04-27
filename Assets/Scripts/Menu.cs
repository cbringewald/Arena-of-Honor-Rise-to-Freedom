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
        Screen.fullScreen = value;
    }

    private void PlayClick()
    {
        if (sfxSource != null && clickSound != null)
            sfxSource.PlayOneShot(clickSound);
    }
}