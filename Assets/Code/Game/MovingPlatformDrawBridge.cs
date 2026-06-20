using Photon.Pun;
using UnityEngine;

// 위아래로 움직이는 플랫폼 (교각)
public class MovingPlatformDrawbridge : MovingPlatform
{
    [Header("Drawbridge Settings")]
    public float swingSpeed = 1.5f;
    public float maxAngle = 90f;
    public Vector3 rotationAxis = Vector3.right;

    private Quaternion lastRotation;
    private Quaternion initialLocalRotation;

    void Start()
    {
        initialLocalRotation = transform.localRotation;
        lastRotation = transform.rotation;
    }

    protected override void HandlePlatformMovement()
    {
        double time = PhotonNetwork.Time;
        // Sin (-1 ~ 1) -> +1f = Sin (0 ~ 2) -> * 0.5f = Sin (0 ~ 1) 을 swingSpeed로 조절
        float factor = (float)((System.Math.Sin(time * swingSpeed) + 1f) * 0.5f);
        float currentAngle = factor * maxAngle;

        transform.localRotation = initialLocalRotation * Quaternion.Euler(rotationAxis * currentAngle);
    }

    // 위에 올라탄 플레이어도 움직이는 플랫폼에 맞추어 이동합니다.
    protected override void UpdatePlayerPositions()
    {
        // 플레이어 이동 로직은 동일합니다.
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
        lastRotation = transform.rotation;

        foreach (var player in playersOnPlatform)
        {
            if (player != null && player.enabled)
            {
                var pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    Vector3 offset = player.transform.position - transform.position;
                    Vector3 rotatedOffset = deltaRotation * offset;
                    Vector3 moveDir = rotatedOffset - offset;

                    // 플레이어가 플랫폼에 박혀있도록 하기 위해 바닥으로 약간 눌러줍니다.
                    if (player.isGrounded) moveDir.y -= 2f;

                    player.Move(moveDir);
                }
            }
        }
    }
}