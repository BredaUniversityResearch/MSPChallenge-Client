using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class KPIBar : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] TextMeshProUGUI m_title;
		[SerializeField] Toggle m_graphToggle;
		[SerializeField] Image m_graphColourDisplay;

		[Header("Values")]
		[SerializeField] KPIValueDisplay m_start = null;
		[SerializeField] KPIValueDisplay m_actual = null; 
		[SerializeField] ValueConversionCollection m_valueConversionCollection = null;

		KPIValue m_currentValue;
		float m_startingValue = 0.0f;
		Action<bool, KPIBar> m_barToggleCallback;

		public string ValueName => m_currentValue.name;
		public bool GraphToggled => m_graphToggle != null && m_graphToggle.isOn;

		void Start()
		{
			if(m_graphToggle != null)
            {
				m_graphToggle.onValueChanged.AddListener((a) => m_barToggleCallback.Invoke(a, this));
			}
		}

		public void SetContent(KPIValue a_value, Action<bool, KPIBar> a_barToggleCallback)
		{
			m_currentValue = a_value;
			m_title.text = a_value.displayName;
			m_barToggleCallback = a_barToggleCallback;
		}

		public void SetActual(float val, int countryId)
		{
			ConvertedUnitFloat convertedValue = GetConvertedValue(val);

			m_actual.UpdateValue(convertedValue);

			string changePercentage = KPIValue.FormatRelativePercentage(m_startingValue, val);
			m_actual.UpdateTooltip(changePercentage);
		}

		private ConvertedUnitFloat GetConvertedValue(float value)
		{
			ConvertedUnitFloat convertedValue;
			if (m_valueConversionCollection != null)
			{
				convertedValue = m_valueConversionCollection.ConvertUnit(value, m_currentValue.unit);
			}
			else
			{
				convertedValue = new ConvertedUnitFloat(value, m_currentValue.unit, 0);
			}

			return convertedValue;
		}

		public void SetStartValue(float value)
		{
			ConvertedUnitFloat convertedValue = GetConvertedValue(value);
			m_start.UpdateValue(convertedValue);
			m_startingValue = value;
		}

		public void SetDisplayedGraphColor(Color graphColor)
		{
			m_title.color = graphColor;
			m_graphColourDisplay.color = graphColor;
		}

		public void SetGraphToggled(bool a_value)
        {
			m_graphToggle.isOn = a_value;
		}
	}
}