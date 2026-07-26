using Photon.Pun;
using Photon.Pun.UtilityScripts;
using System.Collections;
using UnityEngine;

public class BRController : MonoBehaviour
{
    private const int DefaultInvincibleEffectIndex = 8;

    private MoveByKeys playerMovement;
    private PhotonView playerPhotonView;
    private Coroutine protectionCoroutine;
    private int invincibleEffectIndex = DefaultInvincibleEffectIndex;

    // 부활한 로컬 캐릭터에게 지정된 시간만큼 무적 상태와 무적 이펙트를 적용합니다.
    public void Activate(float duration)
    {
        playerMovement = GetComponent<MoveByKeys>();
        playerPhotonView = GetComponent<PhotonView>();

        if (playerMovement == null)
        {
            Debug.LogWarning(
                $"{name}: 부활 무적을 적용할 MoveByKeys를 찾지 못했습니다.");
            return;
        }

        // 무적존과 동일한 이펙트 번호를 사용하여 두 무적 표시가 항상 일치하게 합니다.
        PlayerRaceProgress raceProgress = GetComponent<PlayerRaceProgress>();

        if (raceProgress != null)
        {
            invincibleEffectIndex = raceProgress.invincibleEffectIndex;
        }

        if (protectionCoroutine != null)
        {
            StopCoroutine(protectionCoroutine);
        }

        protectionCoroutine = StartCoroutine(
            ProtectionRoutine(Mathf.Max(0f, duration)));
    }

    private IEnumerator ProtectionRoutine(float duration)
    {
        // 부활 무적 시작과 동시에 무적존에서 사용하는 이펙트를 모든 플레이어에게 표시합니다.
        SetProtectionState(true);

        yield return new WaitForSeconds(duration);

        // 설정 시간이 끝나면 무적 상태와 이펙트를 함께 해제합니다.
        SetProtectionState(false);
        protectionCoroutine = null;
    }

    private void SetProtectionState(bool isActive)
    {
        if (playerMovement != null)
        {
            playerMovement.isInvincible = isActive;
        }

        // 무적존과 같은 네트워크 이펙트 호출을 사용하여 모든 화면에 동일하게 표시합니다.
        if (EffectManager.Instance != null && playerPhotonView != null && playerPhotonView.ViewID != 0)
        {
            EffectManager.Instance.RequestToggleAttachedEffect(invincibleEffectIndex,playerPhotonView.ViewID,isActive);
        }
    }
}
