using UnityEngine;
using UnityEngine.EventSystems;

public class MobileZoomButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("줌 방향 (줌인 = 1, 줌아웃 = -1)")]
    public float zoomDirection = 1f;

    // 전 세계(모든 스크립트)에서 공유하는 static 변수!
    public static float currentZoomInput = 0f;

    // 화면(버튼)에 손가락이 닿았을 때
    public void OnPointerDown(PointerEventData eventData)
    {
        currentZoomInput = zoomDirection;
    }

    // 화면(버튼)에서 손가락이 떨어졌을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        // 손을 떼면 줌 입력 정지
        if (currentZoomInput == zoomDirection)
        {
            currentZoomInput = 0f;
        }
    }
}