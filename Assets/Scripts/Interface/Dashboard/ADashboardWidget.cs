using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public abstract class ADashboardWidget : MonoBehaviour
	{
		public DashboardCategory m_category;
		public bool m_startingWidget;
	}
}