using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using static UnityEngine.Rendering.DebugManager;

// 플레이어가 공격 준비 (Aim) 상태일 때 카메라를 줌인하는 스크립트
public class PlayerCameraZoom : MonoBehaviourPun, IPunObservable
{
    [Header("Dependencies")]
    public MoveByKeys playerMovement;

    [Header("Camera Zoom Settings")]
    public Transform cameraTransform;
    public float zoomXOffset = 3.6f;
    public float zoomYOffset = 3.6f;
    public float zoomFOV = 40f;
    public float zoomSpeed = 5f;

    public GameObject crosshairImage;

    private float defaultXOffset;
    private float defaultYOffset;
    private float defaultFOV;
    private Camera camComponent;

    private bool isDefaultValuesSet = false;
    private bool isAiming;

    [Header("Animation Rigging")]
    public Transform rigAimTarget;

    void Awake()
    {
        // Awake에서는 내 몸에 붙은 컴포넌트나 static 참조만 세팅하는 것이 안전합니다.
        if (playerMovement == null) playerMovement = GetComponent<MoveByKeys>();
    }

    // 플레이어가 풀링을 통해 재사용되기 때문에 Awake 이후 OnEnable에서 확실하게 청소
    void OnEnable()
    {
        if (!photonView.IsMine) return;

        if (CrosshairController.instance != null)
        {
            crosshairImage = CrosshairController.instance.gameObject;
            crosshairImage.SetActive(false);
        }

        if (playerMovement != null)
        {
            playerMovement.isLoadingAttack = false;
            isAiming = false;
        }

        if (Camera.main != null)
        {
            camComponent = GetComponentInChildren<Camera>(true);
            cameraTransform = camComponent.transform;


            if (!isDefaultValuesSet)
            {
                defaultXOffset = cameraTransform.localPosition.x;
                defaultYOffset = cameraTransform.localPosition.y;
                defaultFOV = camComponent.fieldOfView;
                isDefaultValuesSet = true;
            }

            Vector3 localPos = cameraTransform.localPosition;
            localPos.x = defaultXOffset;
            localPos.y = defaultYOffset;
            cameraTransform.localPosition = localPos;
            camComponent.fieldOfView = defaultFOV;
        }

        if (AutoCameraCanvas.Instance != null)
        {
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.ClearLocalScreenEffects();
            }

            AutoCameraCanvas.Instance.gameObject.SetActive(true);
            AutoCameraCanvas.Instance.RegisterLocalPlayerCamera(camComponent);
        }
    }

    void OnDisable()
    {
        if (camComponent != null && AutoCameraCanvas.Instance != null)
        {
            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.ClearLocalScreenEffects();
            }

            AutoCameraCanvas.Instance.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!photonView.IsMine || playerMovement == null || cameraTransform == null || camComponent == null || crosshairImage == null) return;

        isAiming = playerMovement.isLoadingAttack;

        float targetX = isAiming ? zoomXOffset : defaultXOffset;
        float targetFOV = isAiming ? zoomFOV : defaultFOV;

        if (crosshairImage.activeSelf == isAiming &&
            Mathf.Abs(cameraTransform.localPosition.x - targetX) < 0.001f &&
            Mathf.Abs(camComponent.fieldOfView - targetFOV) < 0.01f)
        {
            return; 
        }

        HandleZoom(isAiming);
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 hitPoint = ray.GetPoint(playerMovement.maxRange);
            rigAimTarget.position = hitPoint;
        }
    }

    void HandleZoom(bool isAiming)
    {
        float targetX = isAiming ? zoomXOffset : defaultXOffset;
        float targetY = isAiming ? zoomYOffset : defaultYOffset;
        float targetFOV = isAiming ? zoomFOV : defaultFOV;

        // 현재 조준선 활성화 상태와 타겟 상태가 다를 때만 갱신 (매 프레임 SetActive 호출 방지 성능 최적화)
        if (crosshairImage.activeSelf != isAiming)
        {
            crosshairImage.SetActive(isAiming);
        }

        // 부드러운 카메라 무빙 Lerp
        Vector3 localPos = cameraTransform.localPosition;
        localPos.x = Mathf.Lerp(localPos.x, targetX, Time.deltaTime * zoomSpeed);
        localPos.y = Mathf.Lerp(localPos.y, targetY, Time.deltaTime * zoomSpeed);
        cameraTransform.localPosition = localPos;

        camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    // 조준선을 안전하게 찾아서 꺼주는 서브 루틴
    private void ResetCrosshair()
    {
        if (crosshairImage == null)
        {
            crosshairImage = GameObject.FindWithTag("Crosshair");
        }

        if (crosshairImage != null)
        {
            crosshairImage.SetActive(false);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (rigAimTarget != null)
            {
                stream.SendNext(rigAimTarget.position);
            }
        }
        else
        {
            if (rigAimTarget != null)
            {
                rigAimTarget.position = (Vector3)stream.ReceiveNext();
            }
        }
    }


}