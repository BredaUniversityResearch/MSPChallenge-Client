using UnityEngine;
using UnityEngine.EventSystems;

public class OnPointerExitClose : MonoBehaviour, IPointerExitHandler {

    public GameObject closeThis;

    public void OnPointerExit(PointerEventData eventData)
    {
        closeThis.SetActive(false);
    }
}
