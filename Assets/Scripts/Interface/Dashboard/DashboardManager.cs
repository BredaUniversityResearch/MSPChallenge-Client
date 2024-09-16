using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class DashboardManager : MonoBehaviour
	{
		[SerializeField] Button m_closeButton;
		[SerializeField] string m_widgetAssetPath;
		[SerializeField] string m_categoriesAssetPath;

		List<DashboardCategoryToggle> m_categoryToggles;
		Dictionary<DashboardCategory, List<ADashboardWidget>> m_widgets;

		private void Start()
		{
			if (SimulationManager.Instance.Initialised)
			{
				InitialiseDashboard();
			}
			else
				SimulationManager.Instance.m_onSimulationsInitialised += InitialiseDashboard;
		}

		async void InitialiseDashboard()
		{
			//Load dashboard categories
			//Load widgets
			GameObject[] widgetsObjs = Resources.LoadAll<GameObject>(m_widgetAssetPath);
			DashboardCategory[] categories = Resources.LoadAll<DashboardCategory>(m_categoriesAssetPath);
			
			foreach (var kvp in SimulationManager.Instance.Settings)
			{ 
				foreach(var cat in categories)
				{
					if(kvp.Key.Equals(cat.name))
					{
						m_widgets.Add(cat, new List<ADashboardWidget>());
						break;
					}
				}
			}

            foreach (GameObject widgetObj in widgetsObjs)
            {
				ADashboardWidget widget = widgetObj.GetComponent<ADashboardWidget>();

				if (widget != null && widget.m_startingWidget && m_widgets.ContainsKey(widget.m_category))
				{
					m_widgets[widget.m_category].Add(widget);
				}
            }
        }
	}
}