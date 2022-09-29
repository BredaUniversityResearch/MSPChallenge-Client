using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicSEL : ASimulationLogic
	{
		public override ASimulationData FormatPlanData(Plan a_plan)
		{
			return null;
		}

		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
		}

		public override void HandlePlanUpdate(ASimulationData a_data, Plan a_plan)
		{
		}

		public override void Initialise(ASimulationData a_settings)
		{
			SimulationSettingsSEL config = (SimulationSettingsSEL)a_settings;

			KPIManager.Instance.CreateShippingKPIBars(config.kpis);
			//TODO: handle direction colour
		}

		public override void UpdateAfterEditing(Plan a_plan)
		{
		}
	}
}