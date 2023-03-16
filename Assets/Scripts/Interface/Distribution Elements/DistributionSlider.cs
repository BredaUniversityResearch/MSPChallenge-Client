using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DistributionSlider : MonoBehaviour
	{
		[SerializeField] protected RectTransform[] oldValueIndicators = null;
		[SerializeField] protected DistributionItem parent = null;
		[SerializeField] Slider valueSlider;

		private InterpolatedValueMapping sliderValueMapping = new InterpolatedValueMapping();
    
		[HideInInspector] public bool ignoreSliderCallback;
		float oldValue = 0.0f;
		float oldRemappedNormalizedValue = 0.0f;	//The old unchanged value remapped to be in 'slider space'
		Vector2 availableRange = new Vector2(float.MinValue, float.MaxValue);
		float minValue = 0.0f;
		float maxValue = 1.0f;

		public float Value
		{
			get
			{
				return sliderValueMapping.Map(GetNormalizedSliderValue());
			}
			set
			{
				valueSlider.normalizedValue = sliderValueMapping.InverseMap(value, true);
			}
		}

		public float GetNormalizedSliderValue()
		{
			return valueSlider.normalizedValue;
		}

		public float MinValue
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

		public float MaxValue
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

		public Vector2 AvailableRange
		{
			get { return availableRange; }
			set
			{
				availableRange = value;
			}
		}

		public bool IsChanged()
		{
			return valueSlider.interactable && valueSlider.normalizedValue != oldRemappedNormalizedValue;
		}

		public void SetInteractablity(bool value)
		{
			valueSlider.interactable = value;
		}

		public void SetOldValue(float value)
		{
			oldValue = value;
			UpdateOldValueIndicatorPosition();
		}

		void Start()
		{
			valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		void OnSliderValueChanged(float newValue)
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
			foreach (RectTransform rect in oldValueIndicators)
			{
				rect.anchorMin = new Vector2(oldRemappedNormalizedValue, 0.0f);
				rect.anchorMax = new Vector2(oldRemappedNormalizedValue, 1.0f);
			}
		}

		protected void OnSliderRangeChanged()
		{
			sliderValueMapping.Clear();
			sliderValueMapping.Add(0.0f, minValue);
			sliderValueMapping.Add(1.0f, maxValue);

			UpdateOldValueIndicatorPosition();
		}
	}
}