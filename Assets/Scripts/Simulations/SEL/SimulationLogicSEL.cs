using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicSEL : ASimulationLogic
	{
		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateSEL data = (SimulationUpdateSEL)a_data;
			KPIManager.Instance.ReceiveShippingKPIUpdate(data.kpi);
			if(data.shipping_issues != null && data.shipping_issues.Count > 0)
				IssueManager.Instance.UpdateShippingIssues(data.shipping_issues);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			SimulationSettingsSEL config = (SimulationSettingsSEL)a_settings;

			KPIManager.Instance.CreateShippingKPIBars(config.kpis);
			//TODO: handle direction colour
		}
	}
}