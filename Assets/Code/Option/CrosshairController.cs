using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public static CrosshairController instance;

    [Header("Crosshair Elements")]
    public Image[] shapeImages;
    public Image cooldownRingImage;

    private Color currentColor = Color.white;
    private float currentOpacity = 1.0f;
    private bool isCooldownOptionOn = true;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        currentOpacity = PlayerPrefs.GetFloat("CrosshairOpacity", 1.0f);

        string savedHex = PlayerPrefs.GetString("CrosshairColorHex", "#000000");
        if (ColorUtility.TryParseHtmlString(savedHex, out Color savedColor)) ApplyColor(savedColor);

        int savedShape = PlayerPrefs.GetInt("CrosshairShape", 0);
        ApplyShape(savedShape);

        isCooldownOptionOn = PlayerPrefs.GetInt("CrosshairCooldownVisible", 1) == 1;
        ApplyCooldownVisibility(isCooldownOptionOn);

        float savedSize = PlayerPrefs.GetFloat("CrosshairSize", 1.0f);
        ApplySize(savedSize);

        bool savedOutline = PlayerPrefs.GetInt("CrosshairOutlineVisible", 1) == 1;
        float savedThickness = PlayerPrefs.GetFloat("CrosshairOutlineThickness", 1.0f);

        ApplyOutlineVisibility(savedOutline);
        ApplyOutlineThickness(savedThickness);
    }

    private void OnEnable()
    {
        BaseOptionManager.OnCrosshairColorChanged += ApplyColor;
        BaseOptionManager.OnCrosshairOpacityChanged += ApplyOpacity;
        BaseOptionManager.OnCrosshairShapeChanged += ApplyShape;
        BaseOptionManager.OnCooldownVisibilityChanged += ApplyCooldownVisibility;
        BaseOptionManager.OnCrosshairSizeChanged += ApplySize;
        BaseOptionManager.OnOutlineVisibilityChanged += ApplyOutlineVisibility;
        BaseOptionManager.OnOutlineThicknessChanged += ApplyOutlineThickness;
    }

    private void OnDisable()
    {
        BaseOptionManager.OnCrosshairColorChanged -= ApplyColor;
        BaseOptionManager.OnCrosshairOpacityChanged -= ApplyOpacity;
        BaseOptionManager.OnCrosshairShapeChanged -= ApplyShape;
        BaseOptionManager.OnCooldownVisibilityChanged -= ApplyCooldownVisibility;
        BaseOptionManager.OnCrosshairSizeChanged -= ApplySize;
        BaseOptionManager.OnOutlineVisibilityChanged -= ApplyOutlineVisibility;
        BaseOptionManager.OnOutlineThicknessChanged -= ApplyOutlineThickness;
    }

    private void ApplyColor(Color newColor)
    {
        currentColor = newColor;
        UpdateColorsWithOpacity();
    }

    private void ApplyOpacity(float opacity)
    {
        currentOpacity = opacity;
        UpdateColorsWithOpacity();
    }

    private void UpdateColorsWithOpacity()
    {
        Color finalColor = currentColor;
        finalColor.a = currentOpacity;

        if (shapeImages != null)
        {
            foreach (var img in shapeImages)
            {
                if (img != null) img.color = finalColor;
            }
        }

        if (cooldownRingImage != null)
        {
            cooldownRingImage.color = finalColor;
        }
    }

    private void ApplyShape(int shapeIndex)
    {
        if (shapeImages != null)
        {
            for (int i = 0; i < shapeImages.Length; i++)
            {
                if (shapeImages[i] != null) shapeImages[i].gameObject.SetActive(i == shapeIndex);
            }
        }
    }

    private void ApplySize(float size)
    {
        this.transform.localScale = new Vector3(size, size, 1f);
    }

    private void ApplyCooldownVisibility(bool isOn)
    {
        isCooldownOptionOn = isOn;

        if (cooldownRingImage != null)
        {
            cooldownRingImage.gameObject.SetActive(isOn);
        }
    }

    private void ApplyOutlineVisibility(bool isOn)
    {
        if (shapeImages != null)
        {
            foreach (var img in shapeImages)
            {
                if (img != null)
                {
                    Outline outline = img.GetComponent<Outline>();
                    if (outline != null) outline.enabled = isOn;
                }
            }
        }

        if (cooldownRingImage != null)
        {
            Outline outline = cooldownRingImage.GetComponent<Outline>();
            if (outline != null) outline.enabled = isOn;
        }
    }

    private void ApplyOutlineThickness(float thickness)
    {
        if (shapeImages != null)
        {
            foreach (var img in shapeImages)
            {
                if (img != null)
                {
                    Outline outline = img.GetComponent<Outline>();
                    if (outline != null) outline.effectDistance = new Vector2(thickness, thickness);
                }
            }
        }

        if (cooldownRingImage != null)
        {
            Outline outline = cooldownRingImage.GetComponent<Outline>();
            if (outline != null) outline.effectDistance = new Vector2(thickness, thickness);
        }
    }
}
