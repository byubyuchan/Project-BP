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
    // 기존 계정이어도 게스트 로그인이라면 닉네임 입력 화면을 거치게 한다.
    private bool isGuestLogin = false;

    private const int MinNicknameLength = 3;
    private const int MaxNicknameLength = 25;

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
            PlayFabAuthManager.Instance.OnLoginSuccessExistingUser += HandleExistingUserLogin;
            PlayFabAuthManager.Instance.OnNicknameSetSuccess += LoadNextScene;
            PlayFabAuthManager.Instance.OnLoginFailedEvent += ShowPopup;
            PlayFabAuthManager.Instance.OnNicknameSetFailed += HandleNicknameSetFailure;
        }
    }

    private void OnDestroy()
    {
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.OnLoginSuccessNewUser -= ShowNicknamePanel;
            PlayFabAuthManager.Instance.OnLoginSuccessExistingUser -= HandleExistingUserLogin;
            PlayFabAuthManager.Instance.OnNicknameSetSuccess -= LoadNextScene;
            PlayFabAuthManager.Instance.OnLoginFailedEvent -= ShowPopup;
            PlayFabAuthManager.Instance.OnNicknameSetFailed -= HandleNicknameSetFailure;
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

        if (nicknamePanel != null && nicknamePanel.activeInHierarchy && nicknameInput != null)
        {
            nicknameInput.Select();
            nicknameInput.ActivateInputField();
        }
    }

    public void OnGuestLoginButtonClicked()
    {
        // 기존에 사용한 기기의 게스트 계정이어도 이번 접속에서 닉네임을 다시 입력받는다.
        isGuestLogin = true;

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.LoginWithEditor();
        }
    }

    // 구글 등 정식 로그인 버튼용 (현재는 게스트와 동일하게 처리하거나 추후 구글 플러그인 연동부)
    public void OnLoginButtonClicked()
    {
        isGuestLogin = false;

        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.LoginWithEditor();
        }
    }

    public void OnNicknameSubmit()
    {
        // 공백을 제거한 실제 닉네임을 기준으로 길이를 검사한다.
        if (nicknameInput == null)
        {
            ShowPopup("닉네임 입력창을 찾지 못했습니다.");
            return;
        }

        string nickname = nicknameInput.text.Trim();

        // 2글자 이하의 닉네임은 서버 요청 전에 차단하고 입력 화면을 유지한다.
        if (nickname.Length < MinNicknameLength)
        {
            ShowPopup($"닉네임은 최소 {MinNicknameLength}글자 이상 입력해 주세요.");
            nicknameInput.Select();
            return;
        }

        // PlayFab의 최대 표시 이름 길이를 초과한 닉네임도 서버 요청 전에 차단한다.
        if (nickname.Length > MaxNicknameLength)
        {
            ShowPopup($"닉네임은 최대 {MaxNicknameLength}글자까지 입력할 수 있습니다.");
            nicknameInput.Select();
            return;
        }

        // (변경) 검사와 공백 제거가 끝난 닉네임만 PlayFab에 저장 요청한다.
        nicknameInput.text = nickname;
        if (PlayFabAuthManager.Instance != null)
        {
            PlayFabAuthManager.Instance.SetPlayerNickname(nickname);
        }
    }

    private void ShowNicknamePanel()
    {
        if (loginButtonGroup != null) loginButtonGroup.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(true);
        if (nicknameInput != null) nicknameInput.Select();
    }

    private void HandleExistingUserLogin()
    {
        if (isGuestLogin)
        {
            ShowNicknamePanel();
            return;
        }

        LoadNextScene();
    }

    // 닉네임 저장에 실패해도 씬을 이동하지 않고 입력 화면을 유지한다.
    private void HandleNicknameSetFailure(string message)
    {
        ShowPopup(message);
        if (nicknamePanel != null) nicknamePanel.SetActive(true);
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