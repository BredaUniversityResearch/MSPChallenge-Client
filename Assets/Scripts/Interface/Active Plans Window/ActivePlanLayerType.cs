using System.Collections;
using System.Collections.Generic;
using ColourPalette;
using Networking;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActivePlanLayerType : MonoBehaviour {

	public Toggle toggle;
	public TextMeshProUGUI typeName;
	public Button infoButton;
	public CustomToggleColorSet textColour;
	public CustomToggleColorSet toggleColour;
	public ColourAsset unavailableTextColour;
	public ColourAsset unavailableToggleColour;

    //Should this toggle be disabled if it's not selected
    private bool disabledIfNotSelected;

	public void SetToType(EntityType type, GenericWindow activePlanWindow, bool addAvailabilityDate = false)
	{
		if(addAvailabilityDate)
			typeName.text = $"{type.Name} ({Util.MonthToText(type.availabilityDate, true)})";
		else
			typeName.text = type.Name;

		if (!string.IsNullOrEmpty(type.media))
		{
			infoButton.onClick.AddListener(() =>
			{
				Vector3[] corners = new Vector3[4];
				activePlanWindow.windowTransform.GetWorldCorners(corners);
				InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(MediaUrl.Parse(type.media), new Vector3(corners[2].x, corners[2].y - Screen.height, 0));
			});
		}
		else
			infoButton.gameObject.SetActive(false);

        toggle.onValueChanged.AddListener(SetInteractabilityForState);
    }

    public bool DisabledIfNotSelected
    {
        set
        {
            disabledIfNotSelected = value;
            if (!toggle.isOn)
                toggle.interactable = !disabledIfNotSelected;
            if (value)
            {
	            textColour?.LockToColor(unavailableTextColour);
	            toggleColour?.LockToColor(unavailableToggleColour);
            }
			else
			{
				textColour ?.UnlockColor();
				toggleColour?.UnlockColor();
			}
        }
    }

    void SetInteractabilityForState(bool value)
    {
        if (!value)
        {
            if (disabledIfNotSelected)
                toggle.interactable = false;
        }
        else if (!toggle.interactable)
            toggle.interactable = true;
    }

	public void SetToMultiple()
	{
		typeName.text  = "Multiple different";
		infoButton.gameObject.SetActive(false);
	}
}
