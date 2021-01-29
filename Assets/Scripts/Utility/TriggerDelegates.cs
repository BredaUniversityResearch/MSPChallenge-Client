using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class TriggerDelegates : CustomEventTrigger
{
    [HideInInspector]
    public bool consumeDownEvent;
    [HideInInspector]
    public bool consumeUpEvent;
    [HideInInspector]
    public bool consumeEnterEvent;
    [HideInInspector]
    public bool consumeExitEvent;

    public delegate void OnMouseDown();
    public OnMouseDown OnMouseDownDelegate = null;

    public delegate void OnMouseUp();
    public OnMouseUp OnMouseUpDelegate = null;

    public delegate void OnMouseEnter();
    public OnMouseEnter OnMouseEnterDelegate = null;

    public delegate void OnMouseExit();
    public OnMouseExit OnMouseExitDelegate = null;

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!consumeDownEvent) {
            base.OnPointerDown(eventData);
        }

        if (OnMouseDownDelegate != null)
        {
            OnMouseDownDelegate.Invoke();
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!consumeUpEvent) {
            base.OnPointerUp(eventData);
        }

        if (OnMouseUpDelegate != null)
        {         
            OnMouseUpDelegate.Invoke();
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!consumeEnterEvent) {
            base.OnPointerEnter(eventData);
        }

        if (OnMouseEnterDelegate != null)
        {          
            OnMouseEnterDelegate.Invoke();
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!consumeExitEvent) {
            base.OnPointerExit(eventData);
        }

        if (OnMouseExitDelegate != null)
        {  
            OnMouseExitDelegate.Invoke();
        }
    }
}
