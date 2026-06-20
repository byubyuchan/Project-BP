using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PCOptionManager : BaseOptionManager
{
    [Header("PC Specific Graphics UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown refreshRateDropdown;
    public TMP_Dropdown screenModeDropdown;

    [Header("PC Specific Control UI")]
    public Slider sensitivitySlider;
    public TMP_InputField sensitivityInput;

    private List<Vector2Int> uniqueResolutions = new List<Vector2Int>();
    private List<Resolution> currentRefreshRates = new List<Resolution>();

    private int savedResIndex;
    private int savedHzIndex;
    private int savedModeIndex;

    protected override void InitPlatformGraphics()
    {
        resolutionDropdown.onValueChanged.AddListener(UpdateRefreshRateDropdown);
        resolutionDropdown.onValueChanged.AddListener(delegate { hasUnsavedChanges = true; });
        refreshRateDropdown.onValueChanged.AddListener(delegate { hasUnsavedChanges = true; });
        screenModeDropdown.onValueChanged.AddListener(delegate { hasUnsavedChanges = true; });

        Resolution[] allResolutions = Screen.resolutions;
        uniqueResolutions.Clear();
        resolutionDropdown.ClearOptions();

        int currentResIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            float ratio = (float)allResolutions[i].width / allResolutions[i].height;
            if (Mathf.Abs(ratio - (16f / 9f)) < 0.05f && allResolutions[i].width >= 1280)
            {
                Vector2Int size = new Vector2Int(allResolutions[i].width, allResolutions[i].height);
                if (!uniqueResolutions.Contains(size))
                {
                    uniqueResolutions.Add(size);
                }
            }
        }

        List<string> options = new List<string>();
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            options.Add(uniqueResolutions[i].x + " x " + uniqueResolutions[i].y);

            if (uniqueResolutions[i].x == Screen.width && uniqueResolutions[i].y == Screen.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        UpdateRefreshRateDropdown(currentResIndex);

        if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen) screenModeDropdown.value = 0;
        else if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) screenModeDropdown.value = 1;
        else screenModeDropdown.value = 2;

        savedResIndex = resolutionDropdown.value;
        savedHzIndex = refreshRateDropdown.value;
        savedModeIndex = screenModeDropdown.value;
    }

    private void UpdateRefreshRateDropdown(int resIndex)
    {
        refreshRateDropdown.ClearOptions();
        currentRefreshRates.Clear();

        if (uniqueResolutions.Count == 0) return;

        Vector2Int targetSize = uniqueResolutions[resIndex];
        Resolution[] allResolutions = Screen.resolutions;

        List<string> hzOptions = new List<string>();
        int currentHzIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            if (allResolutions[i].width == targetSize.x && allResolutions[i].height == targetSize.y)
            {
                string hzString = Mathf.RoundToInt((float)allResolutions[i].refreshRateRatio.value).ToString() + " Hz";

                if (!hzOptions.Contains(hzString))
                {
                    currentRefreshRates.Add(allResolutions[i]);
                    hzOptions.Add(hzString);

                    int currentScreenHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
                    int optionHz = Mathf.RoundToInt((float)allResolutions[i].refreshRateRatio.value);

                    if (currentScreenHz == optionHz)
                    {
                        currentHzIndex = currentRefreshRates.Count - 1;
                    }
                }
            }
        }

        if (currentHzIndex == 0 && currentRefreshRates.Count > 0)
        {
            currentHzIndex = currentRefreshRates.Count - 1;
        }

        refreshRateDropdown.AddOptions(hzOptions);
        refreshRateDropdown.value = currentHzIndex;
        refreshRateDropdown.RefreshShownValue();
    }

    protected override void ApplyPlatformGraphics()
    {
        if (currentRefreshRates.Count > 0)
        {
            Resolution res = currentRefreshRates[refreshRateDropdown.value];

            FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
            if (screenModeDropdown.value == 1) mode = FullScreenMode.FullScreenWindow;
            else if (screenModeDropdown.value == 2) mode = FullScreenMode.Windowed;

            Screen.SetResolution(res.width, res.height, mode);
        }

        savedResIndex = resolutionDropdown.value;
        savedHzIndex = refreshRateDropdown.value;
        savedModeIndex = screenModeDropdown.value;
    }

    protected override void RevertPlatformGraphics()
    {
        resolutionDropdown.value = savedResIndex;
        UpdateRefreshRateDropdown(savedResIndex);
        refreshRateDropdown.value = savedHzIndex;
        screenModeDropdown.value = savedModeIndex;
    }

    protected override void InitPlatformControls()
    {
        sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
        sensitivityInput.onValueChanged.AddListener(OnSensitivityInputChanged);

        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        sensitivitySlider.value = savedSensitivity;
        sensitivityInput.text = savedSensitivity.ToString("F2");
    }

    private void OnSensitivitySliderChanged(float value)
    {
        sensitivityInput.text = value.ToString("F2");
        SaveSensitivity(value);
    }

    private void OnSensitivityInputChanged(string text)
    {
        if (float.TryParse(text, out float value))
        {
            value = Mathf.Clamp(value, sensitivitySlider.minValue, sensitivitySlider.maxValue);
            sensitivitySlider.value = value;
            SaveSensitivity(value);
        }
    }

    private void SaveSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }
}