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
		[SerializeField] protected TextMeshProUGUI m_summaryText;
		[SerializeField] protected Toggle m_detailsToggle;
		[SerializeField] protected GameObject m_detailsWindowPrefab;
		[SerializeField] protected Transform m_detailsWindowParent;
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
			m_detailsToggle.onValueChanged.AddListener(ToggleDetails);
		}

		void ToggleDetails(bool a_value)
		{
			if (a_value)
				CreateDetailsWindow();
			else
				DestroyDetailsWindow();		
		}

		protected abstract void CreateDetailsWindow();
		protected abstract void DestroyDetailsWindow();
		public abstract GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, out float a_maxValue, out float a_minValue);
	}
}
