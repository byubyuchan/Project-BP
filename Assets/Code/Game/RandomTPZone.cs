using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// (변경) 여러 목적지 중 하나를 무작위로 선택해 플레이어를 순간이동시킵니다.
public class RandomTPZone : MonoBehaviour
{
    [Header("Destination Settings")]
    // 순간이동 목적지 목록입니다.
    [SerializeField] private List<Transform> destinationPoints = new List<Transform>();

    [Header("Effect Settings")]
    [SerializeField] private string teleportSFX = "Teleport";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        if (destinationPoints == null || destinationPoints.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: 목적지 좌표가 설정되지 않았습니다!");
            return;
        }

        // 등록된 목적지 중 한 곳을 무작위로 선택합니다.
        int randomIndex = Random.Range(0, destinationPoints.Count);
        Transform target = destinationPoints[randomIndex];

        if (target != null)
        {
            PerformTeleport(other.gameObject, target.position, target.rotation);
        }
    }

    private void PerformTeleport(GameObject playerObj, Vector3 pos, Quaternion rot)
    {
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerObj.transform.position = pos;
        playerObj.transform.rotation = rot;

        // (추가) 순간이동 전 위치에서 누적된 중력과 넉백을 제거합니다.
        Photon.Pun.UtilityScripts.MoveByKeys movement =
            playerObj.GetComponent<Photon.Pun.UtilityScripts.MoveByKeys>();
        if (movement != null) movement.ResetMotionAfterTeleport();

        if (cc != null) cc.enabled = true;

        playerObj.GetComponent<PhotonView>().RPC("RPC_SizeReset", RpcTarget.All);

        // 변경된 위치와 크기를 물리 엔진에 즉시 반영합니다.
        Physics.SyncTransforms();

        // 사운드 매니저가 연결되어 있을 때만 순간이동 효과음을 재생합니다.
        if (AudioManager.instance != null && !string.IsNullOrEmpty(teleportSFX))
        {
            AudioManager.instance.PlaySFX(teleportSFX, pos);
        }
    }
}
