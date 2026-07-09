using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundOptionManager : MonoBehaviour
{
    [Header("BGM UI")]
    public Slider bgmSlider;
    public TMP_InputField bgmInput;

    [Header("SFX UI")]
    public Slider sfxSlider;
    public TMP_InputField sfxInput;

    void OnEnable()
    {
        // 1. UI 켜질 때 저장된 값 불러와서 슬라이더/인풋에 세팅
        float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        if (bgmSlider != null) bgmSlider.value = savedBGM;
        if (bgmInput != null) bgmInput.text = savedBGM.ToString("F2");

        if (sfxSlider != null) sfxSlider.value = savedSFX;
        if (sfxInput != null) sfxInput.text = savedSFX.ToString("F2");

        // 2. 값 변경 리스너 달기 (OnEnable에서 다는 게 안전함)
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMSliderChanged);
        if (bgmInput != null) bgmInput.onValueChanged.AddListener(OnBGMInputChanged);

        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        if (sfxInput != null) sfxInput.onValueChanged.AddListener(OnSFXInputChanged);
    }

    void OnDisable()
    {
        // 리스너 해제 (메모리 누수 방지)
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveAllListeners();
        if (bgmInput != null) bgmInput.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
        if (sfxInput != null) sfxInput.onValueChanged.RemoveAllListeners();
    }

    // ==========================================
    // BGM 컨트롤
    // ==========================================
    private void OnBGMSliderChanged(float value)
    {
        if (bgmInput != null) bgmInput.text = value.ToString("F2");
        ApplyBGM(value);
    }

    private void OnBGMInputChanged(string text)
    {
        if (float.TryParse(text, out float value))
        {
            value = Mathf.Clamp(value, 0f, 1f); // 볼륨은 0~1 사이!
            if (bgmSlider != null) bgmSlider.value = value;
            ApplyBGM(value);
        }
    }

    private void ApplyBGM(float value)
    {
        PlayerPrefs.SetFloat("BGMVolume", value);
        PlayerPrefs.Save();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetBGMVolume(value);
            AudioManager.instance.SetAmbVolume(value);
        }
    }

    // ==========================================
    // SFX 컨트롤
    // ==========================================
    private void OnSFXSliderChanged(float value)
    {
        if (sfxInput != null) sfxInput.text = value.ToString("F2");
        ApplySFX(value);
    }

    private void OnSFXInputChanged(string text)
    {
        if (float.TryParse(text, out float value))
        {
            value = Mathf.Clamp(value, 0f, 1f);
            if (sfxSlider != null) sfxSlider.value = value;
            ApplySFX(value);
        }
    }

    private void ApplySFX(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
    }
}