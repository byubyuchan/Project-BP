using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class DynamicFloatingJoystick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("UI References")]
    public RectTransform backgroundRing;
    public RectTransform handle;

    [Header("Settings")]
    public float joystickRadius = 100f; // 조이스틱이 움직일 수 있는 최대 반경

    [InputControl(layout = "Vector2")]
    [SerializeField] private string m_ControlPath;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    private CanvasGroup canvasGroup;

    private void Start()
    {
        // 배경에 CanvasGroup 컴포넌트가 없으면 추가, 시작할 때 투명하게 설정
        canvasGroup = backgroundRing.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = backgroundRing.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f; // 조이스틱 UI 숨김
        canvasGroup.blocksRaycasts = false; // 큰 원이 터치 이벤트를 받지 않도록 설정
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        backgroundRing.position = eventData.position; // 터치한 위치에 조이스틱 배경 이동
        handle.anchoredPosition = Vector2.zero; // 핸들을 중앙으로 초기화

        canvasGroup.alpha = 1f; // 조이스틱 UI 보이기

        OnDrag(eventData); // 터치하자마자 드래그 이벤트도 처리
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        // 터치 위치를 조이스틱 배경의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRing, eventData.position, eventData.pressEventCamera, out position);

        if (position.magnitude > joystickRadius)
        {
            position = position.normalized * joystickRadius; // 최대 반경을 넘지 않도록 제한
        }

        handle.anchoredPosition = position;

        Vector2 inputVector = position / joystickRadius; // -1에서 1 사이의 값으로 정규화된 입력 벡터 계산
        SendValueToControl(inputVector); // 입력 벡터를 OnScreenControl에 전달
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero; // 핸들을 중앙으로 초기화
        canvasGroup.alpha = 0f; // 조이스틱 UI 숨김

        SendValueToControl(Vector2.zero); // 입력 벡터 초기화
    }
}
