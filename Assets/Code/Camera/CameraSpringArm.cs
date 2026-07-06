using UnityEngine;

public class CameraSpringArm : MonoBehaviour
{
    [Header("카메라 세팅")]
    public Transform mainCamera;
    public float defaultDistance = 12.2f;

    // 카메라의 방어막 크기 (이걸 키울수록 벽/바닥에서 멀리 떨어져서 멈춤)
    [SerializeField]
    public float collisionRadius = 4f;

    [Header("충돌 세팅")]
    public LayerMask obstacleLayer;

    void LateUpdate()
    {
        float currentX = mainCamera.localPosition.x;
        float currentY = mainCamera.localPosition.y;

        Vector3 rayOrigin = transform.TransformPoint(new Vector3(currentX, currentY, 0f));
        RaycastHit hit;

        float padding = 0.1f; // 방어막이 커졌으니 패딩은 살짝만!

        // 0.25f 대신 방어막 크기(collisionRadius) 변수 사용!
        if (Physics.SphereCast(rayOrigin, collisionRadius, -transform.forward, out hit, defaultDistance, obstacleLayer))
        {
            float safeZ = -(hit.distance - padding);
            safeZ = Mathf.Clamp(safeZ, -defaultDistance, -0.5f);
            mainCamera.localPosition = new Vector3(currentX, currentY, safeZ);
        }
        else
        {
            mainCamera.localPosition = new Vector3(currentX, currentY, -defaultDistance);
        }
    }
}