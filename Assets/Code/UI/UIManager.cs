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

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                return;
            }
        }

        // Remove panel data that is null or inactive by the time Player clicks close button
        panelStack.RemoveAll(p => p.panel == null || !p.panel.activeSelf);

        // close the top panel if there is any panel in the stack
        if (panelStack.Count > 0)
        {
            PanelData topPanel = panelStack[panelStack.Count - 1];
            topPanel.closeAction?.Invoke();
        }
        else
        {
            // If there is no panel in the stack, call onEmptyEsc action if it exists
            onEmptyEsc?.Invoke();
        }
    }

    public void ShowPanel(GameObject panelObj, System.Action closeFunc)
    {
        panelObj.SetActive(true);

        panelStack.RemoveAll(p => p.panel == panelObj);

        panelStack.Add(new PanelData { panel = panelObj, closeAction = closeFunc });
    }
}