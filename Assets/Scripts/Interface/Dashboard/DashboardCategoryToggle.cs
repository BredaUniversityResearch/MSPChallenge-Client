using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class DashboardCategoryToggle : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] Image m_icon;

		DashboardCategory m_category;
		Action<DashboardCategory> m_onSelect;

		private void Start()
		{
			m_toggle.onValueChanged.AddListener(OnToggleChanged);
		}

		public void Initialise(DashboardCategory a_category, ToggleGroup a_group, Action<DashboardCategory> a_onSelect)
		{
			m_category = a_category;
			m_onSelect = a_onSelect;
			m_toggle.group = a_group;
			m_icon.sprite = a_category.m_icon;
		}

		void OnToggleChanged(bool a_value)
		{ 
			if(a_value) 
				m_onSelect.Invoke(m_category);
		}

		public void ForceActive()
		{
			m_toggle.isOn = true;
		}
	}
}