using UnityEngine;
using Photon.Pun;

public class PlayerCameraSetup : MonoBehaviourPun
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    // 각자의 플레이어에 맞게 플레이어에 내장된 카메라를 메인 카메라로 변경하는 코드
    void OnEnable()
    {
        if (photonView.IsMine)
        {
            Camera oldMainCam = Camera.main;

            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                playerCamera.tag = "MainCamera";
                // 카메라 우선순위 높임
                Camera.SetupCurrent(playerCamera);
            }

            if (audioListener != null)
            {
                audioListener.enabled = true;
            }

            if (oldMainCam != null && oldMainCam != playerCamera)
            {
                oldMainCam.gameObject.SetActive(false);
            }
        }
        else
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (audioListener != null) audioListener.enabled = false;
        }
    }
}