using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseOptionManager : MonoBehaviour
{
    [Header("Panel Control")]
    public GameObject optionPanel;
    public Button closeButton;

    [Header("Tabs and Pages")]
    public Button[] tabButtons; // 0: Graphics, 1: Control, 2: Crosshair
    public GameObject[] pages;

    [Header("Common Graphics Settings")]
    public TMP_Dropdown qualityDropdown;
    public Button applyGraphicsButton;

    [Header("Unsaved Warning UI")]
    public GameObject unsavedWarningPanel;
    public Button popupApplyButton;
    public Button popupDiscardButton;
    public Button popupCancelButton;

    public static System.Action<Color> OnCrosshairColorChanged;
    public static System.Action<float> OnCrosshairOpacityChanged;
    public static System.Action<int> OnCrosshairShapeChanged;
    public static System.Action<float> OnCrosshairSizeChanged;
    public static System.Action<bool> OnCooldownVisibilityChanged;
    public static System.Action<bool> OnOutlineVisibilityChanged;
    public static System.Action<float> OnOutlineThicknessChanged;

    protected bool hasUnsavedChanges = false;
    protected int savedQualityIndex;
    protected System.Action pendingAction = null;

    protected virtual void Start()
    {
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                // C# 클로저(Closure) 문제 방지를 위해 i값을 지역 변수로 복사
                int index = i;
                tabButtons[i].onClick.AddListener(() => AttemptShowPage(index));
            }
        }

        closeButton.onClick.AddListener(AttemptCloseOptionPanel);

        applyGraphicsButton.onClick.AddListener(ApplyGraphicsSettings);
        qualityDropdown.onValueChanged.AddListener(delegate { hasUnsavedChanges = true; });

        unsavedWarningPanel.SetActive(false);

        popupApplyButton.onClick.AddListener(OnPopupApply);
        popupDiscardButton.onClick.AddListener(OnPopupDiscard);
        popupCancelButton.onClick.AddListener(OnPopupCancel);

        //optionPanel.SetActive(false);

        savedQualityIndex = QualitySettings.GetQualityLevel();
        qualityDropdown.value = savedQualityIndex;

        InitPlatformGraphics();
        InitPlatformControls();

        hasUnsavedChanges = false;
    }

    public void OpenOptionPanel()
    {
        UIVisibility visibility = GetComponent<UIVisibility>();
        if (visibility != null)
        {
            bool isMobile = SystemInfo.deviceType == DeviceType.Handheld;
            if (isMobile && !visibility.showOnMobile) return;
            if (!isMobile && !visibility.showOnPC) return;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowPanel(optionPanel, AttemptCloseOptionPanel);
        else
            optionPanel.SetActive(true);

        ShowPage(0);
    }

    public void CloseOptionPanel()
    {
        optionPanel.SetActive(false);
    }

    public void ShowPage(int pageIndex)
    {
        if (pages == null) return;

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == pageIndex);
            }
        }
    }

    protected void ApplyGraphicsSettings()
    {
        QualitySettings.SetQualityLevel(qualityDropdown.value);
        savedQualityIndex = qualityDropdown.value;

        ApplyPlatformGraphics();

        hasUnsavedChanges = false;
    }

    private void AttemptShowPage(int targetPageIndex)
    {
        CheckUnsavedChanges(() => ShowPage(targetPageIndex));
    }

    public void AttemptCloseOptionPanel()
    {
        CheckUnsavedChanges(CloseOptionPanel);
    }

    private void CheckUnsavedChanges(System.Action actionToPerform)
    {
        bool isGraphicsPageActive = pages != null && pages.Length > 0 && pages[0] != null && pages[0].activeSelf;

        if (hasUnsavedChanges && isGraphicsPageActive)
        {
            pendingAction = actionToPerform;
            if (UIManager.Instance != null)
                UIManager.Instance.ShowPanel(unsavedWarningPanel, OnPopupCancel);
            else
                unsavedWarningPanel.SetActive(true);
        }
        else
        {
            actionToPerform();
        }
    }

    private void OnPopupApply()
    {
        ApplyGraphicsSettings();
        unsavedWarningPanel.SetActive(false);
        pendingAction?.Invoke();
    }

    private void OnPopupDiscard()
    {
        qualityDropdown.value = savedQualityIndex;

        RevertPlatformGraphics();

        hasUnsavedChanges = false;
        unsavedWarningPanel.SetActive(false);
        pendingAction?.Invoke();
    }

    private void OnPopupCancel()
    {
        unsavedWarningPanel.SetActive(false);
        pendingAction = null;
    }

    protected abstract void InitPlatformGraphics();
    protected abstract void ApplyPlatformGraphics();
    protected abstract void RevertPlatformGraphics();
    protected abstract void InitPlatformControls();
}