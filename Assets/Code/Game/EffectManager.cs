using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EffectManager : MonoBehaviourPun
{
    public static EffectManager Instance;

    public GameObject[] explosionEffects;
    public GameObject[] localScreenEffects;

    private Dictionary<GameObject, Coroutine> effectCoroutines = new Dictionary<GameObject, Coroutine>();

    void Awake()
    {
        // 싱글톤 세팅 및 중복 방지
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // 1. 모두에게 보이는 폭발
    public void RequestExplosion(int index, Vector3 pos)
    {
        photonView.RPC("RPC_PlayEffect", RpcTarget.All, index, pos);
    }

    // 2. 나만 보이는 피격 효과
    public void RequestLocalEffect(int index, PhotonView targetPV)
    {
        if (targetPV == null || targetPV.Owner == null) return;

        photonView.RPC("RPC_PlayLocalEffect", targetPV.Owner, index);
    }

    // 3. 달라붙은 피격 효과
    public void RequestAttachedExplosion(int index, int targetViewID)
    {
        photonView.RPC("RPC_PlayAttachedEffect", RpcTarget.All, index, targetViewID);
    }

    public void RequestToggleAttachedEffect(int index, int targetViewID, bool isON)
    {
        photonView.RPC("RPC_ToggleAttachedEffect", RpcTarget.All, index, targetViewID, isON);
    }

    private IEnumerator ReturnToPoolRoutine(GameObject fx, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fx != null)
        {
            effectCoroutines.Remove(fx);
            fx.transform.SetParent(PhotonPoolingManager.instance.transform);
            PhotonPoolingManager.instance.Destroy(fx);
        }
    }

    public void StopEffectCoroutine(GameObject fx)
    {
        if (fx != null && effectCoroutines.TryGetValue(fx, out Coroutine routine))
        {
            if (routine != null) StopCoroutine(routine);
            effectCoroutines.Remove(fx);
        }
    }

    public void ClearLocalScreenEffects()
    {
        if (localScreenEffects == null) return;

        foreach (GameObject effectObj in localScreenEffects)
        {
            if (effectObj != null && effectObj.activeSelf)
            {
                if (effectObj.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
                {
                    
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                effectObj.SetActive(false);
            }
        }
    }

    [PunRPC]
    void RPC_PlayEffect(int index, Vector3 pos)
    {
        if (index < explosionEffects.Length)
        {
            GameObject fx = PhotonPoolingManager.instance.Instantiate("VFX/" + explosionEffects[index].name, pos, Quaternion.identity);
            fx.SetActive(true);

            StopEffectCoroutine(fx);
            effectCoroutines[fx] = StartCoroutine(ReturnToPoolRoutine(fx, 2.0f));
        }
    }

    [PunRPC]
    void RPC_PlayLocalEffect(int index)
    {
        if (index < localScreenEffects.Length)
        {
            GameObject effectObj = localScreenEffects[index];

            effectObj.SetActive(true);

            if (effectObj.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ps.Play(true);
            }
        }
    }

    [PunRPC]
    void RPC_PlayAttachedEffect(int index, int targetViewID)
    {
        if (index >= explosionEffects.Length) return;

        PhotonView targetPV = PhotonView.Find(targetViewID);
        if (targetPV == null) return;

        Transform spineTransform = targetPV.transform;
        MoveByKeys moveScript = targetPV.GetComponent<MoveByKeys>();
        if (moveScript != null && moveScript.effectTransform != null)
        {
            spineTransform = moveScript.effectTransform;
        }

        GameObject fx = PhotonPoolingManager.instance.Instantiate("VFX/" + explosionEffects[index].name, spineTransform.position, spineTransform.rotation);
        fx.transform.SetParent(spineTransform);
        fx.SetActive(true);

        StopEffectCoroutine(fx);
        effectCoroutines[fx] = StartCoroutine(ReturnToPoolRoutine(fx, 2.0f));
    }

    [PunRPC]
    void RPC_ToggleAttachedEffect(int index, int targetViewID, bool isON)
    {
        if (index >= explosionEffects.Length) return;

        PhotonView targetPV = PhotonView.Find(targetViewID);
        if (targetPV == null) return;

        Transform spineTransform = targetPV.transform;
        MoveByKeys moveScript = targetPV.GetComponent<MoveByKeys>();

        if (moveScript != null && moveScript.effectTransform != null)
        {
            spineTransform = moveScript.effectTransform;
        }

        string targetEffectName = explosionEffects[index].name;

        if (isON)
        {
            foreach (Transform child in spineTransform)
            {
                if (child.name.Contains(targetEffectName)) return;
            }

            GameObject fx = PhotonPoolingManager.instance.Instantiate("VFX/" + targetEffectName, spineTransform.position, spineTransform.rotation);
            fx.transform.SetParent(spineTransform);
            fx.SetActive(true);
        }
        else
        {
            foreach (Transform child in spineTransform)
            {
                if (child.name.Contains(targetEffectName))
                {
                    GameObject fx = child.gameObject;

                    StopEffectCoroutine(fx);
                    effectCoroutines[fx] = StartCoroutine(ReturnToPoolRoutine(fx, 0.1f));
                    break;
                }
            }
        }
    }
}