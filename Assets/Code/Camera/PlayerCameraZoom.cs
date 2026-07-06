using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;
using UnityEngine.InputSystem; // 마우스 휠 입력을 위해 New Input System 추가
using static UnityEngine.Rendering.DebugManager;

// 플레이어가 공격 준비 (Aim) 상태일 때 카메라를 줌인하는 스크립트
public class PlayerCameraZoom : MonoBehaviourPun, IPunObservable
{
    [Header("Dependencies")]
    public MoveByKeys playerMovement;

    [Header("Camera Zoom Settings")]
    public Transform cameraTransform;
    public float zoomXOffset = 7.2f;
    public float zoomYOffset = 3.6f;
    public float zoomFOV = 40f;
    public float zoomSpeed = 5f;

    // 모바일은 버튼으로 할당하여 줌 기능 구현
    [Header("Scroll Zoom Settings (New)")]
    public float scrollSensitivity = 1000f; // 마우스 휠 민감도 (New Input System은 값이 커서 작게 설정)
    public float minDefaultFOV = 40f;       // 평상시 최대 줌인 FOV
    public float maxDefaultFOV = 110f;       // 평상시 최대 줌아웃 FOV
    public float minAimFOV = 5f;           // 조준 시 최대 줌인 FOV (스나이퍼 느낌)
    public float maxAimFOV = 90f;           // 조준 시 최대 줌아웃 FOV

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
        if (playerMovement == null) playerMovement = GetComponent<MoveByKeys>();
    }

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

        // 상태창이나 채팅창 열려있으면 휠 줌 안 먹히게 방어
        if (playerMovement.isMenuOpen || playerMovement.isUIMode) return;

        isAiming = playerMovement.isLoadingAttack;

        // 마우스 휠 스크롤 값 읽기 (New Input System)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (isAiming)
                {
                    // 조준 중일 때는 조준 FOV 조절
                    zoomFOV -= scroll * scrollSensitivity * 100f;
                    zoomFOV = Mathf.Clamp(zoomFOV, minAimFOV, maxAimFOV);
                }
                else
                {
                    // 평상시일 때는 기본 FOV 조절
                    defaultFOV -= scroll * scrollSensitivity * 100f;
                    defaultFOV = Mathf.Clamp(defaultFOV, minDefaultFOV, maxDefaultFOV);
                }
            }
        }

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

        if (crosshairImage.activeSelf != isAiming)
        {
            crosshairImage.SetActive(isAiming);
        }

        // 부드러운 카메라 무빙 Lerp
        Vector3 localPos = cameraTransform.localPosition;
        localPos.x = Mathf.Lerp(localPos.x, targetX, Time.deltaTime * zoomSpeed);
        localPos.y = Mathf.Lerp(localPos.y, targetY, Time.deltaTime * zoomSpeed);
        cameraTransform.localPosition = localPos;

        // 휠로 바뀐 타겟 FOV를 향해 카메라가 부드럽게 변함!
        camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

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