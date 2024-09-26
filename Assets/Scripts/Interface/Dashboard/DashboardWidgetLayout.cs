using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class DashboardWidgetLayout
	{
		const int columns = 5;
		List<ADashboardWidget> m_widgets;
		List<ADashboardWidget[]> m_widgetLayout;

		public DashboardWidgetLayout()
		{
			m_widgets = new List<ADashboardWidget>();
			m_widgetLayout = new List<ADashboardWidget[]>() { new ADashboardWidget[5] };
		}

		public void AddWidgetFromCatalogue(ADashboardWidget a_widget)
		{ 
		
		}

		public void DuplicateWidget(ADashboardWidget a_widget)
		{ }

		public void ChangeWidgetSize(ADashboardWidget a_widget, int a_newW, int a_newH)
		{ }
	}
}