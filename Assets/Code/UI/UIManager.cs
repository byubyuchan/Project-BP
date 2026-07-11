using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("Input Settings")]
    public InputActionReference escapeAction;

    public static UIManager Instance;

    private float lastEscTime = 0f;

    private class PanelData
    {
        public GameObject panel;
        public System.Action closeAction;
    }

    private List<PanelData> panelStack = new List<PanelData>();

    public System.Action onEmptyEsc;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        if (escapeAction != null)
        {
            escapeAction.action.Enable();
            escapeAction.action.performed += OnEscapePressed;
        }
    }

    private void OnDisable()
    {
        if (escapeAction != null)
        {
            escapeAction.action.performed -= OnEscapePressed;
            escapeAction.action.Disable();
        }
    }

    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        OpenEscapeUI();
    }

    public void OpenEscapeUI()
    {
        if (Time.realtimeSinceStartup - lastEscTime < 0.15f) return;
        lastEscTime = Time.realtimeSinceStartup;

        // PC 옵션 패널을 입력 필드와 일반 UI 스택보다 먼저 찾아 닫는다.
        BaseOptionManager openOption = FindOpenOptionManager();
        if (openOption != null)
        {
            openOption.HandleEscape();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }
        }

        // 부모 패널이 먼저 꺼져도 자체적으로 활성화된 모달을 스택에서 제거하지 않는다.
        panelStack.RemoveAll(p => p.panel == null || !p.panel.activeSelf);

        // 스택에 패널이 있으면 가장 위에 있는 패널부터 닫는다.
        if (panelStack.Count > 0)
        {
            PanelData topPanel = panelStack[panelStack.Count - 1];
            topPanel.closeAction?.Invoke();
        }
        else
        {
            // 닫을 패널이 없을 때만 기본 ESC 동작을 실행한다.
            onEmptyEsc?.Invoke();
        }
    }

    // PC에서는 실제로 켜진 PCOptionManager를 직접 찾고, 다른 플랫폼에서는 활성 옵션 참조를 사용한다.
    private BaseOptionManager FindOpenOptionManager()
    {
        if (SystemInfo.deviceType != DeviceType.Handheld)
        {
            PCOptionManager pcOption = Object.FindFirstObjectByType<PCOptionManager>();
            if (pcOption != null && pcOption.IsOptionPanelOpen()) return pcOption;
        }

        BaseOptionManager activeOption = BaseOptionManager.ActiveInstance;
        if (activeOption != null && activeOption.IsOptionPanelOpen()) return activeOption;

        return null;
    }

    public void ShowPanel(GameObject panelObj, System.Action closeFunc)
    {
        panelObj.SetActive(true);

        panelStack.RemoveAll(p => p.panel == panelObj);

        panelStack.Add(new PanelData { panel = panelObj, closeAction = closeFunc });
    }

    // 이미 활성화된 패널도 ESC 스택에 직접 등록할 수 있게 한다.
    public void RegisterPanel(GameObject panelObj, System.Action closeFunc)
    {
        if (panelObj == null) return;

        // (추가) 같은 패널은 한 번만 등록하고 가장 최근에 열린 순서로 올린다.
        panelStack.RemoveAll(p => p.panel == panelObj);
        panelStack.Add(new PanelData { panel = panelObj, closeAction = closeFunc });
    }

    // 비활성화된 패널을 ESC 스택에서 즉시 제거한다.
    public void UnregisterPanel(GameObject panelObj)
    {
        if (panelObj == null) return;

        panelStack.RemoveAll(p => p.panel == panelObj);
    }
}
