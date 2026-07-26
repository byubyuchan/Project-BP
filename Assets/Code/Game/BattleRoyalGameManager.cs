using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleRoyalGameManager : BaseGameManager
{
    public static BattleRoyalGameManager Instance { get; private set; }

    [Header("Battle Royal Rules")]
    // 배틀로얄 모드의 기본 승리 조건은 30킬입니다.
    [Min(1)] public int winScore = 30;

    // 부활 직후 적용할 무적 시간을 인스펙터에서 조절합니다.
    [Min(0f)] public float respawnInvincibleDuration = 3f;

    private int playerLayer = -1;
    private int localPlayerLayer = -1;
    private bool previousPlayerCollisionIgnored;
    private bool changedPlayerCollisionRule;

    private void Awake()
    {
        // 배틀로얄 씬에서 킬 점수와 부활 규칙을 찾을 수 있도록 싱글톤을 등록합니다.
        if (Instance == null)
        {
            Instance = this;
            ConfigureBattleRoyalPlayerCollision();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ConfigureBattleRoyalPlayerCollision()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        localPlayerLayer = LayerMask.NameToLayer("LocalPlayer");

        if (playerLayer < 0 || localPlayerLayer < 0)
        {
            Debug.LogWarning(
                "[배틀로얄] Player 또는 LocalPlayer 레이어를 찾지 못해 플레이어 충돌 설정을 변경하지 못했습니다.");
            return;
        }

        // 같은 스폰 지점에 생성된 로컬·원격 캐릭터가 서로 밀어 올리지 않도록 배틀로얄에서만 충돌을 끕니다.
        previousPlayerCollisionIgnored =
            Physics.GetIgnoreLayerCollision(
                playerLayer,
                localPlayerLayer);

        Physics.IgnoreLayerCollision(
            playerLayer,
            localPlayerLayer,
            true);

        changedPlayerCollisionRule = true;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        // 배틀로얄 씬을 나갈 때 다른 게임 모드의 기존 충돌 규칙을 복원합니다.
        if (changedPlayerCollisionRule)
        {
            Physics.IgnoreLayerCollision(
                playerLayer,
                localPlayerLayer,
                previousPlayerCollisionIgnored);
        }

        Instance = null;
    }

    private new void Start()
    {
        base.Start();

        // 씬 전환 직후에도 포톤 메시지와 플레이어 목록 갱신이 정상 작동하게 합니다.
        PhotonNetwork.IsMessageQueueRunning = true;
        maxPlayers = PhotonNetwork.CurrentRoom != null
            ? PhotonNetwork.CurrentRoom.MaxPlayers
            : 8;

        if (maxPlayers == 0)
        {
            maxPlayers = 8;
        }

        InitializePlayerUI();
        ResetLocalBattleScore();
        SortPlayerUI();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.onEmptyEsc = OpenSystemMenu;
        }
    }

    private void ResetLocalBattleScore()
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        // 완주 점수와 관계없이 배틀로얄 입장 시 킬 점수를 0점으로 초기화합니다.
        ExitGames.Client.Photon.Hashtable properties =
            new ExitGames.Client.Photon.Hashtable
            {
                ["Score"] = 0
            };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    private void SortPlayerUI()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        // 완주 없이 킬 점수만 높은 순서대로 플레이어 순위를 정렬합니다.
        List<Player> sortedPlayers = PhotonNetwork.PlayerList
            .OrderByDescending(player =>
                player.CustomProperties.ContainsKey("Score")
                    ? (int)player.CustomProperties["Score"]
                    : 0)
            .ToList();

        RefreshAndSortSlots(sortedPlayers);

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            int actorNumber = sortedPlayers[i].ActorNumber;
            if (!activePlayerSlots.TryGetValue(
                    actorNumber,
                    out BasePlayerSlot baseSlot))
            {
                continue;
            }

            RunPlayerSlot scoreSlot = baseSlot as RunPlayerSlot;
            if (scoreSlot == null) continue;

            int score = sortedPlayers[i].CustomProperties.ContainsKey("Score")
                ? (int)sortedPlayers[i].CustomProperties["Score"]
                : 0;

            scoreSlot.UpdateScore(score);
            scoreSlot.UpdateRank(i + 1);
        }
    }

    public void AddKillScore(int killerActorNumber)
    {
        if (currentState == GameState.Finish ||
            PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        Player killer =
            PhotonNetwork.CurrentRoom.GetPlayer(killerActorNumber);
        if (killer == null) return;

        // 공격자 자신의 클라이언트에서 점수를 올려 동시 처치 누락을 방지합니다.
        if (killer.IsLocal)
        {
            AddLocalKillScore();
        }
        else
        {
            photonView.RPC(nameof(RPC_AddLocalKillScore), killer);
        }
    }

    [PunRPC]
    private void RPC_AddLocalKillScore()
    {
        // 피해자의 사망 판정에서 전달된 1킬을 공격자의 점수에 반영합니다.
        AddLocalKillScore();
    }

    private void AddLocalKillScore()
    {
        if (currentState == GameState.Finish ||
            PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        int currentScore =
            PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Score")
                ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Score"]
                : 0;

        ExitGames.Client.Photon.Hashtable properties =
            new ExitGames.Client.Photon.Hashtable
            {
                ["Score"] = currentScore + 1
            };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    public override void OnPlayerPropertiesUpdate(
        Player targetPlayer,
        ExitGames.Client.Photon.Hashtable changedProperties)
    {
        if (!changedProperties.ContainsKey("Score")) return;

        SortPlayerUI();

        int currentScore = (int)changedProperties["Score"];

        // 마스터 클라이언트만 승리 처리를 시작하여 종료 RPC 중복 호출을 막습니다.
        if (currentScore >= winScore &&
            PhotonNetwork.IsMasterClient &&
            currentState != GameState.Finish)
        {
            FinishGame();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (activePlayerSlots.TryGetValue(
                otherPlayer.ActorNumber,
                out BasePlayerSlot slot))
        {
            slot.SetEmpty();
            activePlayerSlots.Remove(otherPlayer.ActorNumber);
        }

        SortPlayerUI();
    }

    [PunRPC]
    protected override void RPC_FinishGameUI(string winnerNickName)
    {
        StopAllCoroutines();
        currentState = GameState.Finish;

        // 전용 승리 UI가 없어도 콘솔과 카운트다운 UI로 결과를 표시합니다.
        if (winnerText != null)
        {
            winnerText.text =
                $"{winnerNickName}님이 {winScore}킬을 달성했습니다!";
            winnerText.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log(
                $"배틀로얄 종료: {winnerNickName}님이 승리했습니다.");
        }

        if (countdownText != null)
        {
            StartCoroutine(
                BattleRoyalFinishRoutine(winnerNickName));
        }
        else
        {
            CountFinish();
        }
    }

    private IEnumerator BattleRoyalFinishRoutine(
        string winnerNickName)
    {
        // 승자를 먼저 안내한 뒤 대기실 이동 카운트다운을 시작합니다.
        ShowMessage($"{winnerNickName}님이 승리했습니다!");
        yield return new WaitForSeconds(2f);
        yield return CountdownCoroutine(
            5,
            "{0}초 뒤 대기실로 이동합니다!",
            "대기실로 이동합니다.");
    }
}
