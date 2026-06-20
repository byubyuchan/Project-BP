using UnityEngine;
using Photon.Pun;

public class ObstacleWreckingBall : Obstacle
{
    public enum Axis { X, Y, Z }
    public enum SwingMode { Pendulum, Continuous }

    [Header("Pendulum Settings")]
    public Transform pivot;
    public Axis swingAxis = Axis.Z;
    public SwingMode mode = SwingMode.Pendulum;
    public float swingSpeed = 2f;
    public float swingAngle = 45f;
    public float offset = 0f;

    private void Update()
    {
        if (pivot != null)
        {
            double time = PhotonNetwork.Time;
            float angle;

            if (mode == SwingMode.Pendulum)
            {
                angle = (float)(System.Math.Sin(time * swingSpeed + offset) * swingAngle);
            }
            else
            {
                angle = (float)((time * swingSpeed * 100f + offset) % 360);
            }

            switch (swingAxis)
            {
                case Axis.X:
                    pivot.localRotation = Quaternion.Euler(angle, 0, 0);
                    break;
                case Axis.Y:
                    pivot.localRotation = Quaternion.Euler(0, angle, 0);
                    break;
                case Axis.Z:
                    pivot.localRotation = Quaternion.Euler(0, 0, angle);
                    break;
            }
        }
    }

    protected override void OnPlayerHit(GameObject player)
    {
        PhotonView targetPV = player.GetComponent<PhotonView>();

        if (targetPV != null)
        {
            Vector3 pushDir = (player.transform.position - transform.position).normalized;
            pushDir.y = 0f; // 수평으로만 날아가게
            pushDir = pushDir.normalized;

            // 플레이어에게 넉백 명령 전송
            targetPV.RPC("RPC_AddKnockback", RpcTarget.All, pushDir * knockbackForce);
        }
    }
}
