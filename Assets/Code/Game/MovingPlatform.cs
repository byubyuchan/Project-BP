using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

// 플랫폼들을 구현하기 위한 추상화 클래스
public abstract class MovingPlatform : MonoBehaviourPun
{
    protected List<CharacterController> playersOnPlatform = new List<CharacterController>();

    protected virtual void FixedUpdate()
    {
        // 마스터 클라이언트만 물리적 위치/회전 계산 (나머지는 PhotonTransformView가 동기화)
        if (PhotonNetwork.IsMasterClient)
        {
            HandlePlatformMovement();
        }

        UpdatePlayerPositions();
    }

    // 자식 클래스에서 각각 구현할 움직임 로직
    protected abstract void HandlePlatformMovement();
    protected abstract void UpdatePlayerPositions();

    // 플랫폼에 올라탄 플레이어와 플랫폼에서 내려간 플레이어를 배열로 저장
    protected void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var cc = other.GetComponent<CharacterController>();
            if (cc != null && !playersOnPlatform.Contains(cc)) playersOnPlatform.Add(cc);
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var cc = other.GetComponent<CharacterController>();
            if (cc != null) playersOnPlatform.Remove(cc);
        }
    }
}