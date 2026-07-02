using UnityEngine;

public class CameraSpringArm : MonoBehaviour
{
    [Header("카메라 세팅")]
    public Transform mainCamera;
    public float defaultDistance = 12.2f;
    public float minDistance = 3f;

    [Header("충돌 세팅")]
    public LayerMask obstacleLayer;

    void LateUpdate()
    {
        float currentX = mainCamera.localPosition.x;
        float currentY = mainCamera.localPosition.y;

        Vector3 desiredLocalPos = new Vector3(currentX, currentY, -defaultDistance);
        Vector3 desiredWorldPos = transform.TransformPoint(desiredLocalPos);

        Vector3 direction = (desiredWorldPos - transform.position).normalized;
        float maxDistance = desiredLocalPos.magnitude;

        RaycastHit hit;

        if (Physics.SphereCast(transform.position, 0.2f, direction, out hit, maxDistance, obstacleLayer))
        {
            float hitRatio = hit.distance / maxDistance;

            float safeZ = Mathf.Clamp(-defaultDistance * hitRatio, -defaultDistance, -minDistance);

            mainCamera.localPosition = new Vector3(currentX, currentY, safeZ);
        }
        else
        {
            mainCamera.localPosition = desiredLocalPos;
        }
    }
}