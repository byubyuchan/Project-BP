using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using System.Collections;

public class Touchpad : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Camera Rotation (Drag)")]
    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_LookControlPath;
    protected override string controlPathInternal { get => m_LookControlPath; set => m_LookControlPath = value; }
    public float lookSensitivity = 0.5f;

    [Header("Attack (Tap)")]
    public OnScreenButton virtualAttackButton;
    public RectTransform attackButtonRect;

    [Header("Judgment Criteria Setting")]
    public float tapTimeLimit = 0.2f; // 탭으로 간주되는 최대 시간
    public float tapDistanceLimit = 20f; // 탭으로 간주되는 최대 이동 거리

    private Vector2 pointerDownPosition;
    private float pointerDownTime;
    private bool isDragging;

    private Vector2 currentDragDelta;

    // 유저가 옵션에서 설정한 민감도 배율을 저장할 변수
    private float userSensitivityMultiplier = 1.0f;

    private bool isAttacking = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        // 터치 시작 시 위치와 시간을 기록
        pointerDownPosition = eventData.position;
        pointerDownTime = Time.unscaledTime;
        isDragging = false;
        currentDragDelta = Vector2.zero;

        // 터치를 시작할 때마다 PlayerPrefs에 저장된 최신 민감도 값을 불러옴
        userSensitivityMultiplier = PlayerPrefs.GetFloat("TouchSensitivity", 1.0f);

        if (IsTouchingAttackButton(eventData.position))
        {
            isAttacking = true;
            virtualAttackButton.OnPointerDown(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Vector2.Distance(eventData.position, pointerDownPosition) > tapDistanceLimit)
        {
            isDragging = true; // 이동 거리가 탭 기준을 초과하면 드래그로 간주
        }

        if (isDragging)
        {
            currentDragDelta = eventData.delta;
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            // 저장해둔 드래그 델타를 카메라 회전에 적용
            SendValueToControl(currentDragDelta * lookSensitivity * userSensitivityMultiplier);

            // 전송 후 드래그 델타 초기화 (다음 프레임에서 0이 전송되면서 카메라 회전이 멈추도록)
            currentDragDelta = Vector2.zero;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        SendValueToControl(Vector2.zero); // 드래그가 끝나면 카메라 회전 정지

        if (isAttacking)
        {
            isAttacking = false;
            virtualAttackButton.OnPointerUp(eventData);
        }

        //if ((Time.unscaledTime - pointerDownTime) <= tapTimeLimit)
        //{
        //    if (virtualAttackButton != null)
        //    {
        //        isAttacking = !isAttacking; // 공격 상태 반전

        //        if (isAttacking)
        //        {
        //            virtualAttackButton.OnPointerDown(eventData); // 공격 켜기
        //        }
        //        else
        //        {
        //            virtualAttackButton.OnPointerUp(eventData);   // 공격 끄기
        //        }
        //    }
        //}

        //// 드래그가 아니고, 탭으로 간주되는 시간과 이동 거리 내에 있다면 공격 트리거
        //if (!isDragging && (Time.unscaledTime - pointerDownTime) <= tapTimeLimit)
        //{
        //    if (virtualAttackButton != null)
        //    {
        //        // 인풋 시스템 정식 루트로 '버튼 눌림(1)' 신호 전송
        //        virtualAttackButton.OnPointerDown(eventData);

        //        // 0.05초 뒤에 떼는 동작 실행
        //        StartCoroutine(ReleaseAttackButton(eventData));
        //    }
        //}
    }

    private bool IsTouchingAttackButton(Vector2 screenPosition)
    {
        if (attackButtonRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(attackButtonRect, screenPosition);
    }

    private IEnumerator ReleaseAttackButton(PointerEventData eventData)
    {
        yield return new WaitForSeconds(0.05f);
        if (virtualAttackButton != null)
        {
            // 인풋 시스템 정식 루트로 '버튼 뗌(0)' 신호 전송
            virtualAttackButton.OnPointerUp(eventData);
        }
    }
}
