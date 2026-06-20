using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunGameManager : BaseGameManager
{
    public static RunGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Track Data")]
    public List<Transform> checkpoints = new List<Transform>();
    public int maxLap = 3;

    private new void Start()
    {
        base.Start();

        maxPlayers = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.MaxPlayers : 8;
        if (maxPlayers == 0) maxPlayers = 8;

        InitializePlayerUI();

        if (PhotonNetwork.CurrentRoom != null)
        {
            SortPlayerUI();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.onEmptyEsc = OpenSystemMenu;
        }
    }

    private void SortPlayerUI()
    {
        // 1. 점수(바퀴 수)를 1순위로 내림차순 정렬
        // 2. 진행도(Distance)를 2순위로 내림차순 정렬 (골인 지점에 가까울수록 수치가 크다고 가정)
        // 만약 '결승선까지 남은 거리'라면 값이 작을수록 앞서있는 것이므로 ThenBy(오름차순)를 사용
        var sortedPlayers = PhotonNetwork.PlayerList
            .OrderByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)p.CustomProperties[PhotonKeys.LAP] : 0)
            .ThenByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)p.CustomProperties[PhotonKeys.PROGRESS] : 0)
            .ToList();

        RefreshAndSortSlots(sortedPlayers);

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            int actorNr = sortedPlayers[i].ActorNumber;
            if (activePlayerSlots.ContainsKey(actorNr))
            {
                // 부모의 BasePlayerSlot을 RunPlayerSlot으로 변환
                RunPlayerSlot slot = activePlayerSlots[actorNr] as RunPlayerSlot;
                if (slot != null)
                {
                    int lap = sortedPlayers[i].CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)sortedPlayers[i].CustomProperties[PhotonKeys.LAP] : 0;
                    slot.UpdateScore(lap);
                    slot.UpdateRank(i + 1);
                }
            }
        }
    }

    // 플레이어가 처음 입장했을 때, 초기 위치와 회전값을 보고하는 함수
    public void ReportAndInitializePlayerInitialPos(GameObject playerObj)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props.Add(PhotonKeys.INIT_X, playerObj.transform.position.x);
        props.Add(PhotonKeys.INIT_Y, playerObj.transform.position.y);
        props.Add(PhotonKeys.INIT_Z, playerObj.transform.position.z);
        props.Add(PhotonKeys.INIT_ROT_Y, playerObj.transform.eulerAngles.y);

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // 플레이어가 체크포인트 트리거에 닿았을 때, 해당 체크포인트가 자신의 다음 목표인지 확인하고 진행도 업데이트
    public void ProcessLocalPlayerCheckpointTrigger(GameObject playerObj, Transform cpTransform)
    {
        if (checkpoints.Count == 0) return;

        Player player = PhotonNetwork.LocalPlayer;
        int expectedIndex = player.CustomProperties.ContainsKey(PhotonKeys.GOAL) ? (int)player.CustomProperties[PhotonKeys.GOAL] : 0;
        if (expectedIndex >= checkpoints.Count) expectedIndex = 0;

        // 닿은 체크포인트가 내 다음 목표
        if (cpTransform == checkpoints[expectedIndex])
        {
            int currentProgress = player.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)player.CustomProperties[PhotonKeys.PROGRESS] : 0;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            int nextGoalIndex = expectedIndex + 1;

            props.Add(PhotonKeys.PROGRESS, currentProgress + 1);
            props.Add(PhotonKeys.GOAL, nextGoalIndex);

            // 부활 지점 업데이트 (마지막으로 통과한 체크포인트 위치)
            props.Add(PhotonKeys.LAST_X, cpTransform.position.x);
            props.Add(PhotonKeys.LAST_Y, cpTransform.position.y);
            props.Add(PhotonKeys.LAST_Z, cpTransform.position.z);
            props.Add(PhotonKeys.LAST_ROT_Y, cpTransform.eulerAngles.y);

            // 랩 완주 판정
            if (nextGoalIndex >= checkpoints.Count)
            {
                int currentLap = player.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)player.CustomProperties[PhotonKeys.LAP] : 0;
                props[PhotonKeys.LAP] = currentLap + 1;
                props[PhotonKeys.GOAL] = 0;

                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                TeleportPlayerToInitialPos(playerObj, player);
            }
            else
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }
        }
    }


    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Score(바퀴 수)나 Progress(진행도) 중 하나라도 변경되면 UI 업데이트
        if (changedProps.ContainsKey(PhotonKeys.LAP) || changedProps.ContainsKey(PhotonKeys.PROGRESS))
        {
            SortPlayerUI();
        }

        if (changedProps.ContainsKey(PhotonKeys.LAP))
        {
            int currentLap = (int)changedProps[PhotonKeys.LAP];

            if (currentLap >= maxLap)
            {
                OnPlayerFinished();
            }
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (activePlayerSlots.ContainsKey(otherPlayer.ActorNumber))
        {
            activePlayerSlots[otherPlayer.ActorNumber].SetEmpty();
            activePlayerSlots.Remove(otherPlayer.ActorNumber);
            SortPlayerUI();
        }
    }

    public void OnPlayerFinished()
    {
        if (currentState == GameState.Finish) return;

        FinishGame();
    }
}