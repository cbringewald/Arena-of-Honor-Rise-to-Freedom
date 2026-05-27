using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsOptions : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Dropdown legacyQualityDropdown;
    [SerializeField] private Dropdown legacyResolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private const string QualityKey = "GraphicsQuality";
    private const string ResolutionWidthKey = "ResolutionWidth";
    private const string ResolutionHeightKey = "ResolutionHeight";
    private const string FullscreenKey = "Fullscreen";

    private readonly List<ResolutionOption> resolutionOptions = new List<ResolutionOption>();

    private struct ResolutionOption
    {
        public int width;
        public int height;

        public ResolutionOption(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return width + " x " + height;
        }
    }

    private void Start()
    {
        BuildQualityDropdowns();
        BuildResolutionDropdowns();
        LoadAndApplyOptions();
        BindUIEvents();
    }

    public void SetQuality(int index)
    {
        int qualityIndex = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityIndex, true);
        PlayerPrefs.SetInt(QualityKey, qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetResolution(int index)
    {
        if (resolutionOptions.Count == 0)
            return;

        int resolutionIndex = Mathf.Clamp(index, 0, resolutionOptions.Count - 1);
        ResolutionOption option = resolutionOptions[resolutionIndex];
        bool fullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
        ApplyResolution(option.width, option.height, fullscreen);

        PlayerPrefs.SetInt(ResolutionWidthKey, option.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, option.height);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        int width = PlayerPrefs.HasKey(ResolutionWidthKey) ? PlayerPrefs.GetInt(ResolutionWidthKey) : Screen.currentResolution.width;
        int height = PlayerPrefs.HasKey(ResolutionHeightKey) ? PlayerPrefs.GetInt(ResolutionHeightKey) : Screen.currentResolution.height;

        ApplyResolution(width, height, fullscreen);

        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void BuildQualityDropdowns()
    {
        List<string> qualityNames = new List<string>(QualitySettings.names);

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(qualityNames);
        }

        if (legacyQualityDropdown != null)
        {
            legacyQualityDropdown.ClearOptions();
            legacyQualityDropdown.AddOptions(qualityNames);
        }
    }

    private void BuildResolutionDropdowns()
    {
        resolutionOptions.Clear();
        HashSet<string> addedResolutions = new HashSet<string>();

        foreach (Resolution resolution in Screen.resolutions)
        {
            string key = resolution.width + "x" + resolution.height;

            if (addedResolutions.Contains(key))
                continue;

            addedResolutions.Add(key);
            resolutionOptions.Add(new ResolutionOption(resolution.width, resolution.height));
        }

        if (resolutionOptions.Count == 0)
            resolutionOptions.Add(new ResolutionOption(Screen.currentResolution.width, Screen.currentResolution.height));

        List<string> labels = new List<string>();

        foreach (ResolutionOption option in resolutionOptions)
            labels.Add(option.ToString());

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
        }

        if (legacyResolutionDropdown != null)
        {
            legacyResolutionDropdown.ClearOptions();
            legacyResolutionDropdown.AddOptions(labels);
        }
    }

    private void LoadAndApplyOptions()
    {
        int qualityIndex = PlayerPrefs.HasKey(QualityKey)
            ? PlayerPrefs.GetInt(QualityKey)
            : QualitySettings.GetQualityLevel();

        qualityIndex = Mathf.Clamp(qualityIndex, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityIndex, true);

        bool fullscreen = PlayerPrefs.HasKey(FullscreenKey)
            ? PlayerPrefs.GetInt(FullscreenKey) == 1
            : Screen.fullScreen;

        int width = PlayerPrefs.HasKey(ResolutionWidthKey) ? PlayerPrefs.GetInt(ResolutionWidthKey) : Screen.currentResolution.width;
        int height = PlayerPrefs.HasKey(ResolutionHeightKey) ? PlayerPrefs.GetInt(ResolutionHeightKey) : Screen.currentResolution.height;
        int resolutionIndex = GetResolutionIndex(width, height);

        ApplyResolution(resolutionOptions[resolutionIndex].width, resolutionOptions[resolutionIndex].height, fullscreen);

        SetDropdownValuesWithoutNotify(qualityIndex, resolutionIndex, fullscreen);
    }

    private void BindUIEvents()
    {
        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveListener(SetQuality);
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        if (legacyQualityDropdown != null)
        {
            legacyQualityDropdown.onValueChanged.RemoveListener(SetQuality);
            legacyQualityDropdown.onValueChanged.AddListener(SetQuality);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(SetResolution);
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (legacyResolutionDropdown != null)
        {
            legacyResolutionDropdown.onValueChanged.RemoveListener(SetResolution);
            legacyResolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    private void SetDropdownValuesWithoutNotify(int qualityIndex, int resolutionIndex, bool fullscreen)
    {
        if (qualityDropdown != null)
            qualityDropdown.SetValueWithoutNotify(qualityIndex);

        if (legacyQualityDropdown != null)
            legacyQualityDropdown.SetValueWithoutNotify(qualityIndex);

        if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);

        if (legacyResolutionDropdown != null)
            legacyResolutionDropdown.SetValueWithoutNotify(resolutionIndex);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
    }

    private int GetResolutionIndex(int width, int height)
    {
        for (int i = 0; i < resolutionOptions.Count; i++)
        {
            ResolutionOption option = resolutionOptions[i];

            if (option.width == width && option.height == height)
                return i;
        }

        return Mathf.Max(0, resolutionOptions.Count - 1);
    }

    private void ApplyResolution(int width, int height, bool fullscreen)
    {
        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(width, height, mode);
    }
}
