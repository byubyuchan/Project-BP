using Photon.Pun;
using UnityEngine;

public class ObstacleSleep : Obstacle
{
    protected override void OnPlayerHit(GameObject player)
    {
        PhotonView targetPV = player.GetComponent<PhotonView>();
        if (targetPV != null)
        {
            targetPV.RPC("RPC_Sleep", RpcTarget.All, upwardForce);
        }
    }
}
