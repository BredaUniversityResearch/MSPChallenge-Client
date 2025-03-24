using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectSingleSelectWindow : GraphContentSelectWindow
	{
		public GameObject m_entryPrefab;
		public Transform m_entryParent;

		Action<int, bool> m_callback;
		bool m_ignoreCallbacks;
		List<GenericTextToggle> m_entries;
		int m_selectedIndex;

		public override void SetContent(HashSet<string> a_selectedIDs, List<string> a_allIDs, List<string> a_displayIDs, Action<int, bool> a_callback, Action<bool> a_allChangeCallback)
		{
			m_callback = a_callback;
			ClearEntries();
			m_selectedIndex = -1;

			for (int i = 0; i < a_allIDs.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				if(a_selectedIDs.Contains(a_allIDs[i]))
				{
					m_selectedIndex = i;
					entry.m_toggle.isOn = true;
					entry.m_toggle.interactable = false;
				}
				else
					entry.m_toggle.isOn = false;
				entry.m_toggle.onValueChanged.AddListener((b) => { ToggleChangedCallback(index, b); });
				entry.m_text.text = a_displayIDs[i]; 
				m_entries.Add(entry);
			}
			if (m_selectedIndex < 0)
				m_selectedIndex = 0;
		}

		public override void SetContent(HashSet<KPIValue> a_selectedValues, List<KPIValue> a_allValues,  Action<int, bool> a_callback, Action<bool> a_allChangeCallback)
		{
			m_callback = a_callback;
			ClearEntries();
			m_selectedIndex = -1;

			for (int i = 0; i < a_allValues.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				if (a_selectedValues.Contains(a_allValues[i]))
				{
					m_selectedIndex = i;
					entry.m_toggle.isOn = true;
					entry.m_toggle.interactable = false;
				}
				else
					entry.m_toggle.isOn = false;
				entry.m_toggle.onValueChanged.AddListener((b) => { ToggleChangedCallback(index, b); });
				entry.m_text.text = a_allValues[i].displayName;
				m_entries.Add(entry);
			}
			if (m_selectedIndex < 0)
				m_selectedIndex = 0;
		}

		public override void SetContent(HashSet<int> a_selectedCountries, List<int> a_allCountries, Action<int, bool> a_callback, Action<bool> a_allChangeCallback)
		{
			m_callback = a_callback;
			ClearEntries();
			m_selectedIndex = -1;

			for (int i = 0; i < a_allCountries.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				if (a_selectedCountries.Contains(a_allCountries[i]))
				{
					m_selectedIndex = i;
					entry.m_toggle.isOn = true;
					entry.m_toggle.interactable = false;
				}
				else
					entry.m_toggle.isOn = false;
				entry.m_toggle.onValueChanged.AddListener((b) => { ToggleChangedCallback(index, b); });
				if(a_allCountries[i] <= 0)
				{
					entry.m_text.text = "All";
				}
				else
					entry.m_text.text = SessionManager.Instance.GetTeamByTeamID(a_allCountries[i]).name;
				m_entries.Add(entry);
			}
			if (m_selectedIndex < 0)
				m_selectedIndex = 0;
		}

		void ClearEntries()
		{
			if (m_entries != null)
			{
				foreach (GenericTextToggle entry in m_entries)
				{
					Destroy(entry.gameObject);
				}
			}
			m_entries = new List<GenericTextToggle>();
		}

		void ToggleChangedCallback(int a_index, bool a_value)
		{
			if (m_ignoreCallbacks)
				return;

			m_ignoreCallbacks = true;
			m_callback.Invoke(m_selectedIndex, false);
			m_entries[m_selectedIndex].m_toggle.isOn = false;
			m_entries[m_selectedIndex].m_toggle.interactable = true;
			m_ignoreCallbacks = false;

			m_selectedIndex = a_index;
			m_entries[a_index].m_toggle.interactable = false;
			m_callback.Invoke(a_index, true);
		}
	}
}
