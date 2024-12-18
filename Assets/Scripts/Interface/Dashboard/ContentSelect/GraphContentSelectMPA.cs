using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace MSP2050.Scripts
{
	public class GraphContentSelectMPA : GraphContentSelectFixedCategory
	{
		public override void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			m_categoryNames = SimulationLogicMEL.Instance.ProtectionKPICategories.ToArray();
			base.Initialise(a_onSettingsChanged, a_widget);
		}
	}
}
