using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MobileOptionManager : BaseOptionManager
{
    [Header("Mobile Specific Graphics UI")]
    public TMP_Dropdown targetFrameRateDropdown;

    [Header("Mobile Specific Control UI")]
    public Slider touchDragSensitivitySlider;
    public TMP_InputField touchDragSensitivityInput;

    private int savedFpsIndex;

    protected override void InitPlatformGraphics()
    {
        if (targetFrameRateDropdown != null)
        {
            targetFrameRateDropdown.onValueChanged.AddListener(delegate { hasUnsavedChanges = true; });

            targetFrameRateDropdown.ClearOptions();
            List<string> fpsOptions = new List<string> { "30 FPS", "60 FPS", "120 FPS" };
            targetFrameRateDropdown.AddOptions(fpsOptions);

            int currentFps = Application.targetFrameRate;
            if (currentFps <= 30) savedFpsIndex = 0;
            else if (currentFps <= 60) savedFpsIndex = 1;
            else savedFpsIndex = 2;

            targetFrameRateDropdown.value = savedFpsIndex;
            targetFrameRateDropdown.RefreshShownValue();
        }
    }

    protected override void ApplyPlatformGraphics()
    {
        if (targetFrameRateDropdown != null)
        {
            if (targetFrameRateDropdown.value == 0) Application.targetFrameRate = 30;
            else if (targetFrameRateDropdown.value == 1) Application.targetFrameRate = 60;
            else Application.targetFrameRate = 120;

            savedFpsIndex = targetFrameRateDropdown.value;
        }
    }

    protected override void RevertPlatformGraphics()
    {
        if (targetFrameRateDropdown != null)
        {
            targetFrameRateDropdown.value = savedFpsIndex;
        }
    }

    protected override void InitPlatformControls()
    {
        if (touchDragSensitivitySlider != null)
            touchDragSensitivitySlider.onValueChanged.AddListener(OnTouchSensitivitySliderChanged);

        if (touchDragSensitivityInput != null)
            touchDragSensitivityInput.onValueChanged.AddListener(OnTouchSensitivityInputChanged);

        float savedSensitivity = PlayerPrefs.GetFloat("TouchSensitivity", 1.0f);

        if (touchDragSensitivitySlider != null)
            touchDragSensitivitySlider.value = savedSensitivity;

        if (touchDragSensitivityInput != null)
            touchDragSensitivityInput.text = savedSensitivity.ToString("F2");
    }

    private void OnTouchSensitivitySliderChanged(float value)
    {
        if (touchDragSensitivityInput != null)
            touchDragSensitivityInput.text = value.ToString("F2");
        SaveSensitivity(value);
    }

    private void OnTouchSensitivityInputChanged(string text)
    {
        if (float.TryParse(text, out float value))
        {
            if (touchDragSensitivitySlider != null)
            {
                value = Mathf.Clamp(value, touchDragSensitivitySlider.minValue, touchDragSensitivitySlider.maxValue);
                touchDragSensitivitySlider.value = value;
            }
            SaveSensitivity(value);
        }
    }

    private void SaveSensitivity(float value)
    {
        PlayerPrefs.SetFloat("TouchSensitivity", value);
        PlayerPrefs.Save();
    }
}