using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// TPZone과는 다르게 랜덤한 위치로 텔레포트 시키는 코드
public class RandomTPZone : MonoBehaviour
{
    [Header("Destination Settings")]
    // 텔레포트 시킬 목적지 리스트 (최소 3개 이상 권장)
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

        // 랜덤하게 한 곳 선택
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

        if (cc != null) cc.enabled = true;

        // 사운드 매니저가 있다면 효과음 재생
        if (AudioManager.instance != null && !string.IsNullOrEmpty(teleportSFX))
        {
            AudioManager.instance.PlaySFX(teleportSFX, pos);
        }
    }
}