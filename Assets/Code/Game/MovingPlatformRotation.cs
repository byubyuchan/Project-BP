using Photon.Pun;
using UnityEngine;
using UnityEngine.UIElements;

// 회전하는 플랫폼
public class MovingPlatformRotation : MovingPlatform
{
    // enum 값으로 회전하는 각도를 선택할 수 있음
    public enum Axis { X, Y, Z }
    public Axis axis = Axis.Y;
    Vector3 rotatedOffset;
    public float rotationSpeed = 90f;

    protected override void HandlePlatformMovement()
    {
        transform.Rotate(0, rotationSpeed * Time.fixedDeltaTime, 0);
    }

    protected override void UpdatePlayerPositions()
    {
        float angle = rotationSpeed * Time.fixedDeltaTime;
        foreach (var player in playersOnPlatform)
        {
            if (player != null && player.enabled)
            {
                var pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    Vector3 offset = player.transform.position - transform.position;
                    switch (axis)
                    {
                        case Axis.X:
                            rotatedOffset = Quaternion.Euler(angle, 0, 0) * offset;
                            break;
                        case Axis.Y:
                            rotatedOffset = Quaternion.Euler(0, angle, 0) * offset;
                            break;
                        case Axis.Z:
                            rotatedOffset = Quaternion.Euler(0, 0, angle) * offset;
                            break;
                    }

                    Vector3 moveDir = rotatedOffset - offset;

                    if (player.isGrounded) moveDir.y = -2f;
                    player.Move(moveDir);
                }
            }
        }
    }
}