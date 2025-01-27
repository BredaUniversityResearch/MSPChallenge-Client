using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public abstract class AGraphContentSelect : MonoBehaviour
	{
		[SerializeField] protected GraphContentSelectToggle[] m_contentToggles;
		[SerializeField] protected GameObject m_detailsWindowPrefab;
		[SerializeField] protected TextMeshProUGUI m_noDataEntry;

		protected ADashboardWidget m_widget;
		protected Action m_onSettingsChanged;
		//Fixed category, toggles for content
		//Fixed category, selectable country (or: all)
		//2 fixed categories, grouped by content (different name)

		public virtual void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			m_widget = a_widget;
			m_onSettingsChanged = a_onSettingsChanged;
			for(int i = 0; i < m_contentToggles.Length; i++ )
			{
				int index = i;
				m_contentToggles[i].m_detailsToggle.onValueChanged.AddListener((b) => ToggleDetails(b, index));

			}
		}

		void ToggleDetails(bool a_value, int a_index)
		{
			if (a_value)
				CreateDetailsWindow(a_index);
			else
				DestroyDetailsWindow(a_index);		
		}

		protected abstract void CreateDetailsWindow(int a_index);
		protected abstract void DestroyDetailsWindow(int a_index);
		public abstract GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue);
	}
}
