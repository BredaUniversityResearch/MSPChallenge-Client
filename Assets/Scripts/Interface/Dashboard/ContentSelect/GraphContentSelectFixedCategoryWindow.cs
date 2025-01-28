using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectFixedCategoryWindow : MonoBehaviour
	{
		public GameObject m_entryPrefab;
		public Transform m_entryParent;
		[SerializeField] Button m_allButton;
		[SerializeField] Button m_noneButton;

		Action<int, bool> m_callback;
		Action<bool> m_allChangeCallback;
		bool m_ignoreCallbacks;
		List<GenericTextToggle> m_entries;

		public void Initialise(HashSet<string> a_selectedIDs, List<string> a_allIDs, List<string> a_displayIDs, Action<int, bool> a_callback, Action<bool> a_allChangeCallback)
		{
			m_callback = a_callback;
			m_allChangeCallback = a_allChangeCallback;
			m_entries = new List<GenericTextToggle>();
			m_allButton.onClick.AddListener(ToggleAll);
			m_noneButton.onClick.AddListener(ToggleNone);

			for (int i = 0; i < a_allIDs.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				entry.m_toggle.isOn = a_selectedIDs.Contains(a_allIDs[i]);
				entry.m_toggle.onValueChanged.AddListener((b) => { ToggleChangedCallback(index, b); });
				entry.m_text.text = a_displayIDs[i]; 
				m_entries.Add(entry);
			}
		}

		public void Initialise(HashSet<int> a_selectedCountries, List<int> a_allCountries, Action<int, bool> a_callback, Action<bool> a_allChangeCallback)
		{
			m_callback = a_callback;
			m_allChangeCallback = a_allChangeCallback;
			m_entries = new List<GenericTextToggle>();
			m_allButton.onClick.AddListener(ToggleAll);
			m_noneButton.onClick.AddListener(ToggleNone);

			for (int i = 0; i < a_allCountries.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				entry.m_toggle.isOn = a_selectedCountries.Contains(a_allCountries[i]);
				entry.m_toggle.onValueChanged.AddListener((b) => { ToggleChangedCallback(index, b); });
				entry.m_text.text = SessionManager.Instance.GetTeamByTeamID(a_allCountries[i]).name;
				m_entries.Add(entry);
			}
		}

		void ToggleAll()
		{
			if (m_ignoreCallbacks)
				return;

			m_ignoreCallbacks = true;
			foreach (GenericTextToggle entry in m_entries)
			{
				entry.m_toggle.isOn = true;
			}
			m_ignoreCallbacks = false;

			m_allChangeCallback.Invoke(true);
		}

		void ToggleNone()
		{
			if (m_ignoreCallbacks)
				return;

			m_ignoreCallbacks = true;
			foreach(GenericTextToggle entry in m_entries)
			{
				entry.m_toggle.isOn = false;
			}
			m_ignoreCallbacks = false;

			m_allChangeCallback.Invoke(false);
		}

		void ToggleChangedCallback(int a_index, bool a_value)
		{
			if (m_ignoreCallbacks)
				return;

			m_callback.Invoke(a_index, a_value);
		}
	}
}
