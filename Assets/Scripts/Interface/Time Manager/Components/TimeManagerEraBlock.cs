using UnityEngine;
using UnityEngine.UI;
using System;

public class TimeManagerEraBlock : MonoBehaviour {

    public Image divisionMask;

    public Button highlight;
    public CustomInputField days;
    public CustomInputField hours;
    public CustomInputField minutes;

    private TimeSpan duration;
    public int planningMonthsTotal;
    public int era;

    void Start()
    {
        days.onEndEdit.AddListener((s) =>
        {
            int numDays = 0;
            int.TryParse(s, out numDays);
            //int numDays = string.IsNullOrEmpty(s) ? 0 : Convert.ToInt16(s);
            Duration = new TimeSpan(numDays, Mathf.Clamp(Convert.ToInt16(hours.text), 0, 23), Mathf.Clamp(Convert.ToInt16(minutes.text), 0, 59), 0);
        });        
        hours.onEndEdit.AddListener((s) =>
        {
            int numHours = 0;
            if (int.TryParse(s, out numHours))
            {
                numHours = Mathf.Clamp(numHours, 0, 23);
            }
            //int numHours = string.IsNullOrEmpty(s) ? 0 : Mathf.Clamp(Convert.ToInt16(s), 0, 23);
            Duration = new TimeSpan(Convert.ToInt16(days.text), numHours, Mathf.Clamp(Convert.ToInt16(minutes.text), 0, 59), 0);
        });
        minutes.onEndEdit.AddListener((s) =>
        {

            int numMinutes = 0;
            if (int.TryParse(s, out numMinutes))
            {
                numMinutes = Mathf.Clamp(numMinutes, 0, 59);
            }
            //string.IsNullOrEmpty(s) ? 0 : Mathf.Clamp(Convert.ToInt16(s), 0, 59);
            Duration = new TimeSpan(Convert.ToInt16(days.text), Mathf.Clamp(Convert.ToInt16(hours.text), 0, 23), numMinutes, 0);
        });
    }

    public TimeSpan Duration
    {
        get
        {
            return duration;
        }
        set
        {
            TimeSpan newDuration = value.TotalSeconds < 60 ? TimeSpan.FromSeconds(60) : value;

            if (!days.isFocused && !hours.isFocused && !minutes.isFocused) {
                days.text = newDuration.Days.ToString();
                hours.text = newDuration.Hours.ToString("D2");
                minutes.text = newDuration.Minutes.ToString("D2");
            }
            duration = newDuration;
            if(active)
                TimeManager.instance.EraRealTimeChanged(era, duration);
        }
    }

    public void SetDurationUI(TimeSpan value)
    {
        if (!days.isFocused && !hours.isFocused && !minutes.isFocused) {
            days.text = value.Days.ToString();
            hours.text = value.Hours.ToString("D2");
            minutes.text = value.Minutes.ToString("D2");
            duration = value;
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
            days.interactable = value;
            hours.interactable = value;
            minutes.interactable = value;

            // Flag
            active = value;
            if (!active)
            {
                days.text = "0";
                hours.text = "00";
                minutes.text = "00";
            }
        }
    }
}