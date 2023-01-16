using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DistributionSliderLong : MonoBehaviour
	{
		[SerializeField] RectTransform m_oldValueIndicator;
		[SerializeField] RectTransform m_availableRangeFill;
		[SerializeField] LongSlider m_valueSlider;

		public Action m_onChangeCallback;

		bool m_ignoreSliderCallback;  
		private long m_oldValue = 0;
		private long m_maxAvailableValue = 1;//Range on slider actually available
		private long m_maxValue = 1;//Slider min max

		public long Value
		{
			get
			{
				return m_valueSlider.value;
			}
			set
			{
				m_ignoreSliderCallback = true;
				m_valueSlider.value = value;
				m_ignoreSliderCallback = false;
			}
		}

		public long MaxValue
		{
			get
			{
				return m_maxValue;
			}
			set
			{
				m_maxValue = value;
				OnSliderRangeChanged();
			}
		}

		public long MaxAvailableValue
		{
			get
			{
				return m_maxAvailableValue;
			}
			set
			{
				m_maxAvailableValue = value;
				OnSliderRangeChanged();
			}
		}

		void Start()
		{
			m_valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		public bool IsChanged()
		{
			return m_valueSlider.interactable && m_valueSlider.value != m_oldValue;
		}

		public  void SetInteractablity(bool a_value)
		{
			m_valueSlider.interactable = a_value;
		}

		public void SetOldValue(long a_value)
		{
			m_oldValue = a_value;
			UpdateOldValueIndicatorPosition();
		}

		protected void OnSliderValueChanged(long a_newValue)
		{
			if (m_ignoreSliderCallback)
				return;

			if (a_newValue > m_maxAvailableValue)
			{
				Value = m_maxAvailableValue;
			}

			if(m_onChangeCallback != null)
				m_onChangeCallback.Invoke();
		}

		private void UpdateOldValueIndicatorPosition()
		{
			if (m_oldValueIndicator != null)
			{
				float x = (float)((double)m_oldValue / (double)m_maxValue);
				m_oldValueIndicator.anchorMin = new Vector2(x, 0.0f);
				m_oldValueIndicator.anchorMax = new Vector2(x, 1.0f);
			}
		}

		void OnSliderRangeChanged()
		{
			m_availableRangeFill.anchorMax = new Vector2((float)((double)m_maxAvailableValue / (double)m_maxValue), 1.0f);
			UpdateOldValueIndicatorPosition();
		}
	}
}