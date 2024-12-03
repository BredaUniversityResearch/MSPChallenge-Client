using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPIEnergyGridDisplay: MonoBehaviour
	{
		[SerializeField]
		private KPIOtherValueArea kpiGroups = null;

		private int targetTeamId = -1;
		private KPIValueCollection targetKPICollection = null;

		private void Start()
		{
			EnergyGridReceivedEvent.Event += OnEnergyGridReceived;
		}

		private void OnDestroy()
		{
			EnergyGridReceivedEvent.Event -= OnEnergyGridReceived;
		}

		private void OnEnable()
		{
			//Refresh content when opened if not valid previously
			if(targetTeamId != -1 && targetKPICollection == null)
			{
				SetBarsToEnergyGridsForCountry(targetTeamId);
			}
		}

		//Callback for use within the Unity UI.
		public void SetBarsToEnergyGridsForCountry(int country)
		{
			targetTeamId = country;
			if (targetKPICollection != null)
			{
				targetKPICollection.OnKPIValuesUpdated -= OnTargetCollectionValuesUpdated;
			}

			targetKPICollection = SimulationManager.Instance.GetKPIValuesForSimulation(SimulationManager.CEL_SIM_NAME, targetTeamId)[0];

			if (targetKPICollection != null)
			{
				targetKPICollection.OnKPIValuesUpdated += OnTargetCollectionValuesUpdated;
				ShowGridsForCountry(targetTeamId);
			}

		}

		private void OnEnergyGridReceived()
		{
			if (targetKPICollection == null)
			{
				//If we still don't have a target update it manually. 
				SetBarsToEnergyGridsForCountry(targetTeamId);
			}

			ShowGridsForCountry(targetTeamId);
		}

		private void OnTargetCollectionValuesUpdated(KPIValueCollection sourceCollection, int previousMostRecentMonth, int mostRecentMonthReceived)
		{
			ShowGridsForCountry(targetTeamId);
		}

		private void ShowGridsForCountry(int teamId)
		{
			List<EnergyGrid> grids = PolicyLogicEnergy.Instance.GetEnergyGridsAtTime(TimeManager.Instance.GetCurrentMonth(), EnergyGrid.GridColor.Either);//Or should this be current month -1
			kpiGroups.SetBarsToGrids(grids, teamId);
		}
	}
}
