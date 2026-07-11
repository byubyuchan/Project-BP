using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseOptionManager : MonoBehaviour
{
    // (추가) ESC 입력에서 시스템 메뉴보다 먼저 처리할 현재 활성 옵션 패널을 저장한다.
    public static BaseOptionManager ActiveInstance { get; private set; }

    [Header("Panel Control")]
    public GameObject optionPanel;
    public Button closeButton;

    [Header("Tabs and Pages")]
    public Button[] tabButtons; // (변경) 0: 그래픽, 1: 조작, 2: 조준선
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
                // (변경) C# 클로저 문제를 방지하기 위해 반복문의 인덱스를 지역 변수로 복사한다.
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

        // 현재 플랫폼의 옵션 패널 하나만 ESC 스택에 등록한다.
        ActiveInstance = this;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowPanel(optionPanel, AttemptCloseOptionPanel);
        else
            optionPanel.SetActive(true);

        ShowPage(0);
    }

    public void CloseOptionPanel()
    {
        // 옵션 배경과 함께 그래픽, 컨트롤, 조준선 등 모든 하위 페이지를 종료한다.
        if (pages != null)
        {
            foreach (GameObject page in pages)
            {
                if (page != null) page.SetActive(false);
            }
        }

        if (unsavedWarningPanel != null) unsavedWarningPanel.SetActive(false);
        pendingAction = null;

        // 옵션 패널을 닫는 즉시 ESC 스택에서도 제거한다.
        if (UIManager.Instance != null) UIManager.Instance.UnregisterPanel(optionPanel);

        optionPanel.SetActive(false);

        // 현재 옵션이 실제로 닫힌 뒤에만 최상위 모달 참조를 해제한다.
        if (ActiveInstance == this) ActiveInstance = null;
    }

    // ESC 입력이 활성 옵션 패널과 미저장 경고창을 우선 처리하게 한다.
    public void HandleEscape()
    {
        if (unsavedWarningPanel != null && unsavedWarningPanel.activeInHierarchy)
        {
            OnPopupCancel();
            return;
        }

        AttemptCloseOptionPanel();
    }

    // 하위 페이지 상태와 관계없이 옵션 패널 자체가 켜져 있을 때만 열린 것으로 판단한다.
    public bool IsOptionPanelOpen()
    {
        return optionPanel != null && optionPanel.activeSelf;
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
