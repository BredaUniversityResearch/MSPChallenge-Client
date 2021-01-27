using System;
using System.Collections.Generic;
using UnityEngine;

class PlanDetailsTabShipping: LockablePlanDetailsTab
{
	private class DistributionItemEntry
	{
		public AbstractLayer targetLayer;
		public EntityType targetType;
		public DistributionItem item;

		public DistributionItemEntry(AbstractLayer targetLayer, EntityType targetType, DistributionItem item)
		{
			this.targetLayer = targetLayer;
			this.targetType = targetType;
			this.item = item;
		}
	};

	[SerializeField]
	private Distribution distributions = null;

	private Plan currentDistributionsForPlan = null;
	private Dictionary<EntityType, DistributionGroupShipping> distributionGroups = new Dictionary<EntityType, DistributionGroupShipping>();
	private List<DistributionItemEntry> distributionItemEntries = new List<DistributionItemEntry>();

	protected override string ContentName => "safety zones"; 
	protected override PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Shipping;

	protected override void Initialise()
	{
		base.Initialise();
		distributions.SetInteractability(false);
	}

	public override void UpdateTabAvailability()
	{
		base.UpdateTabAvailability();
		tabToggle.gameObject.SetActive(planDetails.SelectedPlan.shippingPlan);
		if (isActive && planDetails.SelectedPlan != null && !planDetails.SelectedPlan.shippingPlan)
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
		UpdateDistributions(planDetails.SelectedPlan);
		emptyContentOverlay.SetActive(distributions.NumberGroups == 0);
	}

	protected override void BeginEditing(Plan plan)
	{
		base.BeginEditing(plan);

		distributions.SetInteractability(true);
	}
	
	protected override void StopEditing()
	{
		distributions.SetInteractability(false);
		if (PlanManager.planViewing == lockedPlan)
			LayerManager.RedrawVisibleLayers();
		base.StopEditing();
	}

	protected override void SubmitChangesAndUnlock()
	{
		BatchRequest batch = new BatchRequest();

		ApplySliderValues(lockedPlan);
		RestrictionAreaManager.instance.SubmitSettingsForPlan(lockedPlan, batch);

		lockedPlan.AttemptUnlock(batch);
		InterfaceCanvas.ShowNetworkingBlocker();
		batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
	}

	public override void CancelChangesAndUnlock()
	{
		base.CancelChangesAndUnlock();

		UpdateDistributions(lockedPlan);

		lockedPlan.AttemptUnlock();
		StopEditing();
	}

	public void UpdateDistributions(Plan selectedPlan)
	{
		if (currentDistributionsForPlan != selectedPlan)
		{
			DestroyDistributions();
		}

		for (int i = 0; i < selectedPlan.PlanLayers.Count; ++i)
		{
			PlanLayer layer = selectedPlan.PlanLayers[i];
			foreach (var kvp in layer.BaseLayer.EntityTypes)
			{
				if (TeamManager.IsManager)
				{
					foreach (Team team in TeamManager.GetTeams())
					{
						if (!team.IsManager)
						{
							CreateDistributionForTeam(selectedPlan, team.ID, kvp, layer);
						}
					}
				}
				else
				{
					CreateDistributionForTeam(selectedPlan, TeamManager.CurrentUserTeamID, kvp, layer);
				}
			}
		}
		currentDistributionsForPlan = selectedPlan;
	}

	private void CreateDistributionForTeam(Plan selectedPlan, int teamId, KeyValuePair<int, EntityType> kvp, PlanLayer layer)
	{
		float restrictionSize = RestrictionAreaManager.instance.GetRestrictionAreaSizeAtPlanTime(selectedPlan, kvp.Value, teamId);
		SetDistributionSlider(layer.BaseLayer, kvp.Value, teamId, restrictionSize);
	}

	private void DestroyDistributions()
	{
		distributions.DestroyAllGroups();
		distributionGroups.Clear();
	}

	private void SetDistributionSlider(AbstractLayer baseLayer, EntityType entityType, int teamId, float restrictionSize)
	{
		DistributionGroupShipping group;
		if (!distributionGroups.TryGetValue(entityType, out group))
		{
			group = (DistributionGroupShipping)distributions.CreateGroup(baseLayer.ShortName);
			group.SetTitle(baseLayer.ShortName, entityType.Name);
			distributionGroups.Add(entityType, group);
		}

		DistributionItem item = group.FindDistributionItemByCountryId(teamId);
		if (item == null)
		{
			item = group.CreateItem(teamId, 1f);
			distributionItemEntries.Add(new DistributionItemEntry(baseLayer, entityType, item));
		}
		group.UpdateDistributionItem(item, restrictionSize);
	}

	public void ApplySliderValues(Plan selectedPlan)
	{
		foreach (DistributionItemEntry itemEntry in distributionItemEntries)
		{
			if (itemEntry.item.changed)
			{
				RestrictionAreaSetting setting = new RestrictionAreaSetting(itemEntry.item.Country, itemEntry.item.GetDistributionValue());

				RestrictionAreaManager.instance.SetRestrictionAreaSetting(selectedPlan, itemEntry.targetType, setting);
			}
		}		
	}
}

