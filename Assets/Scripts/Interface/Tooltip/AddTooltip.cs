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
	private static Tooltip dropDownTooltip = null;

    protected void Start () 
	{
        //TooltipManager.CreateToolTipForUI(this.gameObject, text, timeBeforeShowing);

		TriggerDelegates tooltipTrigger = gameObject.AddComponent<TriggerDelegates>();
		tooltipTrigger.consumeDownEvent = !passClickToParent;
		tooltipTrigger.consumeUpEvent = !passClickToParent;

		tooltipTrigger.OnMouseEnterDelegate += () =>
		{
			// note MH: dropdown specific hack to make sure the tooltip is rendered on top of the drop down
			var dropdown = gameObject.GetComponentInChildren<CustomDropdown>();
			if (dropdown != null)
			{
				dropDownTooltip = createDropdownTooltip(dropdown);
				return;
			}

			TooltipManager.ResetAndShowTooltip(text, timeBeforeShowing);
		};

		tooltipTrigger.OnMouseExitDelegate += () =>
		{
			if (dropDownTooltip != null)
			{
				dropDownTooltip.HideTooltip();
				return;
			}
			TooltipManager.HideTooltip();
		};

		tooltipTrigger.OnMouseDownDelegate += () =>
		{
			if (dropDownTooltip != null)
			{
				dropDownTooltip.HideTooltip();
				return;
			}			
			TooltipManager.HideTooltip();
		};
	}

    [CanBeNull]
    private Tooltip createDropdownTooltip(CustomDropdown dropdown)
    {
		var dropdownListTransform = dropdown.gameObject.transform.Find("Dropdown List");
		if (dropdownListTransform == null)
		{
			return null;
		}	    

        var newTooltip = Instantiate(TooltipManager.tooltipPrefabStatic, dropdownListTransform);
        var tooltip = newTooltip.GetComponent<Tooltip>();
        
		var dropdownPoint = Camera.main.WorldToViewportPoint(dropdownListTransform.transform.position);
		float padding = TooltipManager.GetPadding();
		tooltip.SetText(text);
		float scale = tooltip.GetComponentInParent<Canvas>().scaleFactor;
		float tooltipWidth = (tooltip.tooltipText.preferredWidth + padding) * scale;
		float tooltipHeight = (tooltip.tooltipText.preferredHeight + padding) * scale;
		var offsetX = dropdownPoint.normalized.x * Screen.width - tooltipWidth * 0.5f;
		var offsetY = tooltipHeight * 0.5f;
        
        tooltip.Initialise(text, null, new Vector2(offsetX, offsetY));
	    tooltip.ShowToolTip();
	    tooltip.gameObject.transform.SetAsLastSibling();

	    return tooltip;
    }
}
