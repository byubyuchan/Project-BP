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

    public GameObject[] goalObjects;
    public GameObject[] checkZones;

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

        // 본 게임 씬에 도착했으니 셔터 올리고 통신 재개!
        PhotonNetwork.IsMessageQueueRunning = true;

        maxPlayers = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.MaxPlayers : 8;
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

        if (goalObjects != null && goalObjects.Length > 0)
        {
            for (int i = 1; i < goalObjects.Length; i++)
            {
                goalObjects[i].SetActive(false);
            }
        }

        if (checkZones != null && checkZones.Length > 0)
        {
            for (int i = 1; i < checkZones.Length; i++)
            {
                checkZones[i].SetActive(false);
            }
        }
    }

    private void SortPlayerUI()
    {
        // 1. 점수(바퀴 수)를 1순위로 내림차순 정렬
        // 2. 진행도(Distance)를 2순위로 내림차순 정렬 (골인 지점에 가까울수록 수치가 크다고 가정)
        // 만약 '결승선까지 남은 거리'라면 값이 작을수록 앞서있는 것이므로 ThenBy(오름차순)를 사용

        //var sortedPlayers = PhotonNetwork.PlayerList
        //    .OrderByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)p.CustomProperties[PhotonKeys.LAP] : 0)
        //    .ThenByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)p.CustomProperties[PhotonKeys.PROGRESS] : 0)
        //    .ToList();

        var sortedPlayers = PhotonNetwork.PlayerList
            // 1순위: 이제 바퀴 수가 아니라 점수(Score)가 제일 높은 사람이 1등!
            .OrderByDescending(p => p.CustomProperties.ContainsKey("Score") ? (int)p.CustomProperties["Score"] : 0)
            // 2순위: 점수가 같다면 현재 진행도(거리)가 앞선 사람이 이김!
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
                    int score = sortedPlayers[i].CustomProperties.ContainsKey("Score") ? (int)sortedPlayers[i].CustomProperties["Score"] : 0;
                    slot.UpdateScore(score);
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
            //if (nextGoalIndex >= checkpoints.Count)
            //{
            //    int currentLap = player.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)player.CustomProperties[PhotonKeys.LAP] : 0;
            //    props[PhotonKeys.LAP] = currentLap + 1;
            //    props[PhotonKeys.GOAL] = 0;

            //    PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            //    // TeleportPlayerToInitialPos(playerObj, player);

            //    nextGoalIndex = 0;
            //}

            if (nextGoalIndex >= checkpoints.Count)
            {
                int currentLap = player.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)player.CustomProperties[PhotonKeys.LAP] : 0;
                int currentScore = player.CustomProperties.ContainsKey("Score") ? (int)player.CustomProperties["Score"] : 0;

                props[PhotonKeys.LAP] = currentLap + 1;
                props[PhotonKeys.GOAL] = 0;

                props["Score"] = currentScore + 100;

                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                nextGoalIndex = 0;
            }
            else
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            }

            // Fly 캐릭터의 경우, 포탈이 아닌 체크포인트에만 도달하면 퐁당퐁당으로 캐릭터를 유지한 채 완주가 가능함.
            if (expectedIndex < goalObjects.Length) goalObjects[expectedIndex].SetActive(false);
            if (expectedIndex < checkZones.Length) checkZones[expectedIndex].SetActive(false);

            Debug.Log($"Player {player.NickName} passed checkpoint {expectedIndex}. Next goal: {nextGoalIndex}. Progress: {currentProgress + 1}");
        }
        else
        {
            // 방금 막 통과한 '직전' 체크포인트인지 계산 (0번 인덱스면 마지막 체크포인트가 직전)
            int previousIndex = expectedIndex - 1;
            if (previousIndex < 0) previousIndex = checkpoints.Count - 1;

            // 방금 통과한 곳에 살짝 비벼진 게 아니라, 진짜 꼼수를 쓰거나 역주행을 한 거라면?
            if (cpTransform != checkpoints[previousIndex])
            {
                RequestTeleport(playerObj);
            }
        }
    }
    public void ActivateMyNextCheckpoint()
    {
        int expectedIndex = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PhotonKeys.GOAL)
                            ? (int)PhotonNetwork.LocalPlayer.CustomProperties[PhotonKeys.GOAL] : 0;

        if (expectedIndex + 1 < goalObjects.Length) expectedIndex++;
        else expectedIndex = 0;

        // 1. 다음 Goal Object 켜기 안전띠
        if (goalObjects != null && expectedIndex < goalObjects.Length && goalObjects[expectedIndex] != null)
        {
            goalObjects[expectedIndex].SetActive(true);
        }

        // 2. 다음 Invincible Zone 켜기 안전띠
        if (checkZones != null && expectedIndex < checkZones.Length && checkZones[expectedIndex] != null)
        {
            checkZones[expectedIndex].SetActive(true);
        }

        Debug.Log("활성화 인덱스 : " + expectedIndex);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Score") || changedProps.ContainsKey(PhotonKeys.PROGRESS))
        {
            SortPlayerUI();
        }

        // 이제 LAP 검사가 아니라 Score 검사로 우승자를 가림!
        if (changedProps.ContainsKey("Score"))
        {
            int currentScore = (int)changedProps["Score"];
            if (currentScore >= 300)
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


    public void AddKillScore(int killerActorNr)
    {
        Player killer = PhotonNetwork.CurrentRoom.GetPlayer(killerActorNr);
        if (killer != null)
        {
            int currentScore = killer.CustomProperties.ContainsKey("Score") ? (int)killer.CustomProperties["Score"] : 0;
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["Score"] = currentScore + 1;
            killer.SetCustomProperties(props);

            Debug.Log($"<color=red>[킬 로그] {killer.NickName}님이 1킬 달성! 현재 점수: {currentScore + 1}</color>");
        }
    }
}