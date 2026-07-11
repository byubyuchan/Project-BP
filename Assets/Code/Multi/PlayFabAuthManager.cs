using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System;
using Photon.Pun;

public class PlayFabAuthManager : MonoBehaviour
{
    public static PlayFabAuthManager Instance;

    public Action OnLoginSuccessExistingUser;
    public Action OnLoginSuccessNewUser;
    public Action<string> OnLoginFailedEvent;
    public Action OnNicknameSetSuccess;
    // (추가) 닉네임 저장 실패를 로그인 실패와 구분해서 UI에 전달한다.
    public Action<string> OnNicknameSetFailed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 외부(TitleController)에서 버튼 누를 때 호출할 함수
    public void LoginWithEditor()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log($"<color=green>[PlayFab 로그인 성공]</color> ID: {result.PlayFabId}");

        if (result.NewlyCreated)
        {
            // 신규 가입자면 UI 쪽에 "닉네임 창 띄워!" 하고 신호 쏘기
            OnLoginSuccessNewUser?.Invoke();
        }
        else
        {
            // 기존 유저면 DB에서 닉네임 가져오기
            GetPlayerProfile(result.PlayFabId);
        }
    }

    private void GetPlayerProfile(string playFabId)
    {
        var request = new GetPlayerProfileRequest { PlayFabId = playFabId };
        PlayFabClientAPI.GetPlayerProfile(request, result =>
        {
            if (result.PlayerProfile != null && !string.IsNullOrEmpty(result.PlayerProfile.DisplayName))
            {
                PhotonNetwork.NickName = result.PlayerProfile.DisplayName;
            }
            // 닉네임 세팅 끝났으니 "로비로 넘어가!" 하고 신호 쏘기
            OnLoginSuccessExistingUser?.Invoke();
        }, OnLoginFailure);
    }

    public void SetPlayerNickname(string nickname)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = nickname };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, result =>
        {
            PhotonNetwork.NickName = result.DisplayName;
            OnNicknameSetSuccess?.Invoke(); // 닉네임 저장 성공 신호
        }, OnNicknameSetFailure);
    }
    private void OnNicknameSetFailure(PlayFabError error)
    {
        Debug.LogError($"<color=red>[PlayFab 닉네임 설정 오류]</color> {error.GenerateErrorReport()}");

        if (error.Error == PlayFabErrorCode.NameNotAvailable)
        {
            OnNicknameSetFailed?.Invoke("이미 사용 중인 닉네임입니다. 다른 닉네임을 입력해 주세요.");
            return;
        }

        OnNicknameSetFailed?.Invoke("닉네임 저장에 실패했습니다. 잠시 후 다시 시도해 주세요.");
    }
    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogError($"<color=red>[PlayFab 에러]</color> {error.GenerateErrorReport()}");
        OnLoginFailedEvent?.Invoke("서버 접속에 실패했습니다.");
    }

    // =================================================================
    // 진짜 구글 계정 로그인 (모바일 빌드나 실제 토큰 연동용)
    // =================================================================
    public void LoginWithGoogle(string serverAuthCode)
    {
        Debug.Log("<color=yellow>[PlayFab]</color> 구글 계정 로그인을 시도합니다.");

        var request = new LoginWithGoogleAccountRequest
        {
            ServerAuthCode = serverAuthCode,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithGoogleAccount(request, OnLoginSuccess, OnLoginFailure);
    }
}