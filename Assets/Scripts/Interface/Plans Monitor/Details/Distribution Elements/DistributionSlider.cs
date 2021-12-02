using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DistributionSlider : AbstractDistributionSlider/*, IPointerDownHandler, IDragHandler*/
{
	private InterpolatedValueMapping sliderValueMapping = new InterpolatedValueMapping();
    public Slider valueSlider;
    
	private float oldValue = 0.0f;
	private float oldRemappedNormalizedValue = 0.0f;	//The old unchanged value remapped to be in 'slider space'
	public override float Value
	{
		get
		{
			return sliderValueMapping.Map(GetNormalizedSliderValue());
		}
		set
		{
			valueSlider.normalizedValue = sliderValueMapping.InverseMap(value, true);
			//OnSliderValueChanged(value, false);
		}
	}

    public override float GetNormalizedSliderValue()
    {
        return valueSlider.normalizedValue;
    }

    private float minValue = 0.0f;
	public override float MinValue
	{
		get
		{
			return minValue;
		}
		set
		{
			minValue = value;
			OnSliderRangeChanged();
		}
	}

	private float maxValue = 1.0f;
	public override float MaxValue
	{
		get
		{
			return maxValue;
		}
		set
		{
			maxValue = value;
			OnSliderRangeChanged();
		}
	}

    private Vector2 availableRange = new Vector2(float.MinValue, float.MaxValue);
    public override Vector2 AvailableRange
    {
        get { return availableRange; }
        set
        {
            availableRange = value;
        }
    }

    public override bool IsChanged()
	{
		return valueSlider.interactable && valueSlider.normalizedValue != oldRemappedNormalizedValue;
	}

	public override  void SetInteractablity(bool value)
	{
		valueSlider.interactable = value;
	}

	public override void SetOldValue(float value)
	{
		oldValue = value;
		UpdateOldValueIndicatorPosition();
	}

	void Start()
	{
		valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
	}

	//public void OnPointerDown(PointerEventData eventData)
	//{
	//	if (valueSlider.interactable)
	//	{
	//		RectTransform rt = valueSlider.GetComponent<RectTransform>();
	//		float newVal = Value;
	//		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, null, out Vector2 result))
	//		{
	//			newVal = sliderValueMapping.Map(result.x / rt.sizeDelta.x);
	//		}
	//		OnSliderValueChanged(newVal, true);
	//	}
	//}

	////We're using these onDrag and OnPointerDown handlers here instead of the OnValueChanged callback because the distribution groups will change the values as well which caused stack overflows.
	//public void OnDrag(PointerEventData eventData)
	//{
	//	if (valueSlider.interactable)
	//	{
	//		RectTransform rt = valueSlider.GetComponent<RectTransform>();
	//		float newVal = Value;
	//		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, null, out Vector2 result))
	//		{
	//			newVal = sliderValueMapping.Map(result.x / rt.sizeDelta.x);
	//		}
	//		OnSliderValueChanged(newVal, true);
	//	}
	//}

	protected void OnSliderValueChanged(float newValue/*, bool fromUserInteraction*/)
	{
		if (valueSlider.interactable)
		{
			// Snap to current value
			if (!ignoreSliderCallback)
			{
				ignoreSliderCallback = true;
				if (Mathf.Abs(valueSlider.normalizedValue - oldRemappedNormalizedValue) < DistributionItem.SNAP_RANGE)
				{
					if (oldRemappedNormalizedValue - DistributionItem.SNAP_RANGE < 0.0f)
					{
						//Near zero, only snap when  the value is over the snap value.
						if (valueSlider.normalizedValue > oldRemappedNormalizedValue)
						{
							valueSlider.normalizedValue = oldRemappedNormalizedValue;
						}
					}
					else if (oldRemappedNormalizedValue + DistributionItem.SNAP_RANGE > 1.0f)
					{
						//Near one, only snap when the value is below the old value.
						if (valueSlider.normalizedValue < oldRemappedNormalizedValue)
						{
							valueSlider.normalizedValue = oldRemappedNormalizedValue;
						}
					}
					else
					{
						valueSlider.normalizedValue = oldRemappedNormalizedValue;
					}
				}

				if (newValue < AvailableRange.x || newValue > AvailableRange.y)
				{
					valueSlider.normalizedValue = sliderValueMapping.InverseMap(Mathf.Clamp(newValue, AvailableRange.x, AvailableRange.y));
				}

				parent.UpdateNewValueSlider();
				ignoreSliderCallback = false;
			}
		}
	}

	private void UpdateOldValueIndicatorPosition()
	{
		oldRemappedNormalizedValue = sliderValueMapping.InverseMap(oldValue);
		oldValueIndicator.anchorMin = new Vector2(oldRemappedNormalizedValue, 0.0f);
		oldValueIndicator.anchorMax = new Vector2(oldRemappedNormalizedValue, 1.0f);
	}

	protected void OnSliderRangeChanged()
	{
		sliderValueMapping.Clear();
		sliderValueMapping.Add(0.0f, minValue);
		sliderValueMapping.Add(1.0f, maxValue);

		UpdateOldValueIndicatorPosition();
	}

    public override void SetOldValue(long value)
    {
        throw new System.NotImplementedException();
    }

    public override void SetAvailableRangeLong(long min, long max)
    {
        throw new System.NotImplementedException();
    }

    public override void SetAvailableMaximumLong(long max)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateNewValueFill()
    {
        throw new System.NotImplementedException();
    }
}