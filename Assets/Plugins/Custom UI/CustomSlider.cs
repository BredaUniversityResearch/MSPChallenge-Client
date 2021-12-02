using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CustomSlider : Slider, IPointerClickHandler, ICustomSlider, IEndDragHandler
{
	public delegate void InteractabilityChangeCallback(bool newState);
	public InteractabilityChangeCallback interactabilityChangeCallback;
	public UnityEvent m_onRightClick;
	public UnityEvent m_onRelease;

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);
		if (interactabilityChangeCallback != null)
			interactabilityChangeCallback(state != SelectionState.Disabled);
	}

	public void OnPointerClick(PointerEventData a_eventData)
	{
		if (a_eventData.button == PointerEventData.InputButton.Right)
		{
			m_onRightClick?.Invoke();
		}
	}

	public bool Interactable => interactable;

	public void AddInteractabilityChangeCallback(InteractabilityChangeCallback callback)
	{
		interactabilityChangeCallback += callback;
	}

	public void RemoveInteractabilityChangeCallback(InteractabilityChangeCallback callback)
	{
		interactabilityChangeCallback -= callback;
	}

	public void OnEndDrag(PointerEventData a_eventData)
	{
		m_onRelease?.Invoke();
	}

	public override void OnPointerUp(PointerEventData a_eventData)
	{
		base.OnPointerUp(a_eventData);
		m_onRelease?.Invoke();
	}
}