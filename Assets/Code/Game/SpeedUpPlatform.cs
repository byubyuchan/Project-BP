using Photon.Pun;
using UnityEngine;

public class SpeedUpPlatform : MovingPlatform
{
    [Header("가속 설정")]
    public float pushForce = 10f; // 밀어주는 힘 (가속도 느낌)

    // 이 발판은 스스로 움직이지 않으니 이 함수는 비워둡니다.
    protected override void HandlePlatformMovement() { }

    protected override void UpdatePlayerPositions()
    {
        // 발판의 정면 방향 (화살표 방향)
        Vector3 pushDirection = transform.forward;

        foreach (var player in playersOnPlatform)
        {
            if (player != null && player.enabled)
            {
                var pv = player.GetComponent<PhotonView>();

                // 내 캐릭터일 때만 실행
                if (pv != null && pv.IsMine)
                {
                    Vector3 moveDistance = pushDirection * pushForce * Time.fixedDeltaTime;

                    // 바닥에 붙어있게 처리
                    if (player.isGrounded) moveDistance.y = -2f;

                    player.Move(moveDistance);
                }
            }
        }
    }
}