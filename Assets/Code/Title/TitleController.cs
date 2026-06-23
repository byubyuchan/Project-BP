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

        // PlayFab 매니저의 신호를 내 UI 함수들이랑 연결 (구독)
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccessNewUser += ShowNicknamePanel;
            PlayFabAuthManager.Instance.OnLoginSuccessExistingUser += LoadNextScene;
            PlayFabAuthManager.Instance.OnNicknameSetSuccess += LoadNextScene;
            PlayFabAuthManager.Instance.OnLoginFailedEvent += ShowPopup;
        }
    }

    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccessNewUser -= ShowNicknamePanel;
            PlayFabAuthManager.Instance.OnLoginSuccessExistingUser -= LoadNextScene;
            PlayFabAuthManager.Instance.OnNicknameSetSuccess -= LoadNextScene;
            PlayFabAuthManager.Instance.OnLoginFailedEvent -= ShowPopup;
        }
    }

    private void Update()
    {
        if (isWaitingForInput)
        {
            bool isAnyButtonPressed = false;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) isAnyButtonPressed = true;
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isAnyButtonPressed = true;
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) isAnyButtonPressed = true;

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
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void OnGuestLoginButtonClicked()
    {
        // 1. 플레이팹에 게스트 로그인을 요청한다!
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.LoginWithEditor();
        }
    }

    // 구글 등 정식 로그인 버튼용 (현재는 게스트와 동일하게 처리하거나 추후 구글 플러그인 연동부)
    public void OnLoginButtonClicked()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.LoginWithEditor();
        }
    }

    public void OnNicknameSubmit()
    {
        if (string.IsNullOrWhiteSpace(nicknameInput.text))
        {
            ShowPopup("Please enter a nickname");
            nicknameInput.Select();
            return;
        }

        // 플레이팹 DB에 내 이름 변경 요청!
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.SetPlayerNickname(nicknameInput.text);
        }
    }

    private void ShowNicknamePanel()
    {
        if (loginButtonGroup != null) loginButtonGroup.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(true);
        if (nicknameInput != null) nicknameInput.Select();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnTestLoginFailedButtonClicked()
    {
        ShowPopup("Login failed");
    }
}