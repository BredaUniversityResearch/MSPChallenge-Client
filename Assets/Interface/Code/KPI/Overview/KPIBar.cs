using System;
using System.Globalization;
using KPI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KPIBar : MonoBehaviour
{
	[Header("References")]
	public TextMeshProUGUI title;
	public Image teamColour;
	public CustomToggle barToggle;
	public Toggle graphToggle;
	public GameObject childContainer;
    public GameObject otherObjectEnabledOnExpand;
	public RectTransform foldIcon;
	public bool isParent;
	public bool isExpanded = false;
	public string unit;
	public string ValueName
	{
		get;
		set;
	}

	public bool usesKPIValueColor = true;

	[Header("Values")]
	[SerializeField]
	private KPIValueDisplay start = null;
	[SerializeField]
	private KPIValueDisplay actual = null;

	[SerializeField]
	private ValueConversionCollection valueConversionCollection = null;
	//public Text yearly;

	private float startingValue = 0.0f;
	public float CurrentValue
	{
		get;
		private set;
	}

	private Color barCol;
	private ColorBlock toggleColBlock;

	private Action<bool> onBarExpandedStateToggled;

	void Awake()
	{
        if(isParent)
            barToggle.onValueChanged.AddListener(SetExpandedInternal);
        else if(graphToggle.gameObject.activeSelf)
        {
            barToggle.onValueChanged.AddListener((b) => { graphToggle.isOn = !graphToggle.isOn;});
        }
    }

	/// <summary>
	/// Sets the text of the first bar value
	/// </summary>
	public void SetActual(float val, int countryId)
	{
		ConvertedUnit convertedValue = GetConvertedValue(val);

		actual.UpdateValue(convertedValue);
		CurrentValue = val;
		//lastActual = currentValue;

		string changePercentage = KPIValue.FormatRelativePercentage(startingValue, val);
        actual.UpdateTooltip(changePercentage);
		if (countryId != -1 && teamColour != null)
		{
			teamColour.gameObject.SetActive(true);
			if (countryId == 0)
				teamColour.color = Color.white;
			else
				teamColour.color = TeamManager.GetTeamByTeamID(countryId).color;
		}
	}

	private ConvertedUnit GetConvertedValue(float value)
	{
		ConvertedUnit convertedValue;
		if (valueConversionCollection != null)
		{
			convertedValue = valueConversionCollection.ConvertUnit(value, unit);
		}
		else
		{
			convertedValue = new ConvertedUnit(value, unit, 0);
		}

		return convertedValue;
	}

	public void SetStartValue(float value)
	{
		ConvertedUnit convertedValue = GetConvertedValue(value);
		start.UpdateValue(convertedValue);
		startingValue = value;
	}

    private void SetExpandedInternal(bool state)
    {
        isExpanded = state;
        childContainer.SetActive(isExpanded);
        if (otherObjectEnabledOnExpand != null)
        {
            otherObjectEnabledOnExpand.SetActive(isExpanded);
            otherObjectEnabledOnExpand.transform.SetAsFirstSibling();
        }

        Vector3 rot = foldIcon.eulerAngles;
        foldIcon.eulerAngles = isExpanded ? new Vector3(rot.x, rot.y, 0f) : new Vector3(rot.x, rot.y, 90f);

        if (onBarExpandedStateToggled != null)
        {
            onBarExpandedStateToggled(isExpanded);
        }
    }

	public void SetExpandedState(bool state)
    {
        if(isParent)
            barToggle.isOn = state;
    }

	public void SetBarExpandedStateChangedCallback(Action<bool> callback)
	{
		onBarExpandedStateToggled = callback;
	}

	public void SetDisplayedGraphColor(Color graphColor)
	{
		title.color = graphColor;
		graphToggle.graphic.color = graphColor;
	}
}