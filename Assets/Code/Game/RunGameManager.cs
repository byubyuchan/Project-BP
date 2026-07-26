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

    public int winScore = 100;

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

        //var sortedPlayers = PhotonNetwork.PlayerList
        //    .OrderByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)p.CustomProperties[PhotonKeys.LAP] : 0)
        //    .ThenByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)p.CustomProperties[PhotonKeys.PROGRESS] : 0)
        //    .ToList();

        var sortedPlayers = PhotonNetwork.PlayerList

            .OrderByDescending(p => p.CustomProperties.ContainsKey("Score") ? (int)p.CustomProperties["Score"] : 0)

            .ThenByDescending(p => p.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)p.CustomProperties[PhotonKeys.PROGRESS] : 0)
            .ToList();

        RefreshAndSortSlots(sortedPlayers);

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            int actorNr = sortedPlayers[i].ActorNumber;
            if (activePlayerSlots.ContainsKey(actorNr))
            {
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

    public void ReportAndInitializePlayerInitialPos(GameObject playerObj)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props.Add(PhotonKeys.INIT_X, playerObj.transform.position.x);
        props.Add(PhotonKeys.INIT_Y, playerObj.transform.position.y);
        props.Add(PhotonKeys.INIT_Z, playerObj.transform.position.z);
        props.Add(PhotonKeys.INIT_ROT_Y, playerObj.transform.eulerAngles.y);

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void ProcessLocalPlayerCheckpointTrigger(GameObject playerObj, Transform cpTransform)
    {
        TryProcessLocalPlayerCheckpoint(playerObj, cpTransform);
    }

    // 순간이동 전에 목적지가 현재 순서의 체크포인트인지 확인합니다.
    public bool IsExpectedCheckpoint(Transform cpTransform)
    {
        if (cpTransform == null || checkpoints == null || checkpoints.Count == 0) return false;

        int checkpointIndex = FindCheckpointIndex(cpTransform);
        if (checkpointIndex < 0) return false;

        Player player = PhotonNetwork.LocalPlayer;
        int expectedIndex = player.CustomProperties.ContainsKey(PhotonKeys.GOAL)
            ? (int)player.CustomProperties[PhotonKeys.GOAL]
            : 0;

        expectedIndex %= checkpoints.Count;
        return checkpointIndex == expectedIndex;
    }

    // 콜라이더가 체크포인트의 자식이어도 등록된 체크포인트를 찾을 수 있도록 보정합니다.
    private int FindCheckpointIndex(Transform cpTransform)
    {
        if (cpTransform == null || checkpoints == null) return -1;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            Transform checkpoint = checkpoints[i];
            if (checkpoint == null) continue;

            if (cpTransform == checkpoint ||
                cpTransform.IsChildOf(checkpoint) ||
                checkpoint.IsChildOf(cpTransform))
            {
                return i;
            }
        }

        return -1;
    }

    // 체크포인트 진행도와 완주 점수를 실제로 갱신했는지 호출자가 확인할 수 있게 합니다.
    private bool TryProcessLocalPlayerCheckpoint(GameObject playerObj, Transform cpTransform)
    {
        if (playerObj == null || cpTransform == null || checkpoints == null || checkpoints.Count == 0)
        {
            return false;
        }

        Player player = PhotonNetwork.LocalPlayer;
        int expectedIndex = player.CustomProperties.ContainsKey(PhotonKeys.GOAL) ? (int)player.CustomProperties[PhotonKeys.GOAL] : 0;
        expectedIndex %= checkpoints.Count;
        int checkpointIndex = FindCheckpointIndex(cpTransform);

        if (checkpointIndex == expectedIndex)
        {
            int currentProgress = player.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)player.CustomProperties[PhotonKeys.PROGRESS] : 0;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            int nextGoalIndex = expectedIndex + 1;

            props.Add(PhotonKeys.PROGRESS, currentProgress + 1);
            props.Add(PhotonKeys.GOAL, nextGoalIndex);

            // 자식 콜라이더가 아니라 체크포인트 목록에 등록된 기준 위치를 저장합니다.
            Transform checkpoint = checkpoints[checkpointIndex];
            props.Add(PhotonKeys.LAST_X, checkpoint.position.x);
            props.Add(PhotonKeys.LAST_Y, checkpoint.position.y);
            props.Add(PhotonKeys.LAST_Z, checkpoint.position.z);
            props.Add(PhotonKeys.LAST_ROT_Y, checkpoint.eulerAngles.y);

            if (nextGoalIndex >= checkpoints.Count)
            {
                int currentLap = player.CustomProperties.ContainsKey(PhotonKeys.LAP) ? (int)player.CustomProperties[PhotonKeys.LAP] : 0;
                int currentScore = player.CustomProperties.ContainsKey("Score") ? (int)player.CustomProperties["Score"] : 0;

                props[PhotonKeys.LAP] = currentLap + 1;
                props[PhotonKeys.GOAL] = 0;

                props["Score"] = currentScore + 50;

                nextGoalIndex = 0;
            }

            // 네트워크 속성 전송에 실패하면 체크포인트를 끄거나 다음 구간으로 진행하지 않습니다.
            if (!PhotonNetwork.LocalPlayer.SetCustomProperties(props))
            {
                Debug.LogWarning($"체크포인트 {checkpointIndex}의 진행도 저장 요청에 실패했습니다.");
                return false;
            }

            if (goalObjects != null && expectedIndex < goalObjects.Length && goalObjects[expectedIndex] != null)
            {
                goalObjects[expectedIndex].SetActive(false);
            }

            if (checkZones != null && expectedIndex < checkZones.Length && checkZones[expectedIndex] != null)
            {
                checkZones[expectedIndex].SetActive(false);
            }

            // 일반 접촉과 순간이동 모두 다음 체크포인트 표시를 같은 시점에 활성화합니다.
            ActivateCheckpointVisual(nextGoalIndex);

            Debug.Log($"플레이어 {player.NickName} 체크포인트 {expectedIndex} 통과. 다음 체크포인트: {nextGoalIndex}, 진행도: {currentProgress + 1}");
            return true;
        }

        int previousIndex = expectedIndex - 1;
        if (previousIndex < 0) previousIndex = checkpoints.Count - 1;

        // 방금 통과한 체크포인트의 중복 트리거는 무시하고, 순서가 틀린 경우에만 복귀시킵니다.
        if (checkpointIndex >= 0 && checkpointIndex != previousIndex)
        {
            RequestTeleport(playerObj);
        }

        return false;
    }
    public void ActivateMyNextCheckpoint()
    {
        int expectedIndex = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PhotonKeys.GOAL)
                            ? (int)PhotonNetwork.LocalPlayer.CustomProperties[PhotonKeys.GOAL] : 0;

        if (checkpoints.Count > 0) expectedIndex %= checkpoints.Count;
        ActivateCheckpointVisual(expectedIndex);
    }

    public bool ProcessLocalPlayerPortalTransition(GameObject playerObj, Transform destinationCheckpoint)
    {
        // 포탈 도착도 실제 순간이동이 끝난 뒤 공통 체크포인트 판정으로 확정합니다.
        return TryProcessLocalPlayerCheckpoint(playerObj, destinationCheckpoint);
    }

    private void ActivateCheckpointVisual(int expectedIndex)
    {
        if (expectedIndex < 0) return;

        if (goalObjects != null && expectedIndex < goalObjects.Length && goalObjects[expectedIndex] != null)
        {
            goalObjects[expectedIndex].SetActive(true);
        }

        if (checkZones != null && expectedIndex < checkZones.Length && checkZones[expectedIndex] != null)
        {
            checkZones[expectedIndex].SetActive(true);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Score") || changedProps.ContainsKey(PhotonKeys.PROGRESS))
        {
            SortPlayerUI();
        }

        if (changedProps.ContainsKey("Score"))
        {
            int currentScore = (int)changedProps["Score"];
            if (currentScore >= winScore)
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
        }
    }
}
