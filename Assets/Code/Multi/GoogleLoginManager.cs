using Google;
using System.Threading.Tasks;
using UnityEngine;

public class GoogleLoginManager : MonoBehaviour
{
    [Header("구글 클라우드 콘솔 설정")]
    [Tooltip("웹 애플리케이션 유형으로 만든 클라이언트 ID를 여기에 넣으세요.")]
    public string webClientId = "여기에_클라이언트_ID_붙여넣기";

    private GoogleSignInConfiguration configuration;

    private void Awake()
    {
        // 1. 구글 팝업을 띄우기 위한 세팅값 준비
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true,
            RequestAuthCode = true // PlayFab과 연결하려면 이게 무조건 true여야 함!
        };
    }

    // ==========================================================
    // 로그인 UI 버튼을 누를 때 실행할 함수 (async 추가)
    // ==========================================================
    public async void OnGoogleLoginButtonClicked()
    {
        Debug.Log("<color=yellow>[Google]</color> 구글 로그인 팝업을 호출합니다...");

        try
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;

            Debug.Log($"<color=cyan>[디버그]</color> 사용된 ClientID: {configuration.WebClientId}");

            // ContinueWith 대신 await를 써서 유니티 메인 스레드에서 결과를 기다림
            GoogleSignInUser user = await GoogleSignIn.DefaultInstance.SignIn();

            // 팝업 닫히고 여기까지 코드가 무사히 내려오면 100% 성공한 것
            Debug.Log("### [디버그] OnAuthenticationFinished 진입했음! (Task 완료)");
            string authCode = user.AuthCode;
            Debug.Log($"<color=green>[Google 성공]</color> 인증 완료! 발급된 티켓: {authCode}");

            if (PlayFabAuthManager.Instance != null)
            {
                PlayFabAuthManager.Instance.LoginWithGoogle(authCode);
            }
        }
        catch (System.Exception ex)
        {
            // 구글 플러그인 내부에서 터진 에러를 강제로 화면에 토해내게 만듦
            Debug.LogError($"<color=red>[Google 치명적 에러]</color> 예외 발생: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"<color=red>[상세 에러]</color> {ex.InnerException.Message}");
            }
        }
    }

    // ==========================================================
    // 구글 로그인이 끝나고 결과(토큰)를 받아오는 콜백 함수
    // ==========================================================
    private void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        Debug.Log("### [디버그] OnAuthenticationFinished 진입했음!");

        if (task.IsFaulted)
        {
            // 에러 발생 시
            Debug.LogError($"<color=red>[Google 에러]</color> 로그인 실패: {task.Exception}");
        }
        else if (task.IsCanceled)
        {
            // 유저가 로그인 창을 닫아버렸을 때
            Debug.Log("<color=yellow>[Google 취소]</color> 사용자가 로그인을 취소했습니다.");
        }
        else
        {
            // 구글 로그인 성공! 대망의 AuthCode(인증 티켓)를 성공적으로 받아옴
            string authCode = task.Result.AuthCode;
            Debug.Log($"<color=green>[Google 성공]</color> 구글 인증 완료! 발급된 티켓: {authCode}");

            // 우리가 어제 만들어둔 플레이팹 로그인 함수의 자판기 구멍에 티켓을 찔러넣음!
            if (PlayFabAuthManager.Instance != null)
            {
                PlayFabAuthManager.Instance.LoginWithGoogle(authCode);
            }
        }
    }
}