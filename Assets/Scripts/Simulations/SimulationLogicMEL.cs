using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicMEL : ASimulationLogic
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
			//Currently in Server.GetMELConfig()

			SimulationSettingsMEL config = (SimulationSettingsMEL)a_settings;

			KPIManager.Instance.CreateEcologyKPIs(config.content);
			PlanManager.Instance.LoadFishingFleets(config.content);

			//TODO: run this when all simultion setup is done
			//if (loadAllLayers)
			//{
			//	ImportAllLayers();
			//}
		}

		public override void UpdateAfterEditing(Plan a_plan)
		{
		}
	}
}