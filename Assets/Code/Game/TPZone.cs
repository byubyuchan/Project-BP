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

        // 싱글톤 참조가 없는 게임 모드에서도 현재 게임 매니저를 찾는다.
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

            // 캐릭터를 교체하기 전에 목적지 체크포인트의 진행도를 먼저 저장한다.
            // 진행도를 먼저 갱신해야 새 캐릭터가 이후 부활 위치를 올바르게 사용한다.
            if (RunGameManager.Instance != null &&
                !RunGameManager.Instance.ProcessLocalPlayerPortalTransition(
                    other.gameObject, fallbackTarget.transform))
            {
                return;
            }

            fallbackPosition = fallbackTarget.transform.position;
            fallbackRotation = fallbackTarget.transform.rotation;
            manager.TeleportCharacter(other.gameObject, fallbackPosition, fallbackRotation);

            // 다음 체크포인트에 도달하면 기존 캐릭터를 제거하고 새 캐릭터로 교체한다.
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
