using UnityEngine;
using UnityEngine.EventSystems;

public class ClickBlocker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public void OnPointerDown(PointerEventData eventData) { }

    public void OnPointerUp(PointerEventData eventData) { }

    public void OnPointerClick(PointerEventData eventData) { }
}