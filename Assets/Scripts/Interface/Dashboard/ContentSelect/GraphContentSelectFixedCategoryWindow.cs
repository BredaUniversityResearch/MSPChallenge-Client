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

		public void Initialise(bool[] a_valueToggles, List<KPIValue> a_values, Action<int, bool> a_callback)
		{
			m_callback = a_callback;
			for(int i = 0; i < a_valueToggles.Length; i++)
			{
				int index = i;
				GenericTextToggle entry = Instantiate(m_entryPrefab, m_entryParent).GetComponent<GenericTextToggle>();
				entry.m_toggle.isOn = a_valueToggles[i];
				entry.m_toggle.onValueChanged.AddListener((b) => { m_callback(index, b); });
				entry.m_text.text = a_values[i].displayName;
			}
		}
	}
}
