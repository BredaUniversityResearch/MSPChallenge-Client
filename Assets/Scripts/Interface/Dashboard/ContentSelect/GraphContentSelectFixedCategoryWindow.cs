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

		Action<int, bool> m_callback;

		public void Initialise(HashSet<string> a_selectedIDs, List<string> a_allIDs, List<string> a_displayIDs, Action<int, bool> a_callback)
		{
			m_callback = a_callback;
			for(int i = 0; i < a_allIDs.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				entry.m_toggle.isOn = a_selectedIDs.Contains(a_allIDs[i]);
				entry.m_toggle.onValueChanged.AddListener((b) => { m_callback(index, b); });
				entry.m_text.text = a_displayIDs[i];
			}
		}

		public void Initialise(HashSet<int> a_selectedCountries, List<int> a_allCountries, Action<int, bool> a_callback)
		{
			m_callback = a_callback;
			for (int i = 0; i < a_allCountries.Count; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				entry.m_toggle.isOn = a_selectedCountries.Contains(a_allCountries[i]);
				entry.m_toggle.onValueChanged.AddListener((b) => { m_callback(index, b); });
				entry.m_text.text = SessionManager.Instance.GetTeamByTeamID(a_allCountries[i]).name;
			}
		}
	}
}
