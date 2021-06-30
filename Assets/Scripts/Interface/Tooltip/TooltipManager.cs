using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[Serializable]
public class TooltipManager : MonoBehaviour
{

    [SerializeField]
    private Tooltip tooltipPrefab = null;
    private static Tooltip tooltipPrefabStatic;

    private static GameObject tooltipContainer;

    private static float padding = 0;

    private static Tooltip tooltip;
	
	static bool waitingToShow = false;
    private static float timeBeforeShowing = 1.0f;
	static float timeHovered;

    protected void Awake()
    {
        tooltipPrefabStatic = tooltipPrefab;
		
        tooltipContainer = this.gameObject;

        HorizontalLayoutGroup layoutGroup = tooltipPrefab.GetComponent<HorizontalLayoutGroup>();
        padding = layoutGroup.padding.left + layoutGroup.padding.right;

        GameObject newTooltip = Instantiate(tooltipPrefabStatic.gameObject);
        newTooltip.transform.SetParent(tooltipContainer.transform, false);
        tooltip = newTooltip.GetComponent<Tooltip>();
        tooltip.Initialise("", OnShowTooltip);
        tooltip.HideTooltip();
    }

	private void Update()
	{
		if (waitingToShow)
		{
			timeHovered += Time.deltaTime;
			if (timeHovered >= timeBeforeShowing)
			{
				tooltip.ShowToolTip();
				timeHovered = 0f;
				waitingToShow = false;
			}
		}
	}

	public static void ResetAndShowTooltip(string text, float newTimeBeforeShowing)
	{
		HideTooltip();
		timeBeforeShowing = newTimeBeforeShowing;
		if (!string.IsNullOrEmpty(text))
		{
			tooltip.SetText(text);
			waitingToShow = true;
		}
	}

	public static void HideTooltip()
	{
		tooltip.HideTooltip();
		waitingToShow = false;
		timeHovered = 0;
	}
	
    public static void ForceSetToolTip(string text)
    {
        tooltip.SetText(text);
        tooltip.ShowToolTip();
    }

    public static float GetPadding()
    {
        return padding;
    }

	private void OnShowTooltip()
	{
		//Move our transform as the last so we are sure that we render in front of everything.
		transform.SetAsLastSibling();
	}
}
