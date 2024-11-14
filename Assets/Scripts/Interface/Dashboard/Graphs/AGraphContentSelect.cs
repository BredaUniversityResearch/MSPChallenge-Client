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
		//Fixed category, toggles for content
		//Fixed category, selectable country (or: all)
		//2 fixed categories, grouped by content (different name)

		public abstract GraphDataStepped FetchData(GraphTimeSettings a_timeSettings);
	}
}
