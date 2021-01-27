using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class TimeManagerEraDivision : MonoBehaviour
{
    public TimeManagerWindow timeManagerWindow;

    public TextMeshProUGUI planningText;
    public TextMeshProUGUI simulationText;
    public Slider slider;
    public Transform notchParent;
    public GameObject notchPrefab;

    //public InputField daysText, hoursText, minutesText;
    int days, hours, minutes;

    //public int Days { get { return days; } set { days = value;  daysText.text = value.ToString(); } }
    //public int Hours { get { return hours; } set { hours = value;  hoursText.text = value.ToString("D2"); } }
    //public int Minutes { get { return minutes; } set { minutes = value;  minutesText.text = value.ToString("D2"); } }
    //public int TotalSeconds { get { return minutes * 60 + hours * 3600 + days * 86400; } }
	
    private bool sliderTriggersUpdate = true;

    void Start()
    {
        // Init
        //slider.value = 3f;

        // Slider
        slider.onValueChanged.AddListener((f) => Slider(f));

        // Era blocks
        //for (int i = 0; i < timeManagerWindow.timeline.eraBlocks.Length; i++) {
        //    int index = i;
        //    slider.onValueChanged.AddListener((f) => {
        //if (timeManagerWindow.timeline.eraBlocks[index].IsActive) {
        //    // Clamp to no be lower than 1 or progress
        //    float floor = Mathf.Max(1, ((timeManagerWindow.timeline.Progress * 4f) % 1f) * slider.maxValue);
        //    f = Mathf.Clamp(f, floor, slider.maxValue);
        //    slider.value = f;

        //    timeManagerWindow.timeline.eraBlocks[index].divisionMask.fillAmount = 1f - (f / slider.maxValue);
        //    timeManagerWindow.timeline.eraBlocks[index].planningMonthsTotal = (int)(f * 12f);
        //}
        //    });
        //}

        // Clamp
        //daysText.onEndEdit.AddListener((s) => { Days = Convert.ToInt16(s); TimeManager.instance.RemainingTimeChanged(TotalSeconds); });
        //hoursText.onEndEdit.AddListener((s) => { Hours = Mathf.Clamp(Convert.ToInt16(s), 0, 23); TimeManager.instance.RemainingTimeChanged(TotalSeconds); });
        //minutesText.onEndEdit.AddListener((s) => {Minutes = Mathf.Clamp(Convert.ToInt16(s), 0, 59); TimeManager.instance.RemainingTimeChanged(TotalSeconds); });

        if (Main.MspGlobalData != null)
        {
            SetYearsPerEra();
        }
        else
        {
            Main.OnGlobalDataLoaded += GlobalDataLoaded;
        }
    }

    void GlobalDataLoaded()
    {
        Main.OnGlobalDataLoaded -= GlobalDataLoaded;
        SetYearsPerEra();
    }

    void SetYearsPerEra()
    {
        int yearsPerEra = Main.MspGlobalData.YearsPerEra;
        slider.maxValue = yearsPerEra;
		for (int i = 0; i < yearsPerEra; i++)
		{
			Instantiate(notchPrefab, notchParent);
		}
	}

    private void Slider(float val)
    {
        int planningNumber = (int)val;
		//Don't ever have 0 years planning please.
		if (planningNumber == 0)
		{
			slider.value = 1.0f;
			return;
		}

		int simulationNumber = Main.MspGlobalData.YearsPerEra - (int)val;

        planningText.text = planningNumber.ToString() + " year planning";
        simulationText.text = simulationNumber.ToString() + " year simulation";

        if(sliderTriggersUpdate)
            TimeManager.instance.EraGameTimeChanged(planningNumber * 12);
    }

    public void SetSliderValue(int value)
    {
        sliderTriggersUpdate = false;
        slider.value = value;
        planningText.text = value.ToString() + " year planning";
        simulationText.text = (Main.MspGlobalData.YearsPerEra - value).ToString() + " year simulation";
        sliderTriggersUpdate = true;
    }

    //public void SetDuration(TimeSpan newDuration)
    //{
    //    if (!daysText.interactable)
    //        return;

    //    Days = newDuration.Days;
    //    Hours = newDuration.Hours;
    //    Minutes = newDuration.Minutes;
    //}

    //public bool DurationInteractable
    //{
    //    get
    //    {
    //        return daysText.interactable;
    //    }
    //    set
    //    {
    //        daysText.interactable = value;
    //        hoursText.interactable = value;
    //        minutesText.interactable = value;
    //        if (!value)
    //        {
    //            daysText.text = "-";
    //            hoursText.text = "- -";
    //            minutesText.text = "- -";
    //        }
    //    }
    //}

    /// <summary>
    /// Expects a value in the [0-1] range that indicates how much of the era is simulation
    /// </summary>
    public void SetEraSimulationDivision(int era, float simulationValue)
    {
        timeManagerWindow.timeline.eraBlocks[era].divisionMask.fillAmount = simulationValue;
    }

    //public bool Selected
    //{
    //    get { return daysText.isFocused || hoursText.isFocused || minutesText.isFocused; }
    //}
}