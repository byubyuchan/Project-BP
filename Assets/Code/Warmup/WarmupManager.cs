using NUnit.Framework;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class WarmupManager : BaseGameManager
{
    [Header("Player List UI")]
    public TextMeshProUGUI playerCountText;

    [Header("Host Option Popup")]
    public GameObject hostOptionPanel;
    public TextMeshProUGUI targetNameText;
    public Button promoteButton;
    public Button kickButton;

    [Header("Room Settings UI")]
    public GameObject roomSettingsPanel;
    public TMP_InputField settingsNameInput;
    public TMP_Dropdown settingsModeDropdown;
    public TMP_InputField settingsMaxPlayersInput;
    public Toggle settingsPrivateToggle;
    public TMP_InputField settingsPasswordInput;
    public Button applySettingsButton;

    [Header("Game Start UI")]
    public Button startButton;

    [Header("Success Panel UI")]
    public GameObject successPanel;

    [Header("Warning Panel UI")]
    public GameObject warningPanel;
    public TextMeshProUGUI warningText;

    private Player targetPlayer;

    // 퀵 매치 관련 변수
    private bool isQuickMatch = false;
    private bool isGameStarting = false;
    private Coroutine quickMatchCoroutine;

    new void Start()
    {
        base.Start();
        InitializePlayerUI();

        if (PhotonNetwork.InRoom)
        {
            ResetPlayerGameProperties();

            // 현재 방이 퀵 매치 방인지 커스텀 방인지 확인!
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("isQuickMatch"))
            {
                isQuickMatch = (bool)PhotonNetwork.CurrentRoom.CustomProperties["isQuickMatch"];
            }
        }

        hostOptionPanel.SetActive(false);

        if (PhotonNetwork.CurrentRoom != null)
        {
            UpdatePlayerList();
        }

        CloseRoomSettingsPanel();

        // 퀵 매치면 무조건 시작 버튼 숨김! 커스텀 방일 때만 방장에게 표시
        if (PhotonNetwork.IsMasterClient && !isQuickMatch)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(StartGame);
        }
        else
        {
            startButton.gameObject.SetActive(false);
        }

        promoteButton.onClick.AddListener(DelegateHost);
        kickButton.onClick.AddListener(KickPlayer);

        if (settingsPrivateToggle != null)
        {
            settingsPrivateToggle.onValueChanged.AddListener((isOn) =>
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    settingsPasswordInput.interactable = isOn;
                    if (!isOn) settingsPasswordInput.text = "";
                }
            });
        }

        if (successPanel != null) successPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);

        TMP_InputField[] allInputs = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var input in allInputs)
        {
            input.restoreOriginalTextOnEscape = false;
        }

        UIManager.Instance.onEmptyEsc = OpenSystemMenu;

        // 씬에 들어왔을 때 이미 2명 이상이면 카운트다운 시작 체크
        if (isQuickMatch) CheckQuickMatchTimer();
    }

    void Update()
    {
        if (hostOptionPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            RectTransform panelRect = hostOptionPanel.GetComponent<RectTransform>();
            if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
            {
                CloseHostOptionPanel();
            }
        }
    }

    private void UpdatePlayerList()
    {
        List<Player> sortedPlayers = PhotonNetwork.PlayerList
            .OrderByDescending(p => p.IsMasterClient)
            .ToList();

        RefreshAndSortSlots(sortedPlayers);

        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
        playerCountText.text = $"{sortedPlayers.Count} / {maxPlayers}";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();

        // 1. 방이 꽉 찼을 때 자동 시작 (공통)
        if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (quickMatchCoroutine != null) StopCoroutine(quickMatchCoroutine);
                StartGame();
            }
        }
        // 2. 퀵 매치 방인데 인원이 들어오면 10초 타이머 리셋!
        else if (isQuickMatch && PhotonNetwork.IsMasterClient && !isGameStarting)
        {
            ResetQuickMatchTimer();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();

        if (isQuickMatch)
        {
            // 퀵 매치 중 사람이 나가서 2명 미만이 되면 타이머 폭파
            CheckQuickMatchTimer();
        }
        else
        {
            // 커스텀 방일 때 방장 시작 버튼 복구
            if (PhotonNetwork.IsMasterClient && !startButton.gameObject.activeSelf)
            {
                startButton.gameObject.SetActive(true);
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartGame);
            }
        }

        if (targetPlayer == otherPlayer)
        {
            CloseHostOptionPanel();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdatePlayerList();

        if (isQuickMatch)
        {
            // 방장이 튕겨서 새 방장이 넘겨받으면 타이머 다시 체크!
            CheckQuickMatchTimer();
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                startButton.gameObject.SetActive(true);
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartGame);
            }
            else
            {
                startButton.gameObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // 퀵 매치 자동 10초 카운트다운 기믹
    // ==========================================
    private void CheckQuickMatchTimer()
    {
        if (!PhotonNetwork.IsMasterClient || isGameStarting || !isQuickMatch) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            if (quickMatchCoroutine == null) quickMatchCoroutine = StartCoroutine(QuickMatchTimerRoutine());
        }
        else
        {
            if (quickMatchCoroutine != null)
            {
                StopCoroutine(quickMatchCoroutine);
                quickMatchCoroutine = null;
            }
            // ✨ (수정) 부모의 UI 업데이트 RPC 호출
            photonView.RPC("RPC_UpdateCountdownText", RpcTarget.All, "다른 플레이어를 대기 중...");
        }
    }

    private void ResetQuickMatchTimer()
    {
        if (!PhotonNetwork.IsMasterClient || isGameStarting || !isQuickMatch) return;

        if (quickMatchCoroutine != null) StopCoroutine(quickMatchCoroutine);
        quickMatchCoroutine = StartCoroutine(QuickMatchTimerRoutine());
    }

    private IEnumerator QuickMatchTimerRoutine()
    {
        float timer = 10f;
        while (timer > 0)
        {
            photonView.RPC("RPC_UpdateCountdownText", RpcTarget.All, $"매칭 완료! {Mathf.CeilToInt(timer)}초 후 시작합니다...");
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        isGameStarting = true;
        photonView.RPC("RPC_UpdateCountdownText", RpcTarget.All, "게임 진입 중...");

        // 10초 끝! 기존에 만들어둔 StartGame 함수 호출해서 진입
        StartGame();
    }

    [PunRPC]
    private void RPC_UpdateCountdownText(string msg)
    {
        ShowMessage(msg);
    }

    // ==========================================
    // 기존 기능들 (강퇴, 설정 등)
    // ==========================================
    public void OpenHostOptionPanel(Player player, Vector3 mousePos)
    {
        // 퀵 매치에서는 강퇴/방장위임 금지!
        if (isQuickMatch) return;

        targetPlayer = player;
        targetNameText.text = player.NickName;
        hostOptionPanel.transform.position = mousePos;
        UIManager.Instance.ShowPanel(hostOptionPanel, CloseHostOptionPanel);
    }

    private void CloseHostOptionPanel()
    {
        hostOptionPanel.SetActive(false);
        targetPlayer = null;
    }

    private void DelegateHost()
    {
        if (targetPlayer != null && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.SetMasterClient(targetPlayer);
            CloseHostOptionPanel();
        }
    }

    private void KickPlayer()
    {
        if (targetPlayer != null && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_Kicked", targetPlayer);
            CloseHostOptionPanel();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        UpdatePlayerList();
    }

    private void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("CharacterType", "Warrior");
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            // 기존 5초 카운트다운 로직 호출
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
        }
    }

    protected override void CountFinish()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 포톤 메시지 큐 정지 (씬 로드 중 에러 방지)
            PhotonNetwork.IsMessageQueueRunning = false;
            PhotonNetwork.LoadLevel(nextScene);
        }
    }

    public void OpenRoomSettingsPanel()
    {
        // 퀵 매치에서는 방 설정 변경 금지!
        if (isQuickMatch) return;

        UIManager.Instance.ShowPanel(roomSettingsPanel, CloseRoomSettingsPanel);
        Room room = PhotonNetwork.CurrentRoom;

        settingsNameInput.text = room.Name;
        settingsMaxPlayersInput.text = room.MaxPlayers.ToString();

        Hashtable cp = room.CustomProperties;
        if (cp.ContainsKey("roomName")) settingsNameInput.text = cp["roomName"].ToString();
        if (cp.ContainsKey("mode"))
        {
            string currentMode = cp["mode"].ToString();
            int index = settingsModeDropdown.options.FindIndex(o => o.text == currentMode);
            if (index >= 0) settingsModeDropdown.value = index;
        }
        if (cp.ContainsKey("isPrivate")) settingsPrivateToggle.isOn = (bool)cp["isPrivate"];
        if (cp.ContainsKey("password")) settingsPasswordInput.text = cp["password"].ToString();

        bool isHost = PhotonNetwork.IsMasterClient;

        settingsNameInput.interactable = isHost;
        settingsModeDropdown.interactable = isHost;
        settingsMaxPlayersInput.interactable = isHost;
        settingsPrivateToggle.interactable = isHost;
        settingsPasswordInput.interactable = isHost && settingsPrivateToggle.isOn;

        applySettingsButton.gameObject.SetActive(isHost);
    }

    public void CloseRoomSettingsPanel()
    {
        roomSettingsPanel.SetActive(false);
    }

    public void ApplyRoomSettings()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Room room = PhotonNetwork.CurrentRoom;

        if (string.IsNullOrWhiteSpace(settingsNameInput.text))
        {
            ShowWarning("Please enter a valid room name");
            return;
        }

        int max = 0;
        if (!int.TryParse(settingsMaxPlayersInput.text, out max) || max < 2 || max > 8)
        {
            ShowWarning("Players must be a number between 2 and 8");
            return;
        }

        if (max < room.PlayerCount)
        {
            ShowWarning($"Cannot set max players below current player count ({room.PlayerCount})");
            return;
        }

        if (settingsPrivateToggle != null && settingsPrivateToggle.isOn)
        {
            if (string.IsNullOrWhiteSpace(settingsPasswordInput.text) ||
                settingsPasswordInput.text.Length < 1 ||
                settingsPasswordInput.text.Length > 8)
            {
                ShowWarning("Please enter a valid password (1-8 characters)");
                return;
            }
        }

        room.MaxPlayers = (byte)max;

        Hashtable cp = new Hashtable();
        cp["roomName"] = settingsNameInput.text;
        cp["mode"] = settingsModeDropdown.options[settingsModeDropdown.value].text;
        cp["isPrivate"] = settingsPrivateToggle.isOn;
        cp["password"] = settingsPasswordInput.text;

        room.SetCustomProperties(cp);

        ShowSuccessMessage();
    }

    private void ShowSuccessMessage()
    {
        if (successPanel != null) UIManager.Instance.ShowPanel(successPanel, CloseSuccessPanel);
    }

    public void CloseSuccessPanel()
    {
        if (successPanel != null) successPanel.SetActive(false);
    }

    private void ShowWarning(string message)
    {
        if (warningText != null) warningText.text = message;
        if (warningPanel != null) UIManager.Instance.ShowPanel(warningPanel, CloseWarningPanel);
    }

    public void CloseWarningPanel()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 완료: 속성 초기화 및 리스트 업데이트");
        ResetPlayerGameProperties();
        UpdatePlayerList();
        CloseRoomSettingsPanel();

        if (PhotonNetwork.IsMasterClient && !isQuickMatch)
        {
            startButton.gameObject.SetActive(true);
        }
    }

    [PunRPC]
    protected void RPC_StartCountdown()
    {
        startButton.gameObject.SetActive(false);
        StartCoroutine(CountdownCoroutine(5, "{0}", "START!"));
    }

    [PunRPC]
    private void RPC_Kicked()
    {
        PhotonNetwork.LeaveRoom();
    }
}