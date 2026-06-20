//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.EventSystems;

//// IPointerDownHandler(누를 때), IDragHandler(드래그할 때), IPointerUpHandler(뗄 때)
//public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
//{
//    [Header("Joystick UI Connect")]
//    public RectTransform joystickBase;
//    public RectTransform joystickHandle;
//    public CanvasGroup joystickGroup;

//    // 외부(플레이어 이동 스크립트)에서 가져다 쓸 방향 벡터
//    private Vector2 inputVector;
//    public Vector2 InputVector => inputVector;

//    private void Start()
//    {
//        if (joystickGroup != null) joystickGroup.alpha = 0; // 조이스틱 UI 숨김
//    }

//    // 화면을 터치했을 때
//    public void OnPointerDown(PointerEventData eventData)
//    {
//        if (joystickGroup != null) joystickGroup.alpha = 1; // 조이스틱 UI 보이기

//        joystickBase.position = eventData.position; // 터치한 위치에 조이스틱 베이스 이동

//        OnDrag(eventData); // 터치하자마자 드래그 이벤트도 처리
//    }

//    // 터치한 상태로 움직일 때
//    public void OnDrag(PointerEventData eventData)
//    {
//        Vector2 position;

//        // 터치 위치를 조이스틱 베이스의 로컬 좌표로 변환
//        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBase, eventData.position, eventData.pressEventCamera, out position))
//        {
//            Vector2 radius = joystickBase.sizeDelta / 2; // 조이스틱 베이스의 반지름

//            // -1에서 1 사이의 값으로 정규화된 입력 벡터 계산
//            inputVector = new Vector2(position.x / radius.x, position.y / radius.y);

//            // 손가락이 큰 동그라미를 벗어나면 입력 벡터를 정규화하여 최대 크기를 1로 유지
//            if (inputVector.magnitude > 1.0f)
//            {
//                inputVector = inputVector.normalized; // 벡터의 크기가 1보다 크면 정규화
//            }

//            // 조이스틱 핸들을 입력 벡터에 따라 이동
//            joystickHandle.anchoredPosition = new Vector2(inputVector.x * radius.x, inputVector.y * radius.y);
//        }
//    }

//    // 터치에서 손을 뗐을 때
//    public void OnPointerUp(PointerEventData eventData)
//    {
//        inputVector = Vector2.zero; // 입력 벡터 초기화
//        joystickHandle.anchoredPosition = Vector2.zero; // 조이스틱 핸들 중앙으로 이동
//        if (joystickGroup != null) joystickGroup.alpha = 0; // 조이스틱 UI 숨김
//    }
//}
