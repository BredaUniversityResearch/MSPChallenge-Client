using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

class MarkedYearTimeline : MonoBehaviour
{
	[Serializable]
	private class YearSelectionChangedEvent: UnityEvent<int>
	{
	}

	[SerializeField]
	private GameObject markedYearPrefab = null;

	[SerializeField]
	private int yearSpacing = 5;

	[SerializeField]
	private Slider monthSelectorSlider = null;

	[SerializeField]
	private RectTransform availableSliderRangeFill = null;

	[SerializeField]
	private YearSelectionChangedEvent selectedMonthChangedEvent = null;

	private void Start()
	{
		GameState.OnCurrentMonthChanged += OnCurrentMonthChanged;

		if (Main.MspGlobalData != null)
		{
			SetupYearMarkers(yearSpacing);
		}
		else
		{
			Main.OnGlobalDataLoaded += OnMspGlobalDataLoaded;
		}
	}

	private void OnDestroy()
	{
		GameState.OnCurrentMonthChanged -= OnCurrentMonthChanged;
	}

	//Callback set in Unity Editor.
	public void OnDisplayMonthChanged(int newDisplayMonth)
	{
		int displayYear = Mathf.FloorToInt((float)newDisplayMonth);
		if (monthSelectorSlider.value != displayYear)
		{
			monthSelectorSlider.value = displayYear;
		}
	}

	private void OnCurrentMonthChanged(int oldCurrentMonth, int newCurrentMonth)
	{
		if (monthSelectorSlider.value == oldCurrentMonth)
		{
			monthSelectorSlider.value = newCurrentMonth;
		}

		SetLatestAvailableMonth(newCurrentMonth);
	}

	private void SetLatestAvailableMonth(int newCurrentMonth)
	{
		if (availableSliderRangeFill != null)
		{
			availableSliderRangeFill.anchorMax = new Vector2((float)newCurrentMonth / (float)Main.MspGlobalData.session_end_month, availableSliderRangeFill.anchorMax.y);
		}
	}

	private void OnMspGlobalDataLoaded()
	{
		Main.OnGlobalDataLoaded -= OnMspGlobalDataLoaded;
		SetupYearMarkers(yearSpacing);
	}

	private void SetupYearMarkers(int labelSpacingInYears)
	{
		int numYears = Main.MspGlobalData.session_num_years;
		float desiredSubdivisions = ((float)numYears / labelSpacingInYears);
		int numLabels = Mathf.FloorToInt(desiredSubdivisions) + 1;
		for (int i = 0; i < numLabels; ++i)
		{
			int year = Main.MspGlobalData.start + (labelSpacingInYears * i);
			RectTransform label = CreateYearLabel(year.ToString());

			float percentage = (float)i / (desiredSubdivisions);
			label.anchorMin = new Vector2(percentage, label.anchorMin.y);
			label.anchorMax = new Vector2(percentage, label.anchorMax.y);

            //Offset the first and last labels
			if (i == 0)
			{
				label.localPosition = new Vector3(label.sizeDelta.x / 2f, label.localPosition.y);
			}
			else if (i == numLabels - 1 && (label.anchorMax.x > 0.98f))
			{
				//Only offset the last label if the label would show at roughly the end point.
				label.localPosition = new Vector3(-label.sizeDelta.x / 2f, label.localPosition.y);
			}
		}

		if (monthSelectorSlider != null)
		{
			monthSelectorSlider.minValue = 0;
			monthSelectorSlider.maxValue = numYears * 12;
			monthSelectorSlider.wholeNumbers = true;
			monthSelectorSlider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		SetLatestAvailableMonth(GameState.GetCurrentMonth());
	}

	private RectTransform CreateYearLabel(string labelText)
	{
		GameObject label = Instantiate(markedYearPrefab, transform);
		Text labelTextComponent = label.GetComponent<Text>();
		if (labelTextComponent != null)
		{
			labelTextComponent.text = labelText;
		}

		return label.GetComponent<RectTransform>();
	}

	private void OnSliderValueChanged(float newMonthValue)
	{
		selectedMonthChangedEvent.Invoke((int)newMonthValue);
	}
}
