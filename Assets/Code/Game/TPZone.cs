using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TPZone : MonoBehaviour
{
    [Header("Temp Fallback")]
    [SerializeField] private GameObject fallbackTarget;

    private Vector3 fallbackPosition;
    private Quaternion fallbackRotation;

    [SerializeField] private bool isGoal = true;

    private void Awake()
    {
        if (fallbackTarget != null)
        {
            fallbackPosition = fallbackTarget.transform.position;
            fallbackRotation = fallbackTarget.transform.rotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        // 싱글톤 유무와 관계없이 현재 씬의 게임 매니저를 기준으로 처리합니다.
        BaseGameManager manager = Object.FindFirstObjectByType<BaseGameManager>();
        if (manager == null) return;

        if (isGoal)
        {
            if (fallbackTarget == null)
            {
                Debug.LogWarning($"{name}: 포탈 목적지 체크포인트가 설정되지 않았습니다.");
                manager.RequestTeleport(other.gameObject);
                return;
            }

            RunGameManager runGameManager = manager as RunGameManager;

            // 순서가 다른 포탈은 순간이동시키기 전에 차단하여 체크포인트 건너뛰기를 막습니다.
            if (runGameManager != null &&
                !runGameManager.IsExpectedCheckpoint(fallbackTarget.transform))
            {
                manager.RequestTeleport(other.gameObject);
                return;
            }

            fallbackPosition = fallbackTarget.transform.position;
            fallbackRotation = fallbackTarget.transform.rotation;
            manager.TeleportCharacter(other.gameObject, fallbackPosition, fallbackRotation);

            // Transform 순간이동 결과를 물리 엔진에 즉시 반영합니다.
            Physics.SyncTransforms();

            // OnTriggerEnter 재발생에 의존하지 않고 순간이동 직후 목적지 체크포인트를 직접 기록합니다.
            if (runGameManager != null &&
                !runGameManager.ProcessLocalPlayerPortalTransition(
                    other.gameObject, fallbackTarget.transform))
            {
                return;
            }

            //  체크포인트 기록이 끝난 뒤 기존 캐릭터를 제거하고 새 캐릭터로 교체합니다.
            if (PlayerSpawner.instance != null)
            {
                PlayerSpawner.instance.InstantReSpawn(fallbackPosition, fallbackRotation);
            }
            else
            {
                Debug.LogWarning($"{name}: PlayerSpawner를 찾지 못해 캐릭터를 교체하지 못했습니다.");
            }
        }
        else
        {
            if (manager.RequestTeleport(pv.gameObject)) return;

            manager.TeleportCharacter(other.gameObject, fallbackPosition, fallbackRotation);
        }
    }
}
