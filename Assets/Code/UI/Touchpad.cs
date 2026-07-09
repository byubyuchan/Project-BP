using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class Touchpad : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static Touchpad Instance;

    [Header("Camera Rotation (Drag)")]
    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_LookControlPath;
    protected override string controlPathInternal { get => m_LookControlPath; set => m_LookControlPath = value; }

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
    public bool isDragging;

    private Vector2 dragBuffer;
    public Vector2 MobileDragDelta { get; private set; }

    private bool isAttacking = false;

    private void Awake()
    {
        Instance = this;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;
        isDragging = false;
        dragBuffer = Vector2.zero;
        MobileDragDelta = Vector2.zero;

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
            dragBuffer += eventData.delta;
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            MobileDragDelta = dragBuffer;
            dragBuffer = Vector2.zero; // 소모한 버퍼 초기화

            SendValueToControl(Vector2.zero);
        }
        else
        {
            MobileDragDelta = Vector2.zero;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        dragBuffer = Vector2.zero;
        MobileDragDelta = Vector2.zero;
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
        dragBuffer += delta; // 보낸 신호도 버퍼에 누적!
    }
}