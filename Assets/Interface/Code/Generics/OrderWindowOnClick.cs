using UnityEngine;
using UnityEngine.EventSystems;

public class OrderWindowOnClick : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }

    void OnEnable()
    {
        transform.SetAsLastSibling();
    }
}
