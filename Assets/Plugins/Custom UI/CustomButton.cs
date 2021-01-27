using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CustomButton : Button
{
    public delegate void InteractabilityChangeCallback(bool newState);
    public InteractabilityChangeCallback interactabilityChangeCallback;
    public UnityEvent m_onRightClick;

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (interactabilityChangeCallback != null)
            interactabilityChangeCallback(state != SelectionState.Disabled);
    }

	protected override void OnEnable()
	{
		base.OnEnable();
		if (IsInteractable())
		{
			DoStateTransition(SelectionState.Normal, true);
		}
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
