using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform bg, handle;
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }

    public void OnDrag(PointerEventData data)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bg, data.position, data.pressEventCamera, out pos))
        {
            pos.x = (pos.x / bg.sizeDelta.x); 
            pos.y = (pos.y / bg.sizeDelta.y);
            
            Horizontal = pos.x * 2 - 1; 
            Vertical = pos.y * 2 - 1;
            
            handle.anchoredPosition = new Vector2(Horizontal * (bg.sizeDelta.x/3), Vertical * (bg.sizeDelta.y/3));
        }
    }

    public void OnPointerDown(PointerEventData data) => OnDrag(data);
    public void OnPointerUp(PointerEventData data) { Horizontal = 0; Vertical = 0; handle.anchoredPosition = Vector2.zero; }
}
