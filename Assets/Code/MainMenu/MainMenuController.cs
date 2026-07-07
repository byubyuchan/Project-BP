using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MainMenuController : MonoBehaviourPunCallbacks
{
    [Header("Scene Settings")]
    public string customGameSceneName = "CustomLobbyScene";
    public string warmupSceneName = "WarmupScene";

    [Header("Popup UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }
    }
    public void OnQuickMatchClicked()
    {
        if (PhotonNetwork.InRoom)
        {
            ShowPopup("Match Failed, Try Again");
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.Server == ServerConnection.MasterServer)
        {
            ShowPopup("Try Matching...");
            Hashtable expectedProps = new Hashtable() { { "isQuickMatch", true } };
            PhotonNetwork.JoinRandomRoom(expectedProps, 0);
        }
        else ShowPopup("Match Failed, Try Again");
    }

    public void OnCustomGameClicked()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // 커스텀 로비 씬으로 이동 (거기서 CustomLobbyManager가 알아서 작동함)
            SceneManager.LoadScene(customGameSceneName);
        }
        else Debug.LogWarning("서버 연결 중입니다...");

    }

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
        Debug.Log("새로운 퀵 매치 방을 생성합니다.");

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;
        options.IsOpen = true;
        options.IsVisible = true; // 포톤 매칭 시스템이 찾을 수 있게 공개로 둠 (대신 로비UI에서 숨길 것임)

        // 이 방은 퀵 매치용 방이라는 '투명 망토' 속성을 부여
        options.CustomRoomProperties = new Hashtable() { { "isQuickMatch", true } };
        options.CustomRoomPropertiesForLobby = new string[] { "isQuickMatch" };

        // null을 넣으면 포톤이 겹치지 않는 랜덤 방제(GUID)로 알아서 생성해 줌
        PhotonNetwork.CreateRoom(null, options);
    }

    // [방 입장 성공 시] (퀵 매치용 방에 들어갔을 때만 웜업 씬으로)
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
        if (popupPanel != null) popupPanel.SetActive(false);
    }
    
}