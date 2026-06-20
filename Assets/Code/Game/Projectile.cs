using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static Cinemachine.CinemachineTargetGroup;
using static UnityEngine.GraphicsBuffer;

public class Projectile : MonoBehaviourPun
{
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 10f;

    [Header("Explosion Settings")]
    public float explosionRadius = 5f;   // 폭발 반경
    public float explosionForce = 15f;    // 밀어내는 힘

    [SerializeField]
    protected int explosionEffectIndex = 0;

    [SerializeField]
    protected int screenEffectIndex = 0;

    protected bool hasExploded = false;
    protected bool isNPCProjectile;

    private HashSet<int> hitPlayers = new HashSet<int>(20);

    [SerializeField] public string soundKey = "Sniper";

    protected virtual void OnEnable()
    {
        int randomIndex = Random.Range(1, 4);
        AudioManager.instance.PlaySingleClipVariants(soundKey, this.transform.position, randomIndex);

        hasExploded = false;
        hitPlayers.Clear();

        isNPCProjectile = gameObject.CompareTag("NPCProjectile");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = transform.forward * speed;
        }

        if (lifeTime > 0f)
        {
            bool isLocalOrMine = (photonView == null || photonView.ViewID == 0 || photonView.IsMine);
            if (isLocalOrMine)
            {
                CancelInvoke("DestroySelf");
                Invoke("DestroySelf", lifeTime);
            }
        }
    }

    void DestroySelf()
    {
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // =====================================================================
    // 1. 단발성 물리 투사체 (로켓, 총알 등)
    // =====================================================================
    virtual protected void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine || hasExploded) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Map") || collision.gameObject.CompareTag("Checkpoint") || collision.gameObject.CompareTag("Dummy"))
        {
            if (collision.gameObject.CompareTag("Player") && !isNPCProjectile)
            {
                PhotonView targetPV = collision.gameObject.GetComponent<PhotonView>();
                if (targetPV != null && targetPV.OwnerActorNr == photonView.OwnerActorNr)
                {
                    return;
                }
            }
            Explode();
        }
    }

    // =====================================================================
    // 2. 장판기, 지진, 광역기 (투명 Trigger 콜라이더용)
    // =====================================================================
    virtual protected void OnTriggerEnter(Collider col)
    {
        if (!photonView.IsMine || hasExploded) return;

        if (col.gameObject.CompareTag("Dummy"))
        {
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RequestExplosion(explosionEffectIndex, col.transform.position);
            }
            return;
        }

        if (col.gameObject.CompareTag("Player"))
        {
            PhotonView targetPV = col.gameObject.GetComponent<PhotonView>();
            if (targetPV != null)
            {
                if (targetPV.OwnerActorNr == photonView.OwnerActorNr) return;
                if (!hitPlayers.Add(targetPV.OwnerActorNr)) return;

                if (damage > 0f)
                {
                    if (EffectManager.Instance != null)
                    {
                        EffectManager.Instance.RequestExplosion(explosionEffectIndex, targetPV.transform.position);
                    }


                    targetPV.RPC("RPC_TakeDamage", targetPV.Owner, damage);
                }
                ApplyKnockback(col.gameObject, targetPV);
            }
        }
    }

    // =====================================================================
    // 폭발 및 넉백 처리 로직
    // =====================================================================
    virtual protected void Explode(GameObject target = null)
    {
        hasExploded = true;
        CancelInvoke("DestroySelf");

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.RequestExplosion(explosionEffectIndex, transform.position);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                PhotonView targetPV = hit.gameObject.GetComponent<PhotonView>();

                if (targetPV != null)
                {
                    if (targetPV.OwnerActorNr == photonView.OwnerActorNr && !isNPCProjectile) continue;

                    ApplyKnockback(hit.gameObject, targetPV);

                    if (damage > 0f)
                    {
                        targetPV.RPC("RPC_TakeDamage", targetPV.Owner, damage);
                    }
                }
            }
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    void ExplodeNoFX()
    {
        hasExploded = true;

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                PhotonView targetPV = hit.gameObject.GetComponent<PhotonView>();

                if (targetPV != null)
                {
                    if (targetPV.OwnerActorNr == photonView.OwnerActorNr) continue;

                    ApplyKnockback(hit.gameObject, targetPV);
                }
            }
        }
    }

    public void ApplyKnockback(GameObject target, PhotonView targetPV)
    {
        Vector3 direction = (target.transform.position - transform.position).normalized;
        direction.y = 0.5f;

        if (targetPV != null)
        {
            targetPV.RPC("RPC_AddKnockback", targetPV.Owner, direction * explosionForce);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}