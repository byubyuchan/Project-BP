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

    [Header("Zoom Buttons")]
    public MobileZoomButton zoomInButton;
    public RectTransform zoomInRect;
    public MobileZoomButton zoomOutButton;
    public RectTransform zoomOutRect;

    [Header("Judgment Criteria Setting")]
    public float tapTimeLimit = 0.2f;
    public float tapDistanceLimit = 20f;

    private Vector2 pointerDownPosition;
    private float pointerDownTime;
    private bool isDragging;

    private Vector2 currentDragDelta;

    private bool isAttacking = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        pointerDownTime = Time.unscaledTime;
        isDragging = false;
        currentDragDelta = Vector2.zero;

        // 1. 공격 버튼 영역 체크
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
            isDragging = true;
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
            SendValueToControl(currentDragDelta * lookSensitivity * 0.01f);
            currentDragDelta = Vector2.zero;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        SendValueToControl(Vector2.zero);

        if (isAttacking)
        {
            isAttacking = false;
            virtualAttackButton.OnPointerUp(eventData);
        }
    }
    private bool IsTouchingAttackButton(Vector2 screenPosition)
    {
        if (attackButtonRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(attackButtonRect, screenPosition);
    }

    public void ForwardDragDelta(Vector2 delta)
    {
        isDragging = true;
        currentDragDelta = delta;
    }
}