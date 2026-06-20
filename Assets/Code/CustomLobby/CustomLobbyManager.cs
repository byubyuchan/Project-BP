using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CustomLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Main Lobby UI")]
    public GameObject lobbyPanel;
    public Toggle availableRoomToggle;
    public TMP_InputField searchInput;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Create Room Panel UI")]
    public GameObject createRoomPanel;
    public TMP_InputField roomNameInput;
    public TMP_Dropdown modeDropdown;
    public TMP_InputField maxPlayersInput;
    public Toggle privateToggle;
    public TMP_InputField passwordInput;
    public Button showCreateRoomPasswordButton;
    public Image showCreateRoomPasswordImage;
    private bool isCreateRoomPasswordVisible = true;

    [Header("Password Panel UI")]
    public Button showPasswordButton;
    public Image showPasswordImage;
    private bool isPasswordVisible = true;

    [Header("Eye Sprite")]
    public Sprite eyeOpenSprite;
    public Sprite eyeClosedSprite;

    [Header("Join Room UI")]
    public GameObject passwordPanel;
    public TMP_InputField joinPasswordInput;

    [Header("Room List UI")]
    public RoomSlot[] roomSlots;
    public TextMeshProUGUI pageText;

    [Header("Warning Panel UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;

    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();
    private RoomInfo currentTargetRoom;
    private int currentPage = 0;
    private int itemsPerPage;

    [Header("What is the Next Scene?")]
    [SerializeField] private string nextSceneName = "WarmupScene";

    [Header("System Menu UI")]
    public GameObject systemMenuPanel;
    public Button quitButton;
    public Button cancelButton;

    private void Start()
    {
        // 방 입장 시 방장이 위치한 씬으로 자동 이동 및 추후 게임 신 전환 시 파티원 자동 동기화 기능
        PhotonNetwork.AutomaticallySyncScene = true;

        itemsPerPage = roomSlots.Length;

        if (availableRoomToggle != null)
        {
            availableRoomToggle.isOn = false;
            availableRoomToggle.onValueChanged.AddListener(OnAvailableToggleChange);
        }

        if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchValueChange);
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);

        if (privateToggle != null)
        {
            privateToggle.onValueChanged.AddListener(OnPrivateToggleChanged);
            OnPrivateToggleChanged(privateToggle.isOn);
        }

        if (showCreateRoomPasswordButton != null)
            showCreateRoomPasswordButton.onClick.AddListener(ToggleCreateRoomPasswordVisibility);

        if (showPasswordButton != null)
            showPasswordButton.onClick.AddListener(TogglePasswordVisibility);

        if (warningPanel != null) warningPanel.SetActive(false);

        // 네트워크 연결 및 씬 복귀 예외 처리
        if (!PhotonNetwork.IsConnected)
        {
            // 1. 게임을 맨 처음 실행했을 때는 연결이 안 되어 있으므로 서버에 연결
            Debug.Log("서버에 연결을 시도합니다...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            // 핵심: 이미 연결된 상태라면 상태에 따라 적절한 마스터 서버/로비로 복귀
            Debug.Log($"이미 연결됨. 현재 상태: {PhotonNetwork.NetworkClientState}");

            if (PhotonNetwork.InRoom)
            {
                // 2. 게임이 끝나고 로비로 돌아왔을 때 아직 네트워크상으로 방에 남아 있다면 방 나가기
                PhotonNetwork.LeaveRoom();
            }
            else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
            {
                // 3. 방에서는 나왔지만 아직 로비에는 들어가지 않은 상태라면 로비로 복귀
                PhotonNetwork.JoinLobby();
            }
        }

        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (cancelButton != null) cancelButton.onClick.AddListener(CloseSystemMenu);

        TMP_InputField[] allInputs = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var input in allInputs)
        {
            input.restoreOriginalTextOnEscape = false;
        }

        UIManager.Instance.onEmptyEsc = OpenSystemMenu;
    }

    public void OpenCreateRoomPanel()
    {
        // If close createRoomPanel, use CloseCreateRoomPanel function
        UIManager.Instance.ShowPanel(createRoomPanel, CloseCreateRoomPanel);

        //roomNameInput.text = "";
        //if (modeDropdown != null) modeDropdown.value = 0;
        //maxPlayersInput.text = "8";

        //if (privateToggle != null) privateToggle.isOn = false;
        //passwordInput.text = "";

        //isCreateRoomPasswordVisible = true;
        //passwordInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        //if (showCreateRoomPasswordImage != null) showCreateRoomPasswordImage.sprite = eyeOpenSprite;
        //passwordInput.ForceLabelUpdate();
    }

    public void ToggleCreateRoomPasswordVisibility()
    {
        isCreateRoomPasswordVisible = !isCreateRoomPasswordVisible;

        if (isCreateRoomPasswordVisible)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            showCreateRoomPasswordImage.sprite = eyeOpenSprite;
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            showCreateRoomPasswordImage.sprite = eyeClosedSprite;
        }

        passwordInput.ForceLabelUpdate();
    }

    public void CloseCreateRoomPanel()
    {
        createRoomPanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        cachedRoomList.Clear();
        UpdateRoomListUI();
    }

    public void CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            ShowWarning("Please enter a valid room name (1-20 characters)");
            return;
        }

        int max = 0;
        if (!int.TryParse(maxPlayersInput.text, out max) || max < 2 || max > 8)
        {
            ShowWarning("Players must be a number between 2 and 8");
            return;
        }
        byte maxPlayers = (byte)max;

        if (privateToggle != null && privateToggle.isOn)
        {
            if (string.IsNullOrWhiteSpace(passwordInput.text) || passwordInput.text.Length < 1 || passwordInput.text.Length > 8)
            {
                ShowWarning("Please enter a valid password (1-8 characters)");
                return;
            }
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;

        // 해시 테이블을 적용하여 게임 모드, 비공개 여부, 비밀번호 등 추가 방 정보를 생성
        Hashtable cp = new Hashtable();
        cp["roomName"] = roomNameInput.text;
        cp["mode"] = modeDropdown.options[modeDropdown.value].text;
        cp["isPrivate"] = privateToggle.isOn;
        cp["password"] = passwordInput.text;

        // 설정한 방 정보를 로비 목록에서도 읽을 수 있도록 브로드캐스트
        roomOptions.CustomRoomProperties = cp;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "roomName", "mode", "isPrivate", "password" };

        string randomRoomID = System.Guid.NewGuid().ToString();
        PhotonNetwork.CreateRoom(randomRoomID, roomOptions);
    }

    private void ShowWarning(string message)
    {
        if (warningText != null) warningText.text = message;
        if (warningPanel != null) UIManager.Instance.ShowPanel(warningPanel, CloseWarningPanel);
    }

    public void CloseWarningPanel() { if (warningPanel != null) warningPanel.SetActive(false); }

    public override void OnCreatedRoom()
    {
        Debug.Log("방 생성 성공");
        CloseCreateRoomPanel();

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(nextSceneName);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"방 생성 실패: {message}");
        ShowWarning($"Failed to create room");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공(지금은 로비 씬 유지)");
    }

    // 변경된 방 정보만 받아와서 캐시 리스트 갱신
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var info in roomList)
        {
            // 이미 캐시에 있는 방이라면 최신 정보로 덮어씌우기 위해 일단 제거
            int index = cachedRoomList.FindIndex(x => x.Name == info.Name);
            if (index != -1) cachedRoomList.RemoveAt(index);

            // 닫힌 방이 아니라면(새로 생겼거나 인원수 등 정보가 바뀐 방) 캐시에 다시 추가
            if (!info.RemovedFromList)
            {
                cachedRoomList.Add(info);
            }
        }
        // 캐싱된 리스트를 기반으로 UI 렌더링 호출
        UpdateRoomListUI();
    }

    void UpdateRoomListUI()
    {
        List<RoomInfo> filteredList = new List<RoomInfo>();
        string searchText = (searchInput != null) ? searchInput.text.ToLower() : "";
        bool showAvailableOnly = (availableRoomToggle != null && availableRoomToggle.isOn);

        foreach (var room in cachedRoomList)
        {
            string displayRoomName = "";
            if (room.CustomProperties.ContainsKey("roomName"))
            {
                displayRoomName = room.CustomProperties["roomName"].ToString();
            }

            bool matchSearch = string.IsNullOrEmpty(searchText) || displayRoomName.ToLower().Contains(searchText);

            bool matchAvailable = true;
            bool isPrivate = false;
            if (room.CustomProperties.ContainsKey("isPrivate"))
                isPrivate = (bool)room.CustomProperties["isPrivate"];

            if (showAvailableOnly)
            {
                if (room.PlayerCount >= room.MaxPlayers || isPrivate) matchAvailable = false;
            }

            if (matchSearch && matchAvailable)
            {
                filteredList.Add(room);
            }
        }

        int totalPages = Mathf.CeilToInt((float)filteredList.Count / itemsPerPage);
        if (totalPages == 0) totalPages = 1;
        if (currentPage >= totalPages) currentPage = totalPages - 1;
        if (currentPage < 0) currentPage = 0;

        int startIndex = currentPage * itemsPerPage;

        for (int i = 0; i < roomSlots.Length; i++)
        {
            int dataIndex = startIndex + i;
            roomSlots[i].gameObject.SetActive(true);

            if (dataIndex < filteredList.Count)
            {
                RoomInfo info = filteredList[dataIndex];
                roomSlots[i].SetInfo(info);

                Button btn = roomSlots[i].GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(info));
            }
            else
            {
                roomSlots[i].ClearSlot();
                roomSlots[i].GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }

        pageText.text = $"{currentPage + 1} / {totalPages}";
        if (prevPageButton != null) prevPageButton.interactable = (currentPage > 0);
        if (nextPageButton != null) nextPageButton.interactable = (currentPage < totalPages - 1);
    }

    public void OnSlotClicked(RoomInfo room)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("아직 서버 연결 중입니다. 잠시만 기다려주세요!");
            return;
        }

        bool isPrivate = false;
        if (room.CustomProperties.ContainsKey("isPrivate"))
            isPrivate = (bool)room.CustomProperties["isPrivate"];

        if (isPrivate)
        {
            currentTargetRoom = room;
            OpenPasswordPanel();
        }
        else
        {
            PhotonNetwork.JoinRoom(room.Name);
        }
    }

    public void SubmitPassword()
    {
        if (currentTargetRoom == null || joinPasswordInput == null) return;

        string serverPass = "";
        if (currentTargetRoom.CustomProperties != null && currentTargetRoom.CustomProperties.ContainsKey("password"))
        {
            object val = currentTargetRoom.CustomProperties["password"];
            if (val != null) serverPass = val.ToString();
        }

        if (joinPasswordInput.text == serverPass)
        {
            PhotonNetwork.JoinRoom(currentTargetRoom.Name);

            ClosePasswordPanel();
        }
        else
        {
            Debug.LogWarning("비밀번호 불일치");
            joinPasswordInput.text = "";
            joinPasswordInput.Select();
        }
    }

    public void OnPrivateToggleChanged(bool isOn)
    {
        if (passwordInput != null)
        {
            passwordInput.interactable = isOn;
            if (!isOn) passwordInput.text = "";
        }
    }
    public void OnSearchValueChange(string text) { currentPage = 0; UpdateRoomListUI(); }
    public void OnAvailableToggleChange(bool isOn) { currentPage = 0; UpdateRoomListUI(); }
    public void NextPage() { currentPage++; UpdateRoomListUI(); }
    public void PrevPage() { currentPage--; UpdateRoomListUI(); }

    void OpenPasswordPanel()
    {
        UIManager.Instance.ShowPanel(passwordPanel, ClosePasswordPanel);
        joinPasswordInput.text = "";
        joinPasswordInput.Select();

        isPasswordVisible = true;
        joinPasswordInput.contentType = TMP_InputField.ContentType.Alphanumeric;
        if (showPasswordImage != null) showPasswordImage.sprite = eyeOpenSprite;
        joinPasswordInput.ForceLabelUpdate();
    }

    // 스트리머들의 방송 환경을 고려하여 방 생성 및 입장 시 비밀번호 마스킹 토글 기능
    private void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            joinPasswordInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            showPasswordImage.sprite = eyeOpenSprite;
        }
        else
        {
            joinPasswordInput.contentType = TMP_InputField.ContentType.Password;
            showPasswordImage.sprite = eyeClosedSprite;
        }

        joinPasswordInput.ForceLabelUpdate();
    }

    public void ClosePasswordPanel()
    {
        passwordPanel.SetActive(false);
        currentTargetRoom = null;
    }

    private void OpenSystemMenu()
    {
        if (systemMenuPanel != null)
        {
            if (systemMenuPanel.activeSelf) CloseSystemMenu();
            else UIManager.Instance.ShowPanel(systemMenuPanel, CloseSystemMenu);
        }
    }

    private void CloseSystemMenu()
    {
        systemMenuPanel.SetActive(false);
    }

    private void QuitGame()
    {
        Debug.Log("게임을 종료합니다.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}