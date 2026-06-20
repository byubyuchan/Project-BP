using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;

public class TitleController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject pressAnyButtonText;
    public GameObject loginButtonGroup;

    [Header("Popup UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;

    [Header("Guest Login UI")]
    public GameObject nicknamePanel;
    public TMP_InputField nicknameInput;

    [Header("Scene Management")]
    public string nextSceneName = "Lobby";

    private bool isWaitingForInput = true;

    private void Start()
    {
        if (pressAnyButtonText != null) pressAnyButtonText.SetActive(true);
        if (loginButtonGroup != null) loginButtonGroup.SetActive(false);
        if (popupPanel != null) popupPanel.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
    }

    private void Update()
    {
        if (isWaitingForInput)
        {
            bool isAnyButtonPressed = false;

            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                isAnyButtonPressed = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isAnyButtonPressed = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                isAnyButtonPressed = true;
            }

            if (isAnyButtonPressed)
            {
                TransitionToLogin();
            }
        }
    }

    private void TransitionToLogin()
    {
        isWaitingForInput = false;
        if (pressAnyButtonText != null) pressAnyButtonText.SetActive(false);
        if (loginButtonGroup != null) loginButtonGroup.SetActive(true);
    }

    private void ShowPopup(string message)
    {
        if (popupPanel != null && popupText != null)
        {
            popupText.text = message;
            popupPanel.SetActive(true);
        }
    }

    public void OnClosePopupClicked()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    public void OnGuestLoginButtonClicked()
    {
        if (loginButtonGroup != null) loginButtonGroup.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(true);
        if (nicknameInput != null) nicknameInput.Select();
    }

    public void OnNicknameSubmit()
    {
        if (string.IsNullOrWhiteSpace(nicknameInput.text))
        {
            ShowPopup("Please enter a nickname");
            nicknameInput.Select();
            return;
        }

        PhotonNetwork.NickName = nicknameInput.text;
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnLoginButtonClicked()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnTestLoginFailedButtonClicked()
    {
        ShowPopup("Login failed");
    }
}