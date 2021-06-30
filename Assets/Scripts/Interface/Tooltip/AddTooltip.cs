using UnityEngine;
using System.Collections;

/// <summary>
/// AddTooltip
/// </summary>
public class AddTooltip : MonoBehaviour 
{
    [SerializeField, TextArea]
    public string text;
	public float timeBeforeShowing = 0.5f;
	[SerializeField]
	bool passClickToParent = true;

    protected void Start () 
	{
        //TooltipManager.CreateToolTipForUI(this.gameObject, text, timeBeforeShowing);

		TriggerDelegates tooltipTrigger = gameObject.AddComponent<TriggerDelegates>();
		tooltipTrigger.consumeDownEvent = !passClickToParent;

		tooltipTrigger.OnMouseEnterDelegate += () =>
		{
			TooltipManager.ResetAndShowTooltip(text, timeBeforeShowing);
		};

		tooltipTrigger.OnMouseExitDelegate += () =>
		{
			TooltipManager.HideTooltip();
		};

		tooltipTrigger.OnMouseDownDelegate += () =>
		{
			TooltipManager.HideTooltip();
		};
	}
}
