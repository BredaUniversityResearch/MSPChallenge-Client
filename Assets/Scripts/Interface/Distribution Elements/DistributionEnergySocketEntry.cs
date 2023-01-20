using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class DistributionEnergySocketEntry : MonoBehaviour
	{
		[SerializeField] Image m_teamBall;
		[SerializeField] TextMeshProUGUI m_teamName;
		[SerializeField] CustomDropdown m_sendReceiveDropdown;
		[SerializeField] DistributionSliderLong m_slider;
		[SerializeField] CustomInputField m_valueField; 
		[SerializeField] ValueConversionCollection m_valueConversionCollection;

		Team m_team;
		long m_socketMaximum;
		long m_remainingPower;
		long m_oldSliderValue;
		bool m_ignoreValueTextCallback, m_ignoreDropdownCallback;
		bool m_initialised;
		DistributionGroupEnergy m_group;

		public long CurrentValue
		{
			get { return Sending ? -m_slider.Value : m_slider.Value; }
		}

		public bool Changed
		{
			get { return CurrentValue != m_oldSliderValue; }
		}

		bool Sending 
		{ 
			get { return m_sendReceiveDropdown.value == 1; } 
			set 
			{
				m_ignoreDropdownCallback = true;
				m_sendReceiveDropdown.value = value ? 1 : 0; 
				m_ignoreDropdownCallback = false;
			}
		}

		public Team Team => m_team;

		void Initialise()
		{
			m_initialised = true;
			m_valueField.onEndEdit.AddListener(OnValueTextChange);
			m_slider.m_onChangeCallback = OnSliderValueChanged;
			m_sendReceiveDropdown.AddOptions(new List<string>(){ "Receive", "Send" });
			m_sendReceiveDropdown.onValueChanged.AddListener(OnSendReceiveDropdownChange);
		}

		public void SetContent(Team a_team, long a_socketMaximum, long a_allSocketMaximum, long a_sliderValue, /*long a_oldSliderValue,*/ DistributionGroupEnergy a_group, bool a_interactable)
		{
			if (!m_initialised)
				Initialise();

			m_team = a_team;
			m_teamName.text = m_team.name;
			m_teamBall.color = a_team.color;
			Sending = a_sliderValue < 0;
			m_socketMaximum = a_socketMaximum;
			m_group = a_group;
			m_slider.MaxValue = a_allSocketMaximum;
			m_oldSliderValue = a_sliderValue;
			SetValue(a_sliderValue);
			SetInteractability(a_interactable);
			gameObject.SetActive(true);
		}

		void OnSliderValueChanged()
		{
			UpdateValueTextNoCallback();
			m_group.UpdateEntireDistribution();
		}

		void UpdateValueTextNoCallback()
		{
			m_ignoreValueTextCallback = true;
			m_valueField.text = m_valueConversionCollection.ConvertUnit(m_slider.Value, ValueConversionCollection.UNIT_WATT).FormatAsString();
			m_ignoreValueTextCallback = false;
		}

		void OnValueTextChange(string newValueText)
		{
			if (m_ignoreValueTextCallback)
				return;

			long value = 0l;
			m_valueConversionCollection.ParseUnit(newValueText, ValueConversionCollection.UNIT_WATT, out value);
			if(Sending)
			{
				if (value > m_socketMaximum)
					value = m_socketMaximum;

			}
			else if(value > m_slider.MaxAvailableValue)
			{
				value = m_slider.MaxAvailableValue;
			}
			m_slider.Value = value;
			m_group.UpdateEntireDistribution();
		}

		void OnSendReceiveDropdownChange(int a_value)
		{
			if (m_ignoreDropdownCallback)
				return;

			m_slider.Value = 0;
			if(Sending)
			{
				m_slider.MaxAvailableValue = m_socketMaximum;
			}
			else
			{
				m_slider.MaxAvailableValue = Math.Min(m_socketMaximum, m_remainingPower);
			}
			OnSliderValueChanged();
		}

		public void SetInteractability(bool a_value)
		{
			m_slider.SetInteractablity(a_value);
			m_valueField.interactable = a_value;
		}

		public void SetValue(long a_newValue)
		{
			m_slider.Value = a_newValue;
			m_ignoreValueTextCallback = true;
			m_valueField.text = m_valueConversionCollection.ConvertUnit(a_newValue, ValueConversionCollection.UNIT_WATT).FormatAsString();
			m_ignoreValueTextCallback = false;
		}

		//public void SetOldValue(long oldValue)
		//{
		//	m_slider.SetOldValue(oldValue);
		//}

		public void SetRemainingPower(long a_remaining)
		{
			m_remainingPower = a_remaining;
			if(!Sending)
			{
				if (a_remaining < 0 && m_slider.Value > 0)
				{
					m_slider.Value = Math.Max(0, m_slider.Value + a_remaining);
				}
				m_slider.MaxAvailableValue = Math.Max(0, Math.Min(m_socketMaximum, m_slider.Value+a_remaining));
			}
			UpdateValueTextNoCallback();
		}
	}
}
