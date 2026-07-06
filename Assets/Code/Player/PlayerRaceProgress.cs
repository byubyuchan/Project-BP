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

        if (other.CompareTag("Checkpoint"))
        {
            if (RunGameManager.Instance != null)
            {
                RunGameManager.Instance.ProcessLocalPlayerCheckpointTrigger(this.gameObject, other.transform);
            }
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
