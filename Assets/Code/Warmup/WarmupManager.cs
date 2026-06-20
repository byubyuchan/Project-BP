using NUnit.Framework;
using Photon.Pun;
using Photon.Realtime;
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

    new void Start()
    {
        base.Start();
        InitializePlayerUI();

        if (PhotonNetwork.InRoom)
        {
            ResetPlayerGameProperties();
        }

        hostOptionPanel.SetActive(false);

        if (PhotonNetwork.CurrentRoom != null)
        {
            UpdatePlayerList();
        }

        CloseRoomSettingsPanel();

        if (PhotonNetwork.IsMasterClient)
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
    }

    void Update()
    {
        if (hostOptionPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            RectTransform panelRect = hostOptionPanel.GetComponent<RectTransform>();
            if(!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
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

        if(PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                StartGame();
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();

        if(PhotonNetwork.IsMasterClient && !startButton.gameObject.activeSelf)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
        }

        if(targetPlayer == otherPlayer)
        {
            CloseHostOptionPanel();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdatePlayerList();

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

    public void OpenHostOptionPanel(Player player, Vector3 mousePos)
    {
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

            // IsOpen은 로비 방에는 여전히 뜨지만 다른 사람의 입장을 거부하는 설정
            PhotonNetwork.CurrentRoom.IsOpen = false;
            // IsVisible은 방이 로비에서 보이지 않도록 설정
            PhotonNetwork.CurrentRoom.IsVisible = false;
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
        }
    }

    protected override void CountFinish()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(nextScene);
        }
    }

    public void OpenRoomSettingsPanel()
    {
        UIManager.Instance.ShowPanel(roomSettingsPanel, CloseRoomSettingsPanel);
        Room room = PhotonNetwork.CurrentRoom;

        settingsNameInput.text = room.Name;
        settingsMaxPlayersInput.text = room.MaxPlayers.ToString();

        Hashtable cp = room.CustomProperties;
        if(cp.ContainsKey("roomName")) settingsNameInput.text = cp["roomName"].ToString();
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

        if (PhotonNetwork.IsMasterClient)
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
