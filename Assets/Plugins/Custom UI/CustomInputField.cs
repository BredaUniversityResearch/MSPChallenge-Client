using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

public class CustomInputField : TMP_InputField, IPointerClickHandler
{
    public UnityEvent m_onRightClick;
    public delegate void InteractabilityChangeCallback(bool newState);
    public event InteractabilityChangeCallback interactabilityChangeCallback;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (interactabilityChangeCallback != null)
            interactabilityChangeCallback(state != SelectionState.Disabled);
    }

    public override void OnPointerClick(PointerEventData a_eventData)
    {
        base.OnPointerClick(a_eventData);
        if (a_eventData.button == PointerEventData.InputButton.Right)
        {
            m_onRightClick?.Invoke();
        }
    }
}
