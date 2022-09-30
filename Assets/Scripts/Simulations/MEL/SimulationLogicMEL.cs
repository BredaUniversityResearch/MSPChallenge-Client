using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicMEL : ASimulationLogic
	{
		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateMEL data = (SimulationUpdateMEL)a_data;
			KPIManager.Instance.ReceiveEcologyKPIUpdate(data.kpi);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			//Currently in Server.GetMELConfig()

			SimulationSettingsMEL config = (SimulationSettingsMEL)a_settings;

			KPIManager.Instance.CreateEcologyKPIs(config.content);
			PlanManager.Instance.LoadFishingFleets(config.content);

			//TODO: run this when all simulation setup is done
			//if (loadAllLayers)
			//{
			//	ImportAllLayers();
			//}
		}
	}
}