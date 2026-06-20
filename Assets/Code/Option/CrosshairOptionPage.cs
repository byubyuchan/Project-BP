using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrosshairOptionPage : MonoBehaviour
{
    [Header("Settings UI")]
    public Slider opacitySlider;
    public TMP_InputField opacityInput;
    public TMP_Dropdown shapeDropdown;
    public Slider sizeSlider;
    public TMP_InputField sizeInput;
    public Toggle cooldownToggle;

    [Header("Outline Settings UI")]
    public Toggle outlineToggle;
    public Slider outlineThicknessSlider;
    public TMP_InputField outlineThicknessInput;

    private bool isUpdatingUI = false;

    private void Awake()
    {
        if (opacitySlider != null)
            opacitySlider.onValueChanged.AddListener(OnOpacitySliderChanged);

        if (opacityInput != null)
            opacityInput.onEndEdit.AddListener(OnOpacityInputChanged);

        if (shapeDropdown != null)
        {
            shapeDropdown.onValueChanged.AddListener(OnShapeDropdownChanged);
        }

        if (sizeSlider != null)
        {
            sizeSlider.onValueChanged.AddListener(OnSliderSizeChanged);
        }

        if (sizeInput != null)
        {
            sizeInput.onEndEdit.AddListener(OnInputSizeChanged);
        }

        if (cooldownToggle != null)
        {
            cooldownToggle.onValueChanged.AddListener(OnCooldownToggleChanged);
        }

        if (outlineToggle != null)
            outlineToggle.onValueChanged.AddListener(OnOutlineToggleChanged);

        if (outlineThicknessSlider != null)
            outlineThicknessSlider.onValueChanged.AddListener(OnOutlineSliderChanged);

        if (outlineThicknessInput != null)
            outlineThicknessInput.onEndEdit.AddListener(OnOutlineInputChanged);
    }

    private void OnEnable()
    {
        isUpdatingUI = true;

        float savedOpacity = PlayerPrefs.GetFloat("CrosshairOpacity", 1.0f);
        if (opacitySlider != null) opacitySlider.value = savedOpacity * 100f;
        if (opacityInput != null) opacityInput.text = Mathf.RoundToInt(savedOpacity * 100f).ToString();

        int savedShape = PlayerPrefs.GetInt("CrosshairShape", 0);
        if (shapeDropdown != null) shapeDropdown.value = savedShape;

        float savedSize = PlayerPrefs.GetFloat("CrosshairSize", 1.0f);
        if (sizeSlider != null) sizeSlider.value = savedSize;
        if (sizeInput != null) sizeInput.text = savedSize.ToString("F1");

        int savedCooldown = PlayerPrefs.GetInt("CrosshairCooldownVisible", 1);
        if (cooldownToggle != null) cooldownToggle.isOn = (savedCooldown == 1);

        bool isOutlineOn = PlayerPrefs.GetInt("CrosshairOutlineVisible", 1) == 1;
        if (outlineToggle != null) outlineToggle.isOn = isOutlineOn;

        UpdateOutlineInteractable(isOutlineOn);

        float savedThickness = PlayerPrefs.GetFloat("CrosshairOutlineThickness", 1.0f);
        if (outlineThicknessSlider != null) outlineThicknessSlider.value = savedThickness;
        if (outlineThicknessInput != null) outlineThicknessInput.text = savedThickness.ToString("F1");

        isUpdatingUI = false;
    }

    private void OnOpacitySliderChanged(float value)
    {
        if (isUpdatingUI) return;
        isUpdatingUI = true;

        if (opacityInput != null) opacityInput.text = Mathf.RoundToInt(value).ToString();

        SaveAndBroadcastOpacity(value / 100f);

        isUpdatingUI = false;
    }

    private void OnOpacityInputChanged(string text)
    {
        if (isUpdatingUI) return;

        if (float.TryParse(text, out float value))
        {
            isUpdatingUI = true;

            if (opacitySlider != null)
            {
                value = Mathf.Clamp(value, opacitySlider.minValue, opacitySlider.maxValue); // 0 ~ 100
                opacitySlider.value = value;
            }

            if (opacityInput != null) opacityInput.text = Mathf.RoundToInt(value).ToString();

            SaveAndBroadcastOpacity(value / 100f);

            isUpdatingUI = false;
        }
    }

    private void SaveAndBroadcastOpacity(float opacity)
    {
        PlayerPrefs.SetFloat("CrosshairOpacity", opacity);
        PlayerPrefs.Save();
        BaseOptionManager.OnCrosshairOpacityChanged?.Invoke(opacity);
    }

    private void OnShapeDropdownChanged(int index)
    {
        if (isUpdatingUI) return;

        PlayerPrefs.SetInt("CrosshairShape", index);
        PlayerPrefs.Save();

        BaseOptionManager.OnCrosshairShapeChanged?.Invoke(index);
    }

    private void OnSliderSizeChanged(float value)
    {
        if (isUpdatingUI) return;

        isUpdatingUI = true;

        if (sizeInput != null) sizeInput.text = value.ToString("F1");

        SaveAndBroadcastSize(value);

        isUpdatingUI = false;
    }

    private void OnInputSizeChanged(string text)
    {
        if (isUpdatingUI) return;

        if (float.TryParse(text, out float value))
        {
            isUpdatingUI = true;

            if (sizeSlider != null)
            {
                value = Mathf.Clamp(value, sizeSlider.minValue, sizeSlider.maxValue);
                sizeSlider.value = value;
            }

            if (sizeInput != null) sizeInput.text = value.ToString("F1");

            SaveAndBroadcastSize(value);

            isUpdatingUI = false;
        }
    }

    private void SaveAndBroadcastSize(float size)
    {
        PlayerPrefs.SetFloat("CrosshairSize", size);
        PlayerPrefs.Save();

        BaseOptionManager.OnCrosshairSizeChanged?.Invoke(size);
    }

    private void OnCooldownToggleChanged(bool isOn)
    {
        if (isUpdatingUI) return;

        PlayerPrefs.SetInt("CrosshairCooldownVisible", isOn ? 1 : 0);
        PlayerPrefs.Save();

        BaseOptionManager.OnCooldownVisibilityChanged?.Invoke(isOn);
    }

    private void OnOutlineToggleChanged(bool isOn)
    {
        if (isUpdatingUI) return;

        PlayerPrefs.SetInt("CrosshairOutlineVisible", isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateOutlineInteractable(isOn);

        BaseOptionManager.OnOutlineVisibilityChanged?.Invoke(isOn);
    }

    private void UpdateOutlineInteractable(bool isInteractable)
    {
        if (outlineThicknessSlider != null) outlineThicknessSlider.interactable = isInteractable;
        if (outlineThicknessInput != null) outlineThicknessInput.interactable = isInteractable;
    }

    private void OnOutlineSliderChanged(float value)
    {
        if (isUpdatingUI) return;
        isUpdatingUI = true;

        if (outlineThicknessInput != null) outlineThicknessInput.text = value.ToString("F1");

        SaveAndBroadcastOutlineThickness(value);
        isUpdatingUI = false;
    }

    private void OnOutlineInputChanged(string text)
    {
        if (isUpdatingUI) return;

        if (float.TryParse(text, out float value))
        {
            isUpdatingUI = true;

            if (outlineThicknessSlider != null)
            {
                value = Mathf.Clamp(value, outlineThicknessSlider.minValue, outlineThicknessSlider.maxValue);
                outlineThicknessSlider.value = value;
            }

            if (outlineThicknessInput != null) outlineThicknessInput.text = value.ToString("F1");

            SaveAndBroadcastOutlineThickness(value);
            isUpdatingUI = false;
        }
    }

    private void SaveAndBroadcastOutlineThickness(float thickness)
    {
        PlayerPrefs.SetFloat("CrosshairOutlineThickness", thickness);
        PlayerPrefs.Save();
        BaseOptionManager.OnOutlineThicknessChanged?.Invoke(thickness);
    }
}