//using UnityEngine;
//using UnityEngine.EventSystems;

//public class DynamicJoystickPosition : MonoBehaviour, IPointerDownHandler
//{
//    private RectTransform rectTransform;

//    void Awake() => rectTransform = GetComponent<RectTransform>();

//    public void OnPointerDown(PointerEventData eventData)
//    {
//        rectTransform.position = eventData.position;
//    }
//}