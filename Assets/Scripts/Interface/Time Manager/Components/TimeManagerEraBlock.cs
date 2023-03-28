using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TimeManagerEraBlock : MonoBehaviour {

		public Image divisionMask;

		public Button highlight;
		public CustomInputField daysText;
		public CustomInputField hoursText;
		public CustomInputField minutesText;

		public int planningMonthsTotal;
		public int era;

		int days, hours, minutes;

		void Start()
		{
			daysText.onEndEdit.AddListener((s) =>
			{
				days = 0;
				int.TryParse(s, out days);
				if (days == 0 && hours == 0 && minutes == 0)
					minutes = 1;
				UpdateDurationText();
			});        
			hoursText.onEndEdit.AddListener((s) =>
			{
				hours = 0;
				if (int.TryParse(s, out hours))
				{
					hours = Mathf.Clamp(hours, 0, 23);
				}
				if (days == 0 && hours == 0 && minutes == 0)
					minutes = 1;
				UpdateDurationText();

			});
			minutesText.onEndEdit.AddListener((s) =>
			{
				if (int.TryParse(s, out minutes))
				{
					minutes = Mathf.Clamp(minutes, 0, 59);
				}
				if (days == 0 && hours == 0 && minutes == 0)
					minutes = 1;
				UpdateDurationText();
			});
		}

		void UpdateDurationText(bool sendUpdate = true)
		{
			daysText.text = days.ToString();
			hoursText.text = hours.ToString("D2");
			minutesText.text = minutes.ToString("D2");
			if (active && sendUpdate)
				TimeManager.Instance.EraRealTimeChanged(era, new TimeSpan(days, hours, minutes, 0));
		}

		public void SetDurationUI(TimeSpan value)
		{
			if (!daysText.isFocused && !hoursText.isFocused && !minutesText.isFocused) {
				days = value.Days;
				hours = value.Hours;
				minutes = value.Minutes;
				UpdateDurationText(false);
			}
		}

		private bool active = true;
		public bool IsActive
		{
			get
			{
				return active;
			}

			set
			{
				// Button
				highlight.interactable = value;

				// Input Fields
				daysText.interactable = value;
				hoursText.interactable = value;
				minutesText.interactable = value;

				// Flag
				active = value;
				if (!active)
				{
					daysText.text = "0";
					hoursText.text = "00";
					minutesText.text = "00";
				}
			}
		}
	}
}