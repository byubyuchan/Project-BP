using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MainMenuController : MonoBehaviourPunCallbacks
{
    [Header("씬 설정")]
    public string customGameSceneName = "CustomLobbyScene";
    public string warmupSceneName = "WarmupScene";
    public string practiceSceneName = "PracticeScene";

    [Header("알림 UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;

    // 서버 연결이나 방 퇴장이 끝난 뒤 연습장으로 이동할 요청을 저장합니다.
    private bool shouldLoadPracticeScene;

    // 방 퇴장 요청이 중복 호출되지 않도록 현재 퇴장 상태를 저장합니다.
    private bool isLeavingRoom;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 다른 네트워크 씬에서 중지됐을 수 있는 Photon 콜백을 다시 활성화합니다.
        PhotonNetwork.IsMessageQueueRunning = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.InRoom)
        {
            //메인 로비 진입 시 진행 중인 방 퇴장 상태를 추적합니다.
            isLeavingRoom = PhotonNetwork.LeaveRoom();
        }
    }

    public void OnQuickMatchClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            ShowPopup("매칭에 실패했습니다. 다시 시도해주세요.");
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.Server == ServerConnection.MasterServer)
        {
            ShowPopup("매칭을 시작합니다.");
            Hashtable expectedProps = new Hashtable { { "isQuickMatch", true } };
            PhotonNetwork.JoinRandomRoom(expectedProps, 0);
        }
        else
        {
            ShowPopup("매칭에 실패했습니다. 다시 시도해주세요.");
        }
    }

    public void OnCustomGameClicked()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // 커스텀 로비 씬으로 이동하면 CustomLobbyManager가 방 입장을 처리합니다.
            SceneManager.LoadScene(customGameSceneName);
        }
        else
        {
            Debug.LogWarning("서버에 연결 중입니다.");
        }
    }

    public void OnPracticeSceneClicked()
    {
        shouldLoadPracticeScene = true;
        TryLoadPracticeScene();
    }

    // 서버 연결과 기존 방 퇴장이 모두 끝났을 때만 연습장 씬을 불러옵니다.
    private void TryLoadPracticeScene()
    {
        if (!shouldLoadPracticeScene)
        {
            ShowPopup("로딩 실패, 다시 시도해주세요.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            if (!isLeavingRoom)
            {
                isLeavingRoom = PhotonNetwork.LeaveRoom();
                ShowPopup("로딩 실패, 다시 시도해주세요.");
            }
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady &&
            PhotonNetwork.Server == ServerConnection.MasterServer)
        {
            ShowPopup("연습장으로 이동합니다.");
            shouldLoadPracticeScene = false;
            SceneManager.LoadScene(practiceSceneName);
            return;
        }

        ShowPopup("로딩 실패, 다시 시도해주세요.");
    }

    //public override void OnConnectedToMaster()
    //{
    //    // 서버 연결 중 연습장 버튼을 눌렀다면 연결 완료 후 이동을 이어갑니다.
    //    TryLoadPracticeScene();
    //}

    //public override void OnLeftRoom()
    //{
    //    // 기존 방 퇴장이 완료되면 보관한 연습장 이동을 이어갑니다.
    //    isLeavingRoom = false;
    //    TryLoadPracticeScene();
    //}

    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("입장 가능한 빠른 대전 방이 없어 새 방을 생성합니다.");

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 8,
            IsOpen = true,
            IsVisible = true,
            // (변경) 빠른 대전 방을 구분하기 위한 사용자 지정 속성입니다.
            CustomRoomProperties = new Hashtable { { "isQuickMatch", true } },
            CustomRoomPropertiesForLobby = new[] { "isQuickMatch" }
        };

        // 방 이름을 비워두면 Photon이 중복되지 않는 이름을 자동으로 생성합니다.
        PhotonNetwork.CreateRoom(null, options);
    }

    // 빠른 대전 방에 입장한 경우에만 웜업 씬으로 이동합니다.
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("isQuickMatch"))
        {
            PhotonNetwork.IsMessageQueueRunning = false;
            PhotonNetwork.LoadLevel(warmupSceneName);
        }
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
}
