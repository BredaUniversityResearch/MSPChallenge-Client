using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanDetailsTabEcology : LockablePlanDetailsTab
{
	[SerializeField]
	Distribution ecologyDistribution;

	private FishingDistributionDelta fishingBackup;

	protected override string ContentName => "fishing distributions"; 
	protected override PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Ecology;

	protected override void Initialise()
	{
		base.Initialise();
		ecologyDistribution.SetInteractability(false);
	}

	public override void UpdateTabAvailability()
	{
		base.UpdateTabAvailability();
		tabToggle.gameObject.SetActive(planDetails.SelectedPlan.ecologyPlan);
		if (isActive && planDetails.SelectedPlan != null && !planDetails.SelectedPlan.ecologyPlan)
		{
			SetEditContentButtonEnabled(false);
			planDetails.TabSelect(PlanDetails.EPlanDetailsTab.Feedback);
			SetTabActive(false);
		}
	}

	public override void UpdateTabContent()
	{
		if (!isActive || lockedPlan != null)
			return;
		if (planDetails.SelectedPlan.fishingDistributionDelta == null)
			planDetails.SelectedPlan.fishingDistributionDelta = new FishingDistributionDelta();
		ecologyDistribution.SetSliderValuesToFishingDistribution(PlanManager.GetFishingDistributionForPreviousPlan(planDetails.SelectedPlan), planDetails.SelectedPlan.fishingDistributionDelta);
		emptyContentOverlay.SetActive(ecologyDistribution.NumberGroups == 0);
	}

	protected override void BeginEditing(Plan plan)
	{
		base.BeginEditing(plan);

		fishingBackup = lockedPlan.fishingDistributionDelta;
		ecologyDistribution.SetInteractability(true);
		if (lockedPlan.fishingDistributionDelta != null && lockedPlan.fishingDistributionDelta.HasDistributionValues())
		{
			//Calculate an initial distribution if it was empty before
			lockedPlan.fishingDistributionDelta = lockedPlan.fishingDistributionDelta.Clone();
		}
		else
		{
			lockedPlan.fishingDistributionDelta = new FishingDistributionDelta();
		}
		ecologyDistribution.SetSliderValuesToFishingDistribution(PlanManager.GetFishingDistributionForPreviousPlan(planDetails.SelectedPlan), planDetails.SelectedPlan.fishingDistributionDelta);
	}

	protected override void StopEditing()
	{
		fishingBackup = null;
		ecologyDistribution.SetInteractability(false);

		base.StopEditing();
	}

	protected override void SubmitChangesAndUnlock()
	{
		BatchRequest batch = new BatchRequest();


		ecologyDistribution.SetFishingToSliderValues(lockedPlan);

		// Submit entire fishing distribution to server (regardless of changes)
		lockedPlan.fishingDistributionDelta.SubmitToServer(lockedPlan.ID, batch);

		//Update required approval
		Dictionary<int, EPlanApprovalState> newApproval = lockedPlan.CalculateRequiredApproval(null); //TODO: this might still need removed grids
		lockedPlan.SubmitRequiredApproval(batch, newApproval);

		lockedPlan.AttemptUnlock(batch);
		InterfaceCanvas.ShowNetworkingBlocker();
		batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
	}

	public override void CancelChangesAndUnlock()
	{
		base.CancelChangesAndUnlock();

		lockedPlan.fishingDistributionDelta = fishingBackup;

		lockedPlan.AttemptUnlock();
		StopEditing();
	}
}

