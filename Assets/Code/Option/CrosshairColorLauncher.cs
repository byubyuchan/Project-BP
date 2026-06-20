using UnityEngine;
using UnityEngine.UI;

public class CrosshairColorLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject colorPickerPopup;

    private Image buttonImage;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        GetComponent<Button>().onClick.AddListener(OpenPopup);
    }

    private void OnEnable()
    {
        string savedHex = PlayerPrefs.GetString("CrosshairColorHex", "#000000");
        if (ColorUtility.TryParseHtmlString(savedHex, out Color savedColor))
        {
            buttonImage.color = savedColor;
        }

        BaseOptionManager.OnCrosshairColorChanged += UpdateColor;
    }

    private void OnDisable()
    {
        BaseOptionManager.OnCrosshairColorChanged -= UpdateColor;
    }

    private void UpdateColor(Color newColor)
    {
        buttonImage.color = newColor;
    }

    private void OpenPopup()
    {
        if (colorPickerPopup != null)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPanel(colorPickerPopup, ClosePopup);
            }
            else
            {
                colorPickerPopup.SetActive(true);
            }
        }
    }

    private void ClosePopup()
    {
        if (colorPickerPopup != null)
        {
            colorPickerPopup.SetActive(false);
        }
    }
}