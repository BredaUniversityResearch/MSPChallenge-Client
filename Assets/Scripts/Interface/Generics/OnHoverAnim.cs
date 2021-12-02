using UnityEngine;
using UnityEngine.EventSystems;

public class OnHoverAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Set the bool of an animator to true/false on pointer enter/exit")]
    public Animator anim;
    public string boolName;

    public void OnPointerEnter(PointerEventData eventData)
    {
        anim.SetBool(boolName, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        anim.SetBool(boolName, false);
    }
}