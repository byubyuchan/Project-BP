using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using static BaseGameManager;

public class PlayerRaceProgress : MonoBehaviourPun
{
    private MoveByKeys player;

    [Header("Effect Settings")]
    public int invincibleEffectIndex = 8;

    private MoveByKeys GetPlayer()
    {
        if (player != null) return player;

        player = GetComponent<MoveByKeys>();
        if (player == null) player = GetComponentInParent<MoveByKeys>();

        return player;
    }

    private void Start()
    {
        // 포지션 보고
        if (photonView.IsMine && RunGameManager.Instance != null)
        {
            RunGameManager.Instance.ReportAndInitializePlayerInitialPos(this.gameObject);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("Portal"))
        {
            RunGameManager.Instance.hasPortalTicket = true;
        }

        // Check if the trigger is a checkpoint
        if (other.CompareTag("Checkpoint"))
        {
            if (RunGameManager.Instance == null) return;

            int hitIndex = RunGameManager.Instance.checkpoints.IndexOf(other.transform);

            int expectedIndex = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PhotonKeys.GOAL)
                                ? (int)PhotonNetwork.LocalPlayer.CustomProperties[PhotonKeys.GOAL] : 0;

            if (hitIndex == expectedIndex)
            {
                if (!RunGameManager.Instance.hasPortalTicket)
                {
                    RunGameManager.Instance.RequestTeleport(this.gameObject);
                    return;
                }

                // 정상 통과면 티켓 소모
                RunGameManager.Instance.hasPortalTicket = false;
            }

            RunGameManager.Instance.ProcessLocalPlayerCheckpointTrigger(this.gameObject, other.transform);
        }

        if (other.CompareTag("Invincible"))
        {
            MoveByKeys myPlayer = GetPlayer();
            if (myPlayer != null) myPlayer.isInvincible = true;

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RequestToggleAttachedEffect(invincibleEffectIndex, photonView.ViewID, true);
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("Invincible"))
        {
            MoveByKeys myPlayer = GetPlayer();
            if (myPlayer != null) myPlayer.isInvincible = false;

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RequestToggleAttachedEffect(invincibleEffectIndex, photonView.ViewID, false);
            }
        }
    }
}
