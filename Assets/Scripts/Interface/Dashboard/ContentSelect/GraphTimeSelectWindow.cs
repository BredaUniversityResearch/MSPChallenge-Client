using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class GraphTimeSelectWindow : MonoBehaviour
	{
		public Toggle m_yearToggle; //per month / per year
		public CustomDropdown m_aggregationDropdown;

		public Toggle m_rangeToggle; // Latest X / range
		public GameObject m_latestAmountSection;
		public CustomInputField m_latestAmountInput;
		public GameObject m_rangeSection;
		public Slider m_rangeMinSlider;
		public Slider m_rangeMaxSlider;
		public RectTransform m_rangeSliderFill;
		public TextMeshProUGUI m_rangeMinText;
		public TextMeshProUGUI m_rangeMaxText;
	}
}