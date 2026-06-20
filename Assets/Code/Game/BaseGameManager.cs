using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public abstract class BaseGameManager : MonoBehaviourPunCallbacks
{
    // 리스폰 값을 하드코딩으로 받지 않도록 만든 내부 클래스   
    public static class PhotonKeys
    {
        // 게임 데이터 관련
        public const string LAP = "Score";
        public const string PROGRESS = "Progress";
        public const string GOAL = "NextCP";

        // 초기 스폰 위치 관련
        public const string INIT_X = "InitX";
        public const string INIT_Y = "InitY";
        public const string INIT_Z = "InitZ";
        public const string INIT_ROT_Y = "InitRotY";

        // 마지막 체크포인트(부활) 위치 관련
        public const string LAST_X = "LastX";
        public const string LAST_Y = "LastY";
        public const string LAST_Z = "LastZ";
        public const string LAST_ROT_Y = "LastRotY";
    }

    // enum값을 통해 현재 게임 상태를 확인할 수 있음. (현재 사용은 되지 않는 중)
    public enum GameState { Wait, Playing, Finish }
    protected GameState currentState = GameState.Wait;

    [Header("GameEnd")]
    public TextMeshProUGUI winnerText;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI messageText;

    [Header("System Menu UI")]
    public GameObject systemMenuPanel;
    public Button leaveRoomButton;
    public Button cancelButton;

    [Header("Player List UI (Base)")]
    public Transform playerListPanel;
    public GameObject playerSlotPrefab;

    [Header("NextScene")]
    [SerializeField]protected string nextScene;

    protected int maxPlayers;
    protected List<BasePlayerSlot> allSlots = new List<BasePlayerSlot>();
    protected Dictionary<int, BasePlayerSlot> activePlayerSlots = new Dictionary<int, BasePlayerSlot>();

    [SerializeField] protected string BGMKey = "BGM_Practice";
    [SerializeField] protected string AmbKey = "Amb_Forest";

    protected virtual void InitializePlayerUI()
    {
        if (playerListPanel == null || playerSlotPrefab == null) return;

        maxPlayers = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.MaxPlayers : 8;
        if (maxPlayers == 0) maxPlayers = 8;

        for (int i = 0; i < maxPlayers; i++)
        {
            GameObject slotObj = Instantiate(playerSlotPrefab, playerListPanel);
            BasePlayerSlot slot = slotObj.GetComponent<BasePlayerSlot>();

            if (slot != null)
            {
                slot.Init(this);

                allSlots.Add(slot);
                slot.SetEmpty();
            }
        }
    }

    protected void RefreshAndSortSlots(List<Player> sortedPlayers)
    {
        if (allSlots.Count == 0) return;

        activePlayerSlots.Clear();
        int siblingIndex = 0;

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            if (i < allSlots.Count)
            {
                BasePlayerSlot slot = allSlots[i];
                slot.Setup(sortedPlayers[i]);
                slot.transform.SetSiblingIndex(siblingIndex);

                activePlayerSlots.Add(sortedPlayers[i].ActorNumber, slot);
                siblingIndex++;
            }
        }

        for (int i = sortedPlayers.Count; i < allSlots.Count; i++)
        {
            BasePlayerSlot slot = allSlots[i];
            slot.SetEmpty();
            slot.transform.SetSiblingIndex(siblingIndex);
            siblingIndex++;
        }
    }

    protected void Start()
    {
        Application.targetFrameRate = 240;
        PhotonNetwork.AutomaticallySyncScene = true;

        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        if (leaveRoomButton != null) leaveRoomButton.onClick.AddListener(LeaveRoom);
        if (cancelButton != null) cancelButton.onClick.AddListener(CloseSystemMenu);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBGM(BGMKey);
            AudioManager.instance.PlayAmb(AmbKey);
        }
    }

    // 현재 게임의 승자가 결정되는 함수로 순위 재정렬과 승리 UI를 RPC로 호출 함. (현재 사용되지 않는 중)
    protected void FinishGame()
    {
        // 변수 선언
        var winner = PhotonNetwork.PlayerList
        .OrderByDescending(p => p.CustomProperties.ContainsKey("Score") ? (int)p.CustomProperties["Score"] : 0)
        .FirstOrDefault();

        string winnerName = (winner != null) ? winner.NickName : "Null";

        photonView.RPC("RPC_FinishGameUI", RpcTarget.All, winnerName);
    }

    public void ShowMessage(string msg)
    {
        if (!countdownText.gameObject.activeSelf)
        {
            countdownText.gameObject.SetActive(true);
        }
        countdownText.text = msg;
    }

    public void ShowMessageAnother(string msg)
    {
        if (!messageText.gameObject.activeSelf)
        {
            messageText.gameObject.SetActive(true);
        }
        messageText.text = msg;
    }

    protected virtual void CountFinish()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
            PhotonNetwork.LoadLevel(nextScene);
        }
    }

    // 게임 종료 카운트다운
    protected IEnumerator CountdownCoroutine(int time, string formatText, string finalText)
    {
        int count = time;

        while (count > 0)
        {
            ShowMessage(string.Format(formatText, count));
            yield return new WaitForSeconds(1f);
            count--;
        }

        ShowMessage(finalText);
        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);

        CountFinish();
    }

    public void OpenSystemMenu()
    {
        if (systemMenuPanel != null)
        {
            UIManager.Instance.ShowPanel(systemMenuPanel, CloseSystemMenu);

            Photon.Pun.UtilityScripts.MoveByKeys myPlayer = FindAnyObjectByType<Photon.Pun.UtilityScripts.MoveByKeys>();
            if (myPlayer != null) myPlayer.SetMenuOpenState(true);
        }
    }

    private void CloseSystemMenu()
    {
        if (systemMenuPanel != null)
        {
            systemMenuPanel.SetActive(false);

            Photon.Pun.UtilityScripts.MoveByKeys myPlayer = FindAnyObjectByType<Photon.Pun.UtilityScripts.MoveByKeys>();
            if (myPlayer != null) myPlayer.SetMenuOpenState(false);
        }
    }
    protected void ResetPlayerGameProperties()
    {
        if (PhotonNetwork.LocalPlayer != null)
        {
            ExitGames.Client.Photon.Hashtable resetProps = new ExitGames.Client.Photon.Hashtable();

            resetProps.Add(PhotonKeys.LAP, 0);
            resetProps.Add(PhotonKeys.PROGRESS, 0);
            resetProps.Add(PhotonKeys.GOAL, 0);

            // 위치 정보들은 null로 밀어서 초기화
            resetProps.Add(PhotonKeys.INIT_X, null);
            resetProps.Add(PhotonKeys.INIT_Y, null);
            resetProps.Add(PhotonKeys.INIT_Z, null);
            resetProps.Add(PhotonKeys.INIT_ROT_Y, null);

            resetProps.Add(PhotonKeys.LAST_X, null);
            resetProps.Add(PhotonKeys.LAST_Y, null);
            resetProps.Add(PhotonKeys.LAST_Z, null);
            resetProps.Add(PhotonKeys.LAST_ROT_Y, null);

            PhotonNetwork.LocalPlayer.SetCustomProperties(resetProps);
        }
    }
    protected void TeleportPlayerToInitialPos(GameObject playerObj, Player targetPlayer)
    {
        if (targetPlayer.CustomProperties.ContainsKey(PhotonKeys.INIT_X))
        {
            float x = (float)targetPlayer.CustomProperties[PhotonKeys.INIT_X];
            float y = (float)targetPlayer.CustomProperties[PhotonKeys.INIT_Y];
            float z = (float)targetPlayer.CustomProperties[PhotonKeys.INIT_Z];
            float rotY = (float)targetPlayer.CustomProperties[PhotonKeys.INIT_ROT_Y];

            TeleportCharacter(playerObj, new Vector3(x, y, z), Quaternion.Euler(0, rotY, 0));
        }
    }
    public void TeleportCharacter(GameObject playerObj, Vector3 pos, Quaternion rot)
    {
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerObj.transform.position = pos;
        playerObj.transform.rotation = rot;

        if (cc != null) cc.enabled = true;

        playerObj.GetComponent<PhotonView>().RPC("RPC_SizeReset", RpcTarget.All);
    }


    public virtual void LeaveRoom() 
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        if (PhotonPoolingManager.instance != null)
        {
            PhotonPoolingManager.instance.ClearPool();
        }
        SceneManager.LoadScene("MainMenuScene");
    }
    public bool GetBestRespawnPoint(out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        Player p = PhotonNetwork.LocalPlayer;

        // 1. 마지막 체크포인트(Last)가 있는지 확인
        if (p.CustomProperties.ContainsKey(PhotonKeys.LAST_X))
        {
            pos = new Vector3((float)p.CustomProperties[PhotonKeys.LAST_X],
                             (float)p.CustomProperties[PhotonKeys.LAST_Y],
                             (float)p.CustomProperties[PhotonKeys.LAST_Z]);
            rot = Quaternion.Euler(0, (float)p.CustomProperties[PhotonKeys.LAST_ROT_Y], 0);
            return true;
        }

        // 2. 없으면 초기 시작 위치(Init) 확인
        if (p.CustomProperties.ContainsKey(PhotonKeys.INIT_X))
        {
            pos = new Vector3((float)p.CustomProperties[PhotonKeys.INIT_X],
                             (float)p.CustomProperties[PhotonKeys.INIT_Y],
                             (float)p.CustomProperties[PhotonKeys.INIT_Z]);
            rot = Quaternion.Euler(0, (float)p.CustomProperties[PhotonKeys.INIT_ROT_Y], 0);
            return true;
        }

        // 차후, false는 배틀로얄 모드에서 사용하면 좋을듯!! 스폰 위치를 랜덤으로 지정해서 부활하도록?? 

        return false;
    }

    public void TeleportCharacterLocal(GameObject playerObj, Vector3 pos, Quaternion rot)
    {
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerObj.transform.position = pos;
        playerObj.transform.rotation = rot;

        if (cc != null) cc.enabled = true;
    }

    public bool RequestTeleport(GameObject playerObj)
    {
        if (GetBestRespawnPoint(out Vector3 resPos, out Quaternion resRot))
        {
            TeleportCharacter(playerObj, resPos, resRot);
            return true;
        }
        else
        {
            Debug.LogWarning("체크포인트가 없습니다! 정해진 위치로 이동합니다.");
            return false;
        }
    }

    [PunRPC]
    protected virtual void RPC_FinishGameUI(string winnerNickName)
    {
        StopAllCoroutines();

        currentState = GameState.Finish;
        winnerText.text = $"{winnerNickName}님이 승리하셨습니다!";
        winnerText.gameObject.SetActive(true);

        StartCoroutine(CountdownCoroutine(5, "{0}초뒤 게임이 종료됩니다!", "대기실로 이동합니다."));
    }

    //[PunRPC]
    //protected void RPC_EndCountdown()
    //{
    //    StopAllCoroutines();
    //    StartCoroutine(CountdownCoroutine(5, "{0}초뒤 게임이 종료됩니다!", "대기실로 이동합니다."));
    //}
}