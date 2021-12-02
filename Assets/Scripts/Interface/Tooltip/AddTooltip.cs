using UnityEngine;
using System.Collections;
using JetBrains.Annotations;

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

	// note MH: dropdown specific tooltip hack to make sure the tooltip is rendered on top of the drop down
	[CanBeNull]
	private static Tooltip dropdownTooltip = null;

    protected void Start () 
	{
        //TooltipManager.CreateToolTipForUI(this.gameObject, text, timeBeforeShowing);

		TriggerDelegates tooltipTrigger = gameObject.AddComponent<TriggerDelegates>();
		tooltipTrigger.consumeDownEvent = !passClickToParent;
		tooltipTrigger.consumeUpEvent = !passClickToParent;

		tooltipTrigger.OnMouseEnterDelegate += () =>
		{
			// BEGIN note MH: dropdown specific hack to make sure the tooltip is rendered on top of the drop down
			CustomDropdown dropdown = gameObject.GetComponentInChildren<CustomDropdown>();
			if (dropdown != null)
			{
				dropdownTooltip ??= createDropdownTooltip(dropdown);
				if (dropdownTooltip != null)
				{
					dropdownTooltip.ShowToolTip();
					return;
				}
			}
			dropdownTooltip = null;
			// END
			
			TooltipManager.ResetAndShowTooltip(text, timeBeforeShowing);
		};

		tooltipTrigger.OnMouseExitDelegate += () =>
		{
			// BEGIN note MH: dropdown specific hack to make sure the tooltip is rendered on top of the drop down
			if (dropdownTooltip != null)
			{
				dropdownTooltip.HideTooltip();
				return;
			}
			// END
			
			TooltipManager.HideTooltip();
		};

		tooltipTrigger.OnMouseDownDelegate += () =>
		{
			// BEGIN note MH: dropdown specific hack to make sure the tooltip is rendered on top of the drop down
			if (dropdownTooltip != null)
			{
				dropdownTooltip.HideTooltip();
				return;
			}
			// END

			TooltipManager.HideTooltip();
		};
	}

    [CanBeNull]
    private Tooltip createDropdownTooltip(CustomDropdown dropdown)
    {
		Transform dropdownListTransform = dropdown.gameObject.transform.Find("Dropdown List");
		if (dropdownListTransform == null)
		{
			return null;
		}

		Tooltip tooltip = Instantiate(TooltipManager.tooltipPrefabStatic, dropdownListTransform);
		RectTransform rectTransform = dropdownListTransform.gameObject.GetComponent<RectTransform>();
        float scale = tooltip.GetComponentInParent<Canvas>().scaleFactor;
        tooltip.Initialise(
	        text, null,
	        new Vector2(
		        -dropdownListTransform.position.x,
		        -dropdownListTransform.position.y
		        ) - rectTransform.offsetMin * scale
	        );
		tooltip.ShowToolTip();
	    tooltip.gameObject.transform.SetAsLastSibling();

	    return tooltip;
    }
}
