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
        if (checkpoints.Count == 0) return;

        Player player = PhotonNetwork.LocalPlayer;
        int expectedIndex = player.CustomProperties.ContainsKey(PhotonKeys.GOAL) ? (int)player.CustomProperties[PhotonKeys.GOAL] : 0;
        if (expectedIndex >= checkpoints.Count) expectedIndex = 0;

        if (cpTransform == checkpoints[expectedIndex])
        {
            int currentProgress = player.CustomProperties.ContainsKey(PhotonKeys.PROGRESS) ? (int)player.CustomProperties[PhotonKeys.PROGRESS] : 0;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            int nextGoalIndex = expectedIndex + 1;

            props.Add(PhotonKeys.PROGRESS, currentProgress + 1);
            props.Add(PhotonKeys.GOAL, nextGoalIndex);

            props.Add(PhotonKeys.LAST_X, cpTransform.position.x);
            props.Add(PhotonKeys.LAST_Y, cpTransform.position.y);
            props.Add(PhotonKeys.LAST_Z, cpTransform.position.z);
            props.Add(PhotonKeys.LAST_ROT_Y, cpTransform.eulerAngles.y);

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

            if (expectedIndex < goalObjects.Length) goalObjects[expectedIndex].SetActive(false);
            if (expectedIndex < checkZones.Length) checkZones[expectedIndex].SetActive(false);

            Debug.Log($"Player {player.NickName} passed checkpoint {expectedIndex}. Next goal: {nextGoalIndex}. Progress: {currentProgress + 1}");
        }
        else
        {
            int previousIndex = expectedIndex - 1;
            if (previousIndex < 0) previousIndex = checkpoints.Count - 1;

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

        if (checkpoints.Count > 0) expectedIndex %= checkpoints.Count;
        ActivateCheckpointVisual(expectedIndex);
    }

    public bool ProcessLocalPlayerPortalTransition(GameObject playerObj, Transform destinationCheckpoint)
    {
        if (playerObj == null || destinationCheckpoint == null || checkpoints.Count == 0) return false;

        Player player = PhotonNetwork.LocalPlayer;
        int expectedIndex = player.CustomProperties.ContainsKey(PhotonKeys.GOAL)
            ? (int)player.CustomProperties[PhotonKeys.GOAL]
            : 0;

        expectedIndex %= checkpoints.Count;

        if (destinationCheckpoint != checkpoints[expectedIndex])
        {
            RequestTeleport(playerObj);
            return false;
        }

        ProcessLocalPlayerCheckpointTrigger(playerObj, destinationCheckpoint);
        ActivateCheckpointVisual((expectedIndex + 1) % checkpoints.Count);
        return true;
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
        }
    }
}
