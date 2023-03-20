using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	class MarkedYearTimeline : MonoBehaviour
	{
		[Serializable]
		private class YearSelectionChangedEvent: UnityEvent<int>
		{
		}

		[SerializeField]
		private GameObject markedYearPrefab = null;

		[SerializeField]
		private Slider monthSelectorSlider = null;

		[SerializeField]
		private RectTransform availableSliderRangeFill = null;

		[SerializeField]
		private YearSelectionChangedEvent selectedMonthChangedEvent = null;

		private void Start()
		{
			TimeManager.Instance.OnCurrentMonthChanged += OnCurrentMonthChanged;
			SetupYearMarkers(SessionManager.Instance.MspGlobalData.YearsPerEra);
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
				availableSliderRangeFill.anchorMax = new Vector2((float)newCurrentMonth / (float)SessionManager.Instance.MspGlobalData.session_end_month, availableSliderRangeFill.anchorMax.y);
			}
		}

		private void SetupYearMarkers(int labelSpacingInYears)
		{
			int numYears = SessionManager.Instance.MspGlobalData.session_num_years;
			float desiredSubdivisions = ((float)numYears / labelSpacingInYears);
			int numLabels = Mathf.FloorToInt(desiredSubdivisions) + 1;
			for (int i = 0; i < numLabels; ++i)
			{
				int year = SessionManager.Instance.MspGlobalData.start + (labelSpacingInYears * i);
				CreateYearLabel(year.ToString(), i / desiredSubdivisions);
			}

			if (monthSelectorSlider != null)
			{
				monthSelectorSlider.minValue = 0;
				monthSelectorSlider.maxValue = numYears * 12;
				monthSelectorSlider.wholeNumbers = true;
				monthSelectorSlider.onValueChanged.AddListener(OnSliderValueChanged);
			}

			SetLatestAvailableMonth(TimeManager.Instance.GetCurrentMonth());
		}

		private void CreateYearLabel(string labelText, float xAnchorPos)
		{
			GameObject label = Instantiate(markedYearPrefab, transform);
			TextMeshProUGUI labelTextComponent = label.GetComponentInChildren<TextMeshProUGUI>();
			if (labelTextComponent != null)
			{
				labelTextComponent.text = labelText;
				if(xAnchorPos < 0.01f)
				{
					RectTransform textRect = labelTextComponent.GetComponent<RectTransform>();
					textRect.pivot = new Vector2(0f, 0f);
					textRect.anchoredPosition = new Vector2(-4f, 0f);
					labelTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
				}
				else if(xAnchorPos > 0.99f)
				{
					RectTransform textRect = labelTextComponent.GetComponent<RectTransform>();
					textRect.pivot = new Vector2(1f, 0f);
					textRect.anchoredPosition = new Vector2(4f, 0f);
					labelTextComponent.alignment = TextAlignmentOptions.MidlineRight;
				}
			}
			RectTransform labelRect = label.GetComponent<RectTransform>();
			labelRect.anchorMin = new Vector2(xAnchorPos, labelRect.anchorMin.y);
			labelRect.anchorMax = new Vector2(xAnchorPos, labelRect.anchorMax.y);
		}

		private void OnSliderValueChanged(float newMonthValue)
		{
			selectedMonthChangedEvent.Invoke((int)newMonthValue);
		}
	}
}
