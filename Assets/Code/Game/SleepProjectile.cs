using Photon.Pun;
using UnityEngine;

public class SleepProjectile : Projectile
{
    // 플레이어나 맵에 닿으면 폭발
    protected override void OnCollisionEnter(Collision col)
    {
        if (!photonView.IsMine || hasExploded) return;

        if (col.gameObject.CompareTag("Player"))
        {
            Explode(col.gameObject);
        }
        else if (col.gameObject.CompareTag("Map"))
        {
            Explode();
        }
    }

    protected override void OnTriggerEnter(Collider col)
    {
        if (!photonView.IsMine || hasExploded) return;

        if (col.gameObject.CompareTag("Player"))
        {
            Explode(col.gameObject);
        }
        else if (col.gameObject.CompareTag("Map"))
        {
            Explode();
        }
    }

    // 폭발한 투사체가 여러 오브젝트에 부딪혀 여러 번 폭발하는 것을 방지
    protected override void Explode(GameObject target = null)
    {
        hasExploded = true;
        CancelInvoke("DestroySelf");

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.RequestExplosion(explosionEffectIndex, transform.position);
        }

        if (target != null && target.CompareTag("Player"))
        {
            PhotonView targetPV = target.GetComponent<PhotonView>();
            if (targetPV != null)
            {
                targetPV.RPC("RPC_Sleep", RpcTarget.All, explosionForce);
            }
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}