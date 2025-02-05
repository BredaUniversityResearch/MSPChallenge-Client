using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public abstract class GraphContentSelectWindow : MonoBehaviour
	{
		public abstract void SetContent(HashSet<string> a_selectedIDs, List<string> a_allIDs, List<string> a_displayIDs, Action<int, bool> a_callback, Action<bool> a_allChangeCallback);
		public abstract void SetContent(HashSet<KPIValue> a_selectedValues, List<KPIValue> a_allValues, Action<int, bool> a_callback, Action<bool> a_allChangeCallback);
		public abstract void SetContent(HashSet<int> a_selectedCountries, List<int> a_allCountries, Action<int, bool> a_callback, Action<bool> a_allChangeCallback);
	}
}
