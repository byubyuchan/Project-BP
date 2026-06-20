using Photon.Pun;
using UnityEngine;

public class ObstacleLinear : Obstacle
{
    [SerializeField]
    private Transform dir;
    protected override void OnPlayerHit(GameObject player)
    {
        PhotonView targetPV = player.GetComponent<PhotonView>();
        if (targetPV != null)
        {
            Vector3 pushDir = dir.forward;

            pushDir.y = upwardForce;

            // 플레이어의 스크립트에 있는 넉백 RPC 호출
            targetPV.RPC("RPC_AddKnockback", RpcTarget.All, pushDir * knockbackForce);
        }
    }
}
