using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Photon.Pun.UtilityScripts
{
    public class MoveByKeys_FlameShot : MoveByKeys
    {
        private bool isFiring = false;

        private GameObject networkFlameObj;
        private ParticleSystem[] flames;

        private Coroutine soundLoopCoroutine;

        protected new void OnDisable()
        {
            if (photonView.IsMine && isFiring)
            {
                photonView.RPC("RPC_SetFlame", RpcTarget.All, false);
            }

            base.OnDisable();
        }

        protected override void HandleAttack()
        {
            if (isChatting() || isUIMode || isMenuOpen || isSleep) return;

            if (isLoadingAttack && isAttackPressed && !animator.GetCurrentAnimatorStateInfo(1).IsName("Attack"))
            {
                if (!isFiring)
                {
                    isFiring = true;

                    if (networkFlameObj == null && photonView.IsMine)
                    {
                        networkFlameObj = PhotonNetwork.Instantiate("Projectile/" + projectile, firePoint.position, firePoint.rotation);

                        photonView.RPC("RPC_InitFlame", RpcTarget.AllBuffered, networkFlameObj.GetComponent<PhotonView>().ViewID);
                    }

                    photonView.RPC("RPC_SetFlame", RpcTarget.All, true);
                }
            }
            else
            {
                if (isFiring)
                {
                    isFiring = false;
                    photonView.RPC("RPC_SetFlame", RpcTarget.All, false);
                }
            }

            if (isFiring && flames != null && flames.Length > 0 && flames[0] != null)
            {
                if (photonView.IsMine)
                {
                    Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                    RaycastHit hit;
                    Vector3 targetPoint;

                    if (Physics.Raycast(ray, out hit, maxRange, ~aimLayerMask))
                    {
                        targetPoint = hit.point;

                        float distToTarget = Vector3.Distance(Camera.main.transform.position, hit.point);
                        float distToMuzzle = Vector3.Distance(Camera.main.transform.position, firePoint.position);

                        if (distToTarget < distToMuzzle + 5f)
                        {
                            targetPoint = ray.GetPoint(50f);
                        }
                    }
                    else targetPoint = ray.GetPoint(maxRange);

                    Vector3 aimDirection = (targetPoint - firePoint.position).normalized;

                    networkFlameObj.transform.position = firePoint.position;
                    networkFlameObj.transform.rotation = Quaternion.LookRotation(aimDirection);
                }
            }
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            if (photonView.IsMine && isFiring)
            {
                photonView.RPC("RPC_SetFlame", newPlayer, true);
            }
        }

        [PunRPC]
        public void RPC_InitFlame(int viewID)
        {
            PhotonView targetPV = PhotonView.Find(viewID);
            if (targetPV != null)
            {
                networkFlameObj = targetPV.gameObject;
                flames = networkFlameObj.GetComponentsInChildren<ParticleSystem>();
            }
        }

        [PunRPC]
        public void RPC_SetFlame(bool state)
        {
            if (flames != null)
            {
                foreach (var ps in flames)
                {
                    var emission = ps.emission;
                    emission.enabled = state;

                    if (state && !ps.isPlaying)
                    {
                        ps.Play();
                    }
                }
            }
        }
    }
}