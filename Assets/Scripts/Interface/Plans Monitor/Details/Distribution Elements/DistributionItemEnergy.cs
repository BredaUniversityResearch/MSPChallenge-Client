using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class DistributionItemEnergy : DistributionItem
{
	[Header("Energy Specific")]
	public TextMeshProUGUI productionText; 
	public CustomInputField sendText; 
	public CustomInputField receiveText;
	string preEditSendText, preEditReceiveText;
    long itemSocketMaximum;

	[SerializeField]
	private ValueConversionCollection valueConversionCollection = null;

	private void Awake()
	{
		Initialise();
	}

	private void Start()
	{
		sendText.onEndEdit.AddListener(OnSendInputChange);
		receiveText.onEndEdit.AddListener(OnReceiveInputChange);
	}

	public override void UpdateNewValueSlider()
	{
        MarkAsChanged(slider.IsChanged());
        group.UpdateEntireDistribution();
	}

	public override void ToText(float val, float valueAtOne, bool onlyScaleDisplayValue = false)
	{
		if (val >= 0)
		{
			preEditSendText = "";
			preEditReceiveText = valueConversionCollection.ConvertUnit(val * valueAtOne, ValueConversionCollection.UNIT_WATT).FormatAsString();
		}
		else
		{
			preEditReceiveText = "";
			preEditSendText = "+ " +  valueConversionCollection.ConvertUnit(Mathf.Abs(val) * valueAtOne, ValueConversionCollection.UNIT_WATT).FormatAsString();
		}
		sendText.text = preEditSendText;
		receiveText.text = preEditReceiveText;
	}

	private void OnSendInputChange(string newValueText)
	{
		float value;
		valueConversionCollection.ParseUnit(newValueText, ValueConversionCollection.UNIT_WATT, out value);
		slider.ValueLong = -(long)value;
		UpdateNewValueSlider();
	}

	private void OnReceiveInputChange(string newValueText)
	{
		float value;
		valueConversionCollection.ParseUnit(newValueText, ValueConversionCollection.UNIT_WATT, out value);
		slider.ValueLong = (long)value;
		UpdateNewValueSlider();
	}

    public override void SetSliderInteractability(bool value)
    {
        base.SetSliderInteractability(value);
        sendText.interactable = value;
        receiveText.interactable = value;
    }

    public void SetItemSocketMaximum(long value)
    {
        itemSocketMaximum = value;
    }

    public void SetValue(long currentValue)
    {
        slider.ignoreSliderCallback = true;
        slider.ValueLong = currentValue;
        MarkAsChanged(slider.IsChanged());
        slider.UpdateNewValueFill();
        slider.ignoreSliderCallback = false;
    }

    public void SetOldValue(long oldValue)
    {
        slider.SetOldValue(oldValue);
    }

    public void SetMaximum(long maxValue)
    {
        slider.MaxSliderValueLong = maxValue;
    }

    public void SetAvailableRange(long min, long max)
    {
        slider.SetAvailableRangeLong(min, Math.Min(itemSocketMaximum, max));
    }

    public void SetAvailableMaximum(long max)
    {
        slider.SetAvailableMaximumLong(Math.Min(itemSocketMaximum, max));
    }
}

