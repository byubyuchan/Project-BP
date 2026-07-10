using UnityEngine;
using UnityEngine.UI;

// 미리보기 UI와 인게임 UI 양쪽 모두에 붙어서 '디자인'만 100% 동기화하는 스크립트
public class CrosshairVisualSync : MonoBehaviour
{
    [Header("UI 연결 (스샷 구조에 맞춤)")]
    [Tooltip("크기가 조절될 최상위 부모 (CrossHair)")]
    public RectTransform sizeTargetRect;

    [Tooltip("모양 옵션에 따라 껐다 켤 조준선 오브젝트들 (0: Dot_Small, 1: Dot_Large 등)")]
    public GameObject[] shapeObjects;

    [Tooltip("쿨타임 옵션에 따라 껐다 켤 원형 오브젝트 (Circle)")]
    public GameObject cooldownObject;

    [Tooltip("색상과 투명도가 적용될 모든 이미지 (Dot_Small, Dot_Large, Circle 전부 넣기)")]
    public Image[] allColorImages;

    [Tooltip("두께와 On/Off가 적용될 모든 아웃라인 컴포넌트")]
    public Outline[] allOutlines;

    // 내부 저장용
    private Color baseColor = Color.white;
    private float baseOpacity = 1.0f;
    private float cooldownAlphaMultiplier = 1.0f;

    void OnEnable()
    {
        // 1. 켜질 때 PlayerPrefs에서 현재 설정값 싹 다 긁어오기
        string savedHex = PlayerPrefs.GetString("CrosshairColorHex", "#000000");
        if (ColorUtility.TryParseHtmlString(savedHex, out Color savedColor)) baseColor = savedColor;

        baseOpacity = PlayerPrefs.GetFloat("CrosshairOpacity", 1.0f);
        UpdateSize(PlayerPrefs.GetFloat("CrosshairSize", 1.0f));
        UpdateOutlineVisibility(PlayerPrefs.GetInt("CrosshairOutlineVisible", 1) == 1);
        UpdateOutlineThickness(PlayerPrefs.GetFloat("CrosshairOutlineThickness", 1.0f));
        UpdateShape(PlayerPrefs.GetInt("CrosshairShape", 0));
        UpdateCooldownVisibility(PlayerPrefs.GetInt("CrosshairCooldownVisible", 1) == 1);

        ApplyColorAndOpacity();

        // 2. 옵션창 매니저 라디오 구독 켜기!
        BaseOptionManager.OnCrosshairColorChanged += UpdateColor;
        BaseOptionManager.OnCrosshairOpacityChanged += UpdateOpacity;
        BaseOptionManager.OnCrosshairSizeChanged += UpdateSize;
        BaseOptionManager.OnOutlineVisibilityChanged += UpdateOutlineVisibility;
        BaseOptionManager.OnOutlineThicknessChanged += UpdateOutlineThickness;
        BaseOptionManager.OnCrosshairShapeChanged += UpdateShape;
        BaseOptionManager.OnCooldownVisibilityChanged += UpdateCooldownVisibility;
    }

    void OnDisable()
    {
        BaseOptionManager.OnCrosshairColorChanged -= UpdateColor;
        BaseOptionManager.OnCrosshairOpacityChanged -= UpdateOpacity;
        BaseOptionManager.OnCrosshairSizeChanged -= UpdateSize;
        BaseOptionManager.OnOutlineVisibilityChanged -= UpdateOutlineVisibility;
        BaseOptionManager.OnOutlineThicknessChanged -= UpdateOutlineThickness;
        BaseOptionManager.OnCrosshairShapeChanged -= UpdateShape;
        BaseOptionManager.OnCooldownVisibilityChanged -= UpdateCooldownVisibility;
    }

    // ================= [방송 수신 함수들] =================

    private void UpdateColor(Color newColor) { baseColor = newColor; ApplyColorAndOpacity(); }
    private void UpdateOpacity(float newOpacity) { baseOpacity = newOpacity; ApplyColorAndOpacity(); }

    public void ApplyCooldownAlphaMode(float multiplier)
    {
        if (Mathf.Approximately(cooldownAlphaMultiplier, multiplier)) return;
        cooldownAlphaMultiplier = multiplier;
        ApplyColorAndOpacity();
    }

    private void ApplyColorAndOpacity()
    {
        Color finalColor = baseColor;
        finalColor.a = baseOpacity * cooldownAlphaMultiplier;

        foreach (var img in allColorImages)
        {
            if (img != null) img.color = finalColor;
        }

        foreach (var outline in allOutlines)
        {
            if (outline != null)
            {
                Color outlineColor = outline.effectColor;
                outlineColor.a = baseOpacity * cooldownAlphaMultiplier;
                outline.effectColor = outlineColor;
            }
        }
    }

    private void UpdateSize(float size)
    {
        if (sizeTargetRect != null) sizeTargetRect.localScale = Vector3.one * size;
    }

    private void UpdateOutlineVisibility(bool isVisible)
    {
        foreach (var outline in allOutlines)
        {
            if (outline != null) outline.enabled = isVisible;
        }
    }

    private void UpdateOutlineThickness(float thickness)
    {
        foreach (var outline in allOutlines)
        {
            if (outline != null) outline.effectDistance = new Vector2(thickness, -thickness);
        }
    }
    private void UpdateShape(int index)
    {
        if (shapeObjects == null) return;

        for (int i = 0; i < shapeObjects.Length; i++)
        {
            if (shapeObjects[i] != null)
            {
                shapeObjects[i].SetActive(i == index);
            }
        }
    }

    private void UpdateCooldownVisibility(bool isVisible)
    {
        if (cooldownObject != null) cooldownObject.SetActive(isVisible);
    }
}