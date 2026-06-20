using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class PlayerRaceProgress : MonoBehaviourPun
{
    private MoveByKeys player;
    private void Start()
    {
        // Report the initial position
        if (photonView.IsMine && RunGameManager.Instance != null)
        {
            RunGameManager.Instance.ReportAndInitializePlayerInitialPos(this.gameObject);
        }
        if (photonView.IsMine)
        {
            player = GetComponent<MoveByKeys>();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        // Check if the trigger is a checkpoint
        if (other.CompareTag("Checkpoint"))
        {
            if (RunGameManager.Instance != null)
            {
                RunGameManager.Instance.ProcessLocalPlayerCheckpointTrigger(this.gameObject, other.transform);
            }
        }

        if (other.CompareTag("Invincible")) player.isInvincible = true;
    }

    public void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("Invincible")) player.isInvincible = false;
    }
}
