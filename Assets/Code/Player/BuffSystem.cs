using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView), typeof(HPController))]
public class BuffSystem : MonoBehaviourPun
{
    private Dictionary<string, Coroutine> activeBuffs = new Dictionary<string, Coroutine>();
    private HPController hpController;

    void Awake()
    {
        hpController = GetComponent<HPController>();
    }

    private void OnDisable()
    {
        ClearAllBuffs();
    }

    // ==========================================
    // 화상 버프 루틴 (이펙트 제어 X, 타이머와 데미지만 수행)
    // ==========================================
    private IEnumerator BurnRoutine(float duration, float damagePerTick)
    {
        float tickInterval = 0.5f;
        float elapsed = 0f;

        // 이펙트는 이미 투사체가 켰으므로 여기서 호출하지 않음!

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            if (hpController != null && !hpController.isDead)
            {
                // 데미지 틱 발생
                photonView.RPC("RPC_TakeDamage", photonView.Owner, damagePerTick);
            }
            else break; // 사망 시 즉시 종료
        }

        activeBuffs.Remove("Burn");
    }

    // 캐릭터 사망 시 호출하여 상태 초기화
    public void ClearAllBuffs()
    {
        StopAllCoroutines();
        activeBuffs.Clear();
    }

    // ==========================================
    // 버프 적용 입구 (투사체 등에서 호출)
    // ==========================================

    [PunRPC]
    public void RPC_ApplyBuff(string buffName, float duration, float power)
    {
        if (!photonView.IsMine) return;

        if (activeBuffs.ContainsKey(buffName))
        {
            if (activeBuffs[buffName] != null) StopCoroutine(activeBuffs[buffName]);
            activeBuffs.Remove(buffName);
        }

        // 새 버프 실행
        switch (buffName)
        {
            case "Burn":
                activeBuffs["Burn"] = StartCoroutine(BurnRoutine(duration, power));
                break;
        }
    }
}