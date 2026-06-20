using UnityEngine;
using Photon.Pun;

// 장애물의 부모가 되는 클래스, 플레이어와 충돌 시 넉백 효과를 적용하는 기능을 담당
public class Obstacle : MonoBehaviourPun
{
    [Header("Obstacle Settings")]
    public float knockbackForce = 15f; // 날아가는 힘
    public float upwardForce = 0.5f;   // 살짝 위로 띄워주는 힘

    protected virtual void OnPlayerHit(GameObject player)
    {
        PhotonView targetPV = player.GetComponent<PhotonView>();
        if (targetPV != null)
        {
            // 충돌 방향 계산 (장애물 -> 플레이어)
            Vector3 pushDir = (player.transform.position - transform.position).normalized;
            pushDir.y = upwardForce; // 살짝 공중으로 뜨게 함

            // 플레이어의 스크립트에 있는 넉백 RPC 호출
            targetPV.RPC("RPC_AddKnockback", RpcTarget.All, pushDir * knockbackForce);
        }
    }

    protected virtual void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Player"))
        {
            OnPlayerHit(col.gameObject);
        }
    }
}