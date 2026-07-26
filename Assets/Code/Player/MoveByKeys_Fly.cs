using UnityEngine;
using UnityEngine.InputSystem;

namespace Photon.Pun.UtilityScripts
{
    public class MoveByKeys_Fly : MoveByKeys
    {
        [Header("Flight Specs")]
        public float ascendSpeed = 10f;

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

            finalMove.y += ascendInput * ascendSpeed;

            controller.Move(finalMove * Time.deltaTime);

            if (!isGrounded)
            {
                bool isMovingOrAscending = (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f || ascendInput > 0.1f);

                if (isMovingOrAscending)
                {
                    windSoundTimer += Time.deltaTime;
                    if (windSoundTimer >= 0.5f)
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