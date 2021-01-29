using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

public class DistributionSliderCentered : AbstractDistributionSlider, IPointerDownHandler, IDragHandler
{
	public RectTransform newValueFill;
	public RectTransform oldValueFill;
	public RectTransform range;

    [SerializeField]
    protected LongSlider valueSlider = null;

    private long oldValue = 0;
    public override long ValueLong
    {
        get
        {
            return valueSlider.value;
        }
        set
        {
            valueSlider.value = value;
        }
    }

    public override float GetNormalizedSliderValue()
    {
        return valueSlider.normalizedValue;
    }
    
    public override long MaxSliderValueLong
    {
        get
        {
            return valueSlider.maxValue;
        }
        set
        {
            valueSlider.minValue = -value;
            valueSlider.maxValue = value;
        }
    }

    private long minAvailableValue = 0;
    private long maxAvailableValue = 1;
    public override void SetAvailableRangeLong(long min, long max)
    {
        minAvailableValue = Math.Max(-MaxSliderValueLong, min);
        maxAvailableValue = Math.Min(MaxSliderValueLong, max);
        OnSliderAvailableRangeChanged();
    }

    void Start()
    {
        valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    public override void SetAvailableMaximumLong(long max)
    {
        maxAvailableValue = Math.Min(MaxSliderValueLong, max);
        OnSliderAvailableRangeChanged();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //if (valueSlider.interactable)
        //{
        //    StartCoroutine(UpdateValueEndOfFrame());
        //}
    }

    //We're using these onDrag and OnPointerDown handlers here instead of the OnValueChanged callback because the distribution groups will change the values as well which caused stack overflows.
    public void OnDrag(PointerEventData eventData)
    {
        //if (valueSlider.interactable)
        //{
        //    StartCoroutine(UpdateValueEndOfFrame());
        //}
    }

    //public IEnumerator UpdateValueEndOfFrame()
    //{
    //    yield return new WaitForEndOfFrame();
    //    OnSliderValueChanged(ValueLong, true);
    //}
    
    public override bool IsChanged()
    {
        return valueSlider.interactable && valueSlider.value != oldValue;
    }

    public override void SetInteractablity(bool value)
    {
        valueSlider.interactable = value;
    }

    private void UpdateOldValueIndicatorPosition()
    {
        float normalizedPosition = GetNormalizedPosition(oldValue);
        oldValueIndicator.anchorMin = new Vector2(normalizedPosition, 0.0f);
        oldValueIndicator.anchorMax = new Vector2(normalizedPosition, 1.0f);
    }

    public override void SetOldValue(long value)
    {
        oldValue = value;
        UpdateOldValueIndicatorPosition();
    }

    protected void OnSliderValueChanged(long newValue/*, bool fromUserInteraction*/)
    {
        if (ignoreSliderCallback)
            return;

        if (newValue < minAvailableValue)
        {
            ValueLong = minAvailableValue;
            return;
        }
        else if (newValue > maxAvailableValue)
        {
            ValueLong = maxAvailableValue;
            return;
        }

        UpdateNewValueFill();
        parent.UpdateNewValueSlider();
    }

    protected void OnSliderAvailableRangeChanged()
    {
        range.anchorMin = new Vector2(Mathf.Min(0.5f, GetNormalizedPosition(minAvailableValue)), 0.0f);
        range.anchorMax = new Vector2(Mathf.Max(0.5f, GetNormalizedPosition(maxAvailableValue)), 1.0f);
        ignoreSliderCallback = true;
        ValueLong = Math.Min(maxAvailableValue, ValueLong);
        ignoreSliderCallback = false;
    }

    float GetNormalizedPosition(long valueToNormalize)
    {
        return maxAvailableValue - minAvailableValue == 0 ? 0.5f : (float)(valueToNormalize + MaxSliderValueLong) / (float)(MaxSliderValueLong * 2);
    }

    public override void SetOldValue(float value)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateNewValueFill()
    {
        float normalizedValue = GetNormalizedSliderValue();
        if (normalizedValue > 0.5f)
        {
            newValueFill.anchorMin = new Vector2(0.5f, 0f);
            newValueFill.anchorMax = new Vector2(normalizedValue, 1f);
        }
        else
        {
            newValueFill.anchorMin = new Vector2(normalizedValue, 0f);
            newValueFill.anchorMax = new Vector2(0.5f, 1f);
        }


        newValueFill.anchoredPosition = new Vector2(0f, 0f);
        newValueFill.sizeDelta = new Vector2(0f, 0f);
    }
}