using Photon.Pun;
using UnityEngine;

// 앞뒤로 이동하는 플랫폼
public class MovingPlatformLinear : MovingPlatform
{
    public float movingSpeed = 2f;
    public float distance = 5f;
    public Vector3 dir = Vector3.forward;

    private Vector3 startPosition;
    private Vector3 lastPosition;

    void Start() { startPosition = lastPosition = transform.position; }

    // 핑퐁 함수를 통해 이동 후 이동한만큼 역으로 다시 이동할 수 있도록 구현
    protected override void HandlePlatformMovement()
    {
        float pingPong = Mathf.PingPong(Time.time * movingSpeed, distance);
        transform.position = startPosition + (dir.normalized * pingPong);
    }

    protected override void UpdatePlayerPositions()
    {
        Vector3 platformDelta = transform.position - lastPosition;
        lastPosition = transform.position;

        foreach (var player in playersOnPlatform)
        {
            if (player != null && player.enabled)
            {
                var pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    Vector3 moveDir = platformDelta;
                    if (player.isGrounded) moveDir.y = -2f;
                    player.Move(moveDir);
                }
            }
        }
    }
}