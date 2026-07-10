using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviourPunCallbacks
{
    public static EffectManager Instance;

    public GameObject[] explosionEffects;
    public GameObject[] localScreenEffects;

    private Dictionary<GameObject, Coroutine> effectCoroutines = new Dictionary<GameObject, Coroutine>();

    // Key: "캐릭터ViewID_이펙트인덱스", Value: 생성된 이펙트 GameObject
    private Dictionary<string, GameObject> activeLoopEffects = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
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

        string key = targetViewID + "_" + index;

        if (isON)
        {
            // 중복 생성 원천 차단
            if (activeLoopEffects.ContainsKey(key) && activeLoopEffects[key] != null) return;

            PhotonView targetPV = PhotonView.Find(targetViewID);
            if (targetPV == null) return;

            Transform spineTransform = targetPV.transform;
            MoveByKeys moveScript = targetPV.GetComponent<MoveByKeys>();

            if (moveScript != null && moveScript.effectTransform != null)
            {
                spineTransform = moveScript.effectTransform;
            }

            string targetEffectName = explosionEffects[index].name;

            GameObject fx = PhotonPoolingManager.instance.Instantiate("VFX/" + targetEffectName, spineTransform.position, spineTransform.rotation);
            fx.transform.SetParent(spineTransform);
            fx.SetActive(true);

            activeLoopEffects[key] = fx;
        }
        else
        {
            // 꺼질 때는 장부에서 찾아서 안전하게 반납
            if (activeLoopEffects.TryGetValue(key, out GameObject fx))
            {
                if (fx != null)
                {
                    StopEffectCoroutine(fx);
                    effectCoroutines[fx] = StartCoroutine(ReturnToPoolRoutine(fx, 0.1f));
                }
                activeLoopEffects.Remove(key);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        List<string> keysToRemove = new List<string>();

        // 현재 돌아가는 모든 루핑 장부를 전수조사
        foreach (var kvp in activeLoopEffects)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[0], out int viewID))
            {
                if (viewID / 1000 == otherPlayer.ActorNumber || kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (string key in keysToRemove)
        {
            if (activeLoopEffects.TryGetValue(key, out GameObject fx))
            {
                if (fx != null)
                {
                    StopEffectCoroutine(fx);
                    fx.SetActive(false);
                    fx.transform.SetParent(PhotonPoolingManager.instance.transform);
                    PhotonPoolingManager.instance.Destroy(fx);
                    Debug.Log($"<color=red>[EffectManager] 탈주 플레이어의 고아 이펙트 강제 철거 완료: {key}</color>");
                }
            }
            activeLoopEffects.Remove(key);
        }
    }
}