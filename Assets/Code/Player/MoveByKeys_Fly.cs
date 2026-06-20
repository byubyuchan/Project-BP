using UnityEngine;
using UnityEngine.InputSystem;

namespace Photon.Pun.UtilityScripts
{
    public class MoveByKeys_Fly : MoveByKeys
    {
        [Header("Flight Specs")]
        public float ascendSpeed = 10f;   // 상승/하강 속도

        private float ascendInput;

        [Header("# Fly Sound Settings")]
        private float windSoundTimer = 0f;

        protected override void OnJump(InputValue value)
        {
            if (!photonView.IsMine) return;
            ascendInput = value.isPressed ? 1f : 0f;
        }

        protected override void HandleMovement()
        {
            if (animator != null)
            {
                animator.SetFloat("H", horizontalInput, 0.1f, Time.deltaTime);
                animator.SetFloat("V", verticalInput, 0.1f, Time.deltaTime);
                animator.SetBool("IsGround", false);
            }

            Vector3 forwardMove = cameraPivot.forward;
            Vector3 rightMove = transform.right;

            Vector3 moveDir = (forwardMove * verticalInput) + (rightMove * horizontalInput);
            Vector3 finalMove = moveDir * speed;

            // 스페이스바 누르면 수직 상승
            finalMove.y += ascendInput * ascendSpeed;

            // 중력(velocity.y) 연산을 아예 빼버리고 순수 비행 벡터로 냅다 밀어버림!
            controller.Move(finalMove * Time.deltaTime);

            if(!isGrounded)
            {
                bool isMovingOrAscending = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f || ascendInput > 0.1f);

                if (isMovingOrAscending)
                {
                    windSoundTimer += Time.deltaTime;
                    if (windSoundTimer >= 0.5f) // 1초 쿨타임 계산
                    {
                        windSoundTimer = 0f;

                        photonView.RPC("RPC_PlayActionSound", RpcTarget.All, "AirStep");
                    }
                }
                else
                {
                    windSoundTimer = 0.45f;
                }
            }

        }
    }
}