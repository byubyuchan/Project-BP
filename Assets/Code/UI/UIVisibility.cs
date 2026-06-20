using UnityEngine;
using UnityEngine.InputSystem;

public class UIVisibility : MonoBehaviour
{
    [Header("Base Platform Setting")]
    public bool showOnPC = true;
    public bool showOnMobile = true;

    [Header("Gamepad Setting")]
    [Tooltip("Hide this UI when a gamepad is connected")]
    public bool hideIfGamepadConnected = true;

    private CanvasGroup canvasGroup;
    private int lastState = -1;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Update()
    {
        // Check platform If PC or Mobile
        bool isMobile = SystemInfo.deviceType == DeviceType.Handheld;

        // Check if a gamepad is connected
        bool isGamepadConnected = false;

        if (Gamepad.current != null)
        {
            string deviceName = Gamepad.current.name.ToLower();
            string displayName = Gamepad.current.displayName.ToLower();

            if (!deviceName.Contains("wacom") && !deviceName.Contains("vjoy") && deviceName != "gamepad" && displayName != "gamepad")
            {
                isGamepadConnected = true;
            }
        }

        // Determine if the UI should be shown based on platform and gamepad status
        bool shouldShow = true;

        if (isGamepadConnected && hideIfGamepadConnected)
        {
            // Hide the UI if a gamepad is connected and the setting is enabled
            shouldShow = false;
        }
        else
        {
            // Follow the original platform settings when no gamepad is connected
            if (isMobile && !showOnMobile) shouldShow = false;
            if (!isMobile && !showOnPC) shouldShow = false;
        }

        int currentState = shouldShow ? 1 : 0;

        // Only update the canvas group if the visibility state has changed
        if (lastState != currentState)
        {
            lastState = currentState;
            canvasGroup.alpha = shouldShow ? 1 : 0; // visible or invisible
            canvasGroup.interactable = shouldShow; // clickable or not
            canvasGroup.blocksRaycasts = shouldShow; // block or ignore raycasts
        }
    }
}