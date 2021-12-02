using System.Collections.Generic;
using KPI;
using UnityEngine;

public class KPIEnergyGridDisplay: MonoBehaviour
{
	[SerializeField]
	private KPIGroups kpiGroups = null;

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

	//Callback for use within the Unity UI.
	public void SetBarsToEnergyGridsForCountry(int country)
	{
		targetTeamId = country;
		if (targetKPICollection != null)
		{
			targetKPICollection.OnKPIValuesUpdated -= OnTargetCollectionValuesUpdated;
		}

		targetKPICollection = KPIManager.GetKPIValuesForCategory(EKPICategory.Energy, targetTeamId);

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
		List<EnergyGrid> grids = PlanManager.GetEnergyGridsAtTime(GameState.GetCurrentMonth(), EnergyGrid.GridColor.Either);//Or should this be current month -1
		kpiGroups.SetBarsToGrids(grids, teamId);
	}
}
