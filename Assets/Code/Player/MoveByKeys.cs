using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Photon.Pun.UtilityScripts
{
    [RequireComponent(typeof(CharacterController), typeof(PhotonView))]
    public class MoveByKeys : MonoBehaviourPunCallbacks
    {
        public float speed = 5f;            // 이동 속도 (기존 1000은 너무 컸으니 조정)
        public float jumpHeight = 2f;       // 점프 높이
        public float gravity = -20f;        // 중력 세기
        public float rotationSpeed = 0.1f;

        protected CharacterController controller;
        protected Animator animator;
        protected Vector3 velocity;           // 수직 속도 (중력/점프용)
        protected bool isGrounded;

        [Header("Rotation Settings")]
        public Transform cameraPivot;
        public float mouseSensitivity = 3f;
        public float upRange = 70f;
        public float downRange = 20f;
        public float zoomUpRange = 90f;
        public float zoomDownRange = 40f;

        public float verticalRotation = 0f; // 현재 수직 회전값 저장용

        [Header("Projectile Settings")]
        public string projectile;
        public Transform firePoint;
        public Transform aimPoint;
        public float maxRange = 10000f;

        protected Vector3 impact = Vector3.zero;

        public bool isUIMode = false;
        public bool isMenuOpen = false;
        public bool isSleep = false;

        public bool isLoadingAttack = false;

        [Header("Aim Settings")]
        public LayerMask aimLayerMask;

        [Header("Attack Settings")]
        public float attackCooldown = 0.5f;
        public float lastAttackTime;

        protected float originalSpeed;
        protected float horizontalInput;
        protected float verticalInput;

        protected Vector3 localSize;
        protected ItemData currentItem;

        [Header("Input System")]
        protected Vector2 rawMoveInput;
        protected Vector2 rawLookInput;
        protected Vector2 mouseDelta;

        [Header("Action State")]
        protected bool isAttackPressed = false;

        [Header("Effect Transform")]
        public Transform effectTransform;

        protected Coroutine sleepCoroutine;

        public bool isBlocked = false;

        [Header("면역 or 무적")]
        public bool isNoCC = false;
        public bool isInvincible = false;

        [Header("# Footstep Settings")]
        private float footstepTimer = 0f;
        [SerializeField] private Transform dustSpawnPosition;

        public void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            localSize = transform.localScale;
        }

        public void Start()
        {
            if (!photonView.IsMine)
            {
                if (TryGetComponent<PlayerInput>(out PlayerInput pi))
                {
                    pi.enabled = false;
                }
                this.enabled = false;

                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            originalSpeed = speed;

            if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (photonView.IsMine)
            {
                isUIMode = false;
                isMenuOpen = false;
                isLoadingAttack = false;
                rawMoveInput = Vector2.zero;
                rawLookInput = Vector2.zero;

                impact = Vector3.zero;
                velocity = Vector3.zero;
                isInvincible = false;

                SetLayerRecursively(gameObject, LayerMask.NameToLayer("LocalPlayer"));
                UpdateCursorState();
            }
        }

        public override void OnDisable()
        {
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("Player"));
            base.OnDisable();
        }

        void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (photonView.IsMine && isLoadingAttack)
            {
                photonView.RPC("RPC_LoadAction", newPlayer, "ReadyToAttack", true);
            }

            if (photonView.IsMine && isSleep)
            {
                photonView.RPC("RPC_Sleep", newPlayer, 5f);
            }

            if (transform.localScale.x > localSize.x + 0.1f)
            {
                photonView.RPC("RPC_SizeUp", newPlayer);
            }
            else if (transform.localScale.x < localSize.x - 0.1f)
            {
                photonView.RPC("RPC_SizeDown", newPlayer);
            }
        }

        void OnMove(InputValue value)
        {
            if (!photonView.IsMine) return;
            Vector2 val = value.Get<Vector2>();
            rawMoveInput = val;
        }

        void OnLook(InputValue value)
        {
            if (!photonView.IsMine) return;
            rawLookInput = value.Get<Vector2>();
        }

        protected virtual void OnJump(InputValue value)
        {
            if (!photonView.IsMine) return; // 추가
            if (isChatting() || isUIMode || isMenuOpen || isSleep) return;

            if (isGrounded && value.isPressed)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                photonView.RPC("RPC_TriggerAction", RpcTarget.All, "Jump");
                photonView.RPC("RPC_PlayActionSound", RpcTarget.All, "Jump");
            }
        }

        void OnAttack(InputValue value)
        {
            if (!photonView.IsMine) return;
            isAttackPressed = value.isPressed;
        }

        void OnAim()
        {
            if (!photonView.IsMine) return; // 추가
            if (isChatting() || isUIMode || isMenuOpen || isSleep) return;

            bool isCurrentAttack = animator.GetCurrentAnimatorStateInfo(1).IsName("Attack");
            bool isNextAttack = animator.GetNextAnimatorStateInfo(1).IsName("Attack");

            if (!isCurrentAttack && !isNextAttack)
            {
                isLoadingAttack = !isLoadingAttack;
                isAttackPressed = false;
                photonView.RPC("RPC_LoadAction", RpcTarget.All, "ReadyToAttack", isLoadingAttack);
            }
        }

        void OnOpenUI()
        {
            if (!photonView.IsMine) return; // 추가
            if (isMenuOpen) return;

            isUIMode = !isUIMode;
            UpdateCursorState();
        }

        void OnChatting()
        {
            if (!photonView.IsMine) return;
            if (isMenuOpen) return;

            if (ChatManager.Instance != null)
            {
                ChatManager.Instance.ToggleChat();
            }
        }

        void OnOpenESC()
        {
            if (!photonView.IsMine) return; // 추가
            UIManager.Instance.OpenEscapeUI();
        }

        public void OnUseItem()
        {
            if (!photonView.IsMine) return;

            if (currentItem == null) return;

            if (AudioManager.instance != null)
            {
                photonView.RPC("RPC_PlayActionSound", RpcTarget.All, "UseItem");
            }

            if (currentItem.RPCName == "RPC_Magnet")
            {
                photonView.RPC(currentItem.RPCName, RpcTarget.All, currentItem.range, currentItem.power);
            }
            else
            {
                photonView.RPC(currentItem.RPCName, RpcTarget.All);
            }

            // 사용 후 데이터 비우기
            currentItem = null;

            if (ItemSlotUI.Instance != null)
            {
                ItemSlotUI.Instance.ClearSlot();
            }
        }

        // =================================================================
        protected virtual void HandleMovement()
        {
            if (animator != null)
            {
                animator.SetFloat("H", horizontalInput, 0.1f, Time.deltaTime);
                animator.SetFloat("V", verticalInput, 0.1f, Time.deltaTime);
                animator.SetBool("IsGround", isGrounded);
            }

            Vector3 moveDir = (transform.forward * verticalInput) + (transform.right * horizontalInput);
            if (impact.magnitude > 0.2f) impact = Vector3.Lerp(impact, Vector3.zero, 5f * Time.deltaTime);
            else impact = Vector3.zero;

            Vector3 finalMove = (moveDir * speed) + impact;

            if (isGrounded && velocity.y <= 0)
            {
                finalMove.y = -100f;
            }

            controller.Move(finalMove * Time.deltaTime);
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        protected virtual void HandleAttack()
        {
            if (isChatting() || isUIMode || isMenuOpen || isSleep) return;

            if (isLoadingAttack && isAttackPressed && !animator.GetCurrentAnimatorStateInfo(1).IsName("Attack"))
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;
                    photonView.RPC("RPC_TriggerAction", RpcTarget.All, "Attack");
                }
            }
        }

        public void Update()
        {
            if (!photonView.IsMine) return;

            if (Camera.main == null) return;

            HandleFootstepTimer();

            // 1. 상태 체크 (채팅/메뉴/UI모드일 때 입력값 강제 0 처리)
            isBlocked = isChatting() || isMenuOpen || isUIMode || isSleep;

            if (isBlocked)
            {
                horizontalInput = 0;
                verticalInput = 0;
                mouseDelta = Vector2.zero;
            }
            else
            {

                horizontalInput = Mathf.Clamp(rawMoveInput.x, -1f, 1f);
                verticalInput = Mathf.Clamp(rawMoveInput.y, -1f, 1f);
                mouseDelta = rawLookInput;
            }

            bool wasGrounded = isGrounded;

            // 2. 바닥 체크
            isGrounded = controller.isGrounded;

            if (!wasGrounded && isGrounded)
            {
                photonView.RPC("RPC_PlayActionSound", RpcTarget.All, "Landing");
            }

            // 3. 회전 처리
            if (!isBlocked)
            {
                float sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
                transform.Rotate(Vector3.up * mouseDelta.x * rotationSpeed * sensitivity);

                verticalRotation -= mouseDelta.y * mouseSensitivity * sensitivity;
                float currentMax = isLoadingAttack ? zoomUpRange : upRange;
                float currentMin = isLoadingAttack ? -zoomDownRange : -downRange;
                verticalRotation = Mathf.Clamp(verticalRotation, currentMin, currentMax);

                if (cameraPivot != null)
                    cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
            }
            HandleMovement();
            HandleAttack();
        }

        public void SetMenuOpenState(bool isOpen)
        {
            if (!photonView.IsMine) return;

            isMenuOpen = isOpen;
            UpdateCursorState();
        }

        private void UpdateCursorState()
        {
            bool showCursor = isUIMode || isMenuOpen;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = showCursor;
        }

        protected bool isChatting()
        {
            return EventSystem.current != null &&
                   EventSystem.current.currentSelectedGameObject != null &&
                   EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;
        }

        public void Shoot()
        {
            if (!photonView.IsMine) return;

            //isLoadingAttack = false;
            //photonView.RPC("RPC_LoadAction", RpcTarget.All, "ReadyToAttack", isLoadingAttack);

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit; 
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out hit, maxRange, ~aimLayerMask))
            {
                targetPoint = hit.point;

                float distToTarget = Vector3.Distance(Camera.main.transform.position, hit.point);
                float distToMuzzle = Vector3.Distance(Camera.main.transform.position, firePoint.position);

                if (distToTarget < distToMuzzle + 5f)
                {
                    targetPoint = ray.GetPoint(50f);
                }
            }
            else targetPoint = ray.GetPoint(maxRange);

            Vector3 aimDirection = (targetPoint - firePoint.position).normalized;

            PhotonNetwork.Instantiate("Projectile/" + projectile, firePoint.position, Quaternion.LookRotation(aimDirection));
        }

        public void Quake()
        {
            if (!photonView.IsMine) return;

            if (isGrounded)
            {
                RaycastHit hit;

                Vector3 rayStart = transform.position + new Vector3(0,10f,0);

                // 플레이어의 y축 10f 에서부터 아래로 50f까지 바닥 찾기
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
                {
                    // 맞은 곳이 있다면 ProjectOnPlane으로 경사면에 맞춰서 발사체 위치와 회전 계산
                    Vector3 spawnPos = hit.point + (hit.normal * 0.05f);
                    Vector3 forwardOnSlope = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                    Quaternion spawnRot = Quaternion.LookRotation(forwardOnSlope, hit.normal);

                    PhotonNetwork.Instantiate("Projectile/" + projectile, spawnPos, spawnRot);
                }
            }
        }

        public void HitScan()
        {
            if (!photonView.IsMine) return;

            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out hit, maxRange, ~aimLayerMask))
            {
                targetPoint = hit.point;

                float distToTarget = Vector3.Distance(Camera.main.transform.position, hit.point);
                float distToMuzzle = Vector3.Distance(Camera.main.transform.position, firePoint.position);

            }
            else return;

            Vector3 aimDirection = (targetPoint - firePoint.position).normalized;

            PhotonNetwork.Instantiate("Projectile/" + projectile, targetPoint, Quaternion.LookRotation(aimDirection));
        }

        public void ApplySpeedBoost(float additionalSpeed)
        {
            if (verticalInput > 0.1f) speed = originalSpeed + additionalSpeed;
            else ResetSpeed();
        }

        public void ResetSpeed()
        {
            speed = originalSpeed;
        }

        public System.Collections.IEnumerator WakeUpAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            photonView.RPC("RPC_WakeUp", RpcTarget.All);
        }

        private void HandleFootstepTimer()
        {
            if (isChatting() || isMenuOpen || isUIMode || isSleep) return;

            bool isMoving = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f);

            if (isGrounded && isMoving)
            {
                footstepTimer += Time.deltaTime;
                if (footstepTimer >= 0.25f) // 0.5초 주기
                {
                    footstepTimer = 0f;

                    photonView.RPC("RPC_PlayActionSound", RpcTarget.All, "Run");
                }
            }
            else
            {
                footstepTimer = 0.2f;
            }
        }

        [PunRPC]
        public void RPC_TakeDamage(float damage) 
        {
            if (isInvincible) return;

            if (photonView.IsMine && damage > 0f)
            {
                HPController hpController = GetComponent<HPController>();
                if (hpController != null && hpController.Hp >= 0f)
                {
                    hpController.Hp -= damage;

                    EffectManager.Instance.RequestExplosion(2, effectTransform.position);
                    if (!hpController.isDead && hpController.Hp <= 0f)
                    {
                        hpController.Die();
                    }
                }
            }
        }

        [PunRPC]
        public void RPC_AddKnockback(Vector3 force)
        {
            if (isNoCC || isInvincible) return;

            if (photonView.IsMine)
            {
                impact += force;
                velocity.y = 0.5f;
            }
        }

        [PunRPC] // 좌클릭 공격
        public void RPC_TriggerAction(string triggerName)
        {
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }

        [PunRPC] // 우클릭 줌
        public void RPC_LoadAction(string triggerName, bool state)
        {
            if (animator != null)
            {
                animator.SetBool(triggerName, state);
            }
        }

        // ======================Item===========================

        [PunRPC]
        public void RPC_GetItem(string itemName)
        {
            // 나(당사자)만 실행
            if (!photonView.IsMine) return;

            if (currentItem) return;

            // 경로는 Assets/Resources/Items/ 안에 SO 파일들이 있어야 합니다.
            currentItem = Resources.Load<ItemData>("Items/" + itemName);

            if (currentItem != null)
            {
                Debug.Log($"<color=cyan>[아이템 획득]</color> {itemName}!");
                // 여기서 UI 아이콘(currentItem.itemIcon) 등을 업데이트하면 됩니다.

                if (ItemSlotUI.Instance != null)
                {
                    ItemSlotUI.Instance.SetItem(currentItem.itemIcon);
                }
                AudioManager.instance.PlaySFX("GetItem", this.transform.position);
            }
            else
            {
                Debug.LogError($"아이템 데이터를 찾을 수 없습니다: {itemName}. Resources/Items 폴더를 확인하세요!");
            }
        }

        [PunRPC]
        public void RPC_SizeDown()
        {
            transform.localScale *= 0.5f;
        }

        [PunRPC]
        public void RPC_SizeUp()
        {
            transform.localScale *= 2f;
        }

        [PunRPC]
        public void RPC_SizeReset()
        {
            transform.localScale = localSize;
        }

        [PunRPC]
        public void RPC_Magnet(float radius, float totalStr)
        {
            if (isNoCC || isInvincible) return;

            if (!photonView.IsMine) return;

            StartCoroutine(DoMagnet(radius, totalStr));
        }

        [PunRPC]
        public void RPC_Sleep(float time)
        {
            if (isNoCC || isInvincible) return;

            isSleep = true;
            if (animator != null) animator.SetBool("IsSleep", true);

            // 2. 당사자만 적용 (입력 및 카메라 제어)
            if (photonView.IsMine)
            {
                if (sleepCoroutine != null) StopCoroutine(sleepCoroutine);

                if (isLoadingAttack)
                {
                    isLoadingAttack = false;
                    photonView.RPC("RPC_LoadAction", RpcTarget.All, "ReadyToAttack", false);
                }

                verticalRotation = 0f;
                if (cameraPivot != null) cameraPivot.localRotation = Quaternion.identity;
                sleepCoroutine = StartCoroutine(WakeUpAfterDelay(time));
            }
        }

        [PunRPC]
        public void RPC_WakeUp()
        {
            isSleep = false;
            if (animator != null) animator.SetBool("IsSleep", false);
        }

        private System.Collections.IEnumerator DoMagnet(float radius, float totalStr)
        {
            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
                foreach (Collider hit in colliders)
                {
                    if (hit.CompareTag("Player") && hit.gameObject != this.gameObject)
                    {
                        PhotonView targetPV = hit.GetComponent<PhotonView>();
                        if (targetPV != null)
                        {
                            Vector3 diff = transform.position - hit.transform.position;
                            // 아주 가까워지면(1m 이내) 더 이상 당기지 않음 (뒤로 넘어가는 것 방지)
                            if (diff.magnitude < 1.5f) continue;

                            Vector3 pullDirection = diff.normalized;

                            targetPV.RPC("RPC_AddKnockback", RpcTarget.All, pullDirection * (totalStr * Time.deltaTime));
                        }
                    }
                }
                elapsed += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }
        }

        [PunRPC]
        public void RPC_PlayActionSound(string prefix)
        {
            int randomIndex = Random.Range(1, 4);

            if (prefix == "AirStep" || prefix == "UseItem")
            {
                // 딕셔너리에는 "AirStep"이라는 이름으로 파일 1개만 등록해 두면 됨
                AudioManager.instance.PlaySingleClipVariants(prefix, this.transform.position, randomIndex);
            }
            else
            {
                // 기존에 3개씩 파일 넣어서 잘 쓰던 것들(Run, Jump, Landing 등)은 기존 방식 100% 유지
                string soundKey = prefix + randomIndex;
                AudioManager.instance.PlayDynamicSFX(soundKey, this.transform.position, false);
            }

            // 먼지 재사용 및 Landing 시 Spine에 먼지가 하나 더 생성되는 오류
            // 작은 먼지 크기 차이를 넓게 두고 모양을 변경해 다양성 있도록 개선
            if (!photonView.IsMine) return;

            if (prefix == "Run")
            {
                EffectManager.Instance.RequestExplosion(4, dustSpawnPosition.position);
            }
            else if (prefix == "Jump")
            {
                EffectManager.Instance.RequestExplosion(5, dustSpawnPosition.position);
            }
            else if (prefix == "Landing")
            {
                EffectManager.Instance.RequestExplosion(6, dustSpawnPosition.position);
            }
            else if (prefix == "AirStep")
            {
                EffectManager.Instance.RequestExplosion(7, dustSpawnPosition.position);
            }
        }
    }
}