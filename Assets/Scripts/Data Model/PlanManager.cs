using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.Networking;


public static class PlanManager
{
	public enum PlanViewState { All, Base, Changes, Time };

	private static List<Plan> plans = new List<Plan>();
	private static Dictionary<int, PlanLayer> planLayers = new Dictionary<int, PlanLayer>();
	private static Dictionary<int, EnergyGrid> energyGrids = new Dictionary<int, EnergyGrid>();
	private static HashSet<Plan> unseenPlanChanges = new HashSet<Plan>();

	//Fishing
	public static List<string> fishingFleets;
	public static float initialFishingMapping;
	public static float fishingDisplayScale;
	public static float shippingDisplayScale = 10000; // = 10km
	public static FishingDistributionDelta initialFishingValues { get; private set; }

	//Viewing & Viewstates
	public static PlanViewState planViewState = PlanViewState.All;
	public static Plan planViewing;
	public static int timeViewing = -1; //Used if planViewing is null. -1 is current time.
	public static bool inPlanUIChange;
    private static int planToViewOnUpdate;

    private static bool ignoreRedrawOnViewStateChange = false;

	public static Plan ProcessReceivedPlan(PlanObject planObject, Dictionary<AbstractLayer, int> layerUpdateTimes)
	{
		int planID = planObject.id;
		Plan targetPlan = GetPlanWithID(planID);

        if (targetPlan != null)
        {
            targetPlan.UpdatePlan(planObject, layerUpdateTimes);
        }
        else
		{
			targetPlan = new Plan(planObject, layerUpdateTimes);
			AddPlan(targetPlan);
			PlanAdded(targetPlan);
			//tracker.CompletedUpdate();
		}

		RestrictionAreaManager.instance.ProcessReceivedRestrictions(targetPlan, planObject.restriction_settings);
		return targetPlan;
	}

	public static void AddPlan(Plan newPlan)
	{
		if (plans.Count == 0)
		{
			plans.Add(newPlan);
			return;
		}

		for (int i = 0; i < plans.Count; i++)
			if (plans[i].StartTime > newPlan.StartTime)
			{
				plans.Insert(i, newPlan);
				return;
			}

		plans.Add(newPlan);
	}

	public static void UpdatePlanTime(Plan updatedPlan)
	{
		plans.Remove(updatedPlan);
		AddPlan(updatedPlan);
	}

	public static void SetPlanViewState(PlanManager.PlanViewState state, bool redraw = true)
	{
		bool needsRedraw = redraw && (!ignoreRedrawOnViewStateChange && planViewState != state);
		planViewState = state;
		if (needsRedraw)
			LayerManager.RedrawVisibleLayers();
	}

	public static void ShowWorldAt(int time)
	{
		if (timeViewing == time || planViewing != null)
			return;
		if (time == -1)
			LayerManager.UpdateVisibleLayersToBase();
		else
			LayerManager.UpdateVisibleLayersToTime(time);
		timeViewing = time;
	}

	public static void ShowPlan(Plan plan)
	{
		if (Main.InEditMode || Main.EditingPlanDetailsContent)
			return;

		//InterfaceCanvas.Instance.viewTimeWindow.CloseWindow(false);
		UIManager.ignoreLayerToggleCallback = true;
		if (planViewing != null)
		{
			PlansMonitor.SetViewPlanFrameState(planViewing, false);
		}
		planViewing = plan;
        timeViewing = -1;
        PlansMonitor.SetViewPlanFrameState(planViewing, true);
        InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Plan, false);//Needs to be done before redraw
		LayerManager.UpdateVisibleLayersToPlan(plan);
		UIManager.ignoreLayerToggleCallback = false;
		InterfaceCanvas.Instance.activePlanWindow.SetToPlan(plan);
	}

	public static void HideCurrentPlan(bool updateLayers = true)
	{
		if (Main.InEditMode || Main.EditingPlanDetailsContent)
			return;

		UIManager.ignoreLayerToggleCallback = true;
		if (planViewing != null)
		{
			PlansMonitor.SetViewPlanFrameState(planViewing, false);
		}
		planViewing = null;

        //Doesnt have to redraw as we'll do so when updating layers to base anyway
        ignoreRedrawOnViewStateChange = true;
        InterfaceCanvas.Instance.activePlanWindow.viewAllToggle.isOn = true;
        ignoreRedrawOnViewStateChange = false;

        if(updateLayers)
            LayerManager.UpdateVisibleLayersToBase();
		UIManager.ignoreLayerToggleCallback = false;
		InterfaceCanvas.Instance.activePlanWindow.CloseWindow();
		InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Normal, false);
	}

	public static SubEntityPlanState GetSubEntityPlanState(SubEntity subEntity)
	{
		//added, moved, removed, notinplan, notshown
		PlanLayer currentPlanLayer = subEntity.Entity.Layer.CurrentPlanLayer();
		bool layerInPlan = planViewing == null || planViewing.IsLayerpartOfPlan(subEntity.Entity.Layer);

		if (planViewState == PlanViewState.All)
		{
			if (currentPlanLayer == null) //Only show the base layer
			{
				if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
				else
					return SubEntityPlanState.NotShown;
			}
			if (!layerInPlan)
			{
				if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
				else
					return SubEntityPlanState.NotShown;
			}
			if (currentPlanLayer.IsDatabaseIDInNewGeometry(subEntity.GetDatabaseID()))
			{
				if (!currentPlanLayer.BaseLayer.IsEntityTypeVisible(subEntity.Entity.EntityTypes))
					return SubEntityPlanState.NotShown;
				if (subEntity.Entity.Layer.IsPersisIDCurrentlyNew(subEntity.GetPersistentID()))
					return SubEntityPlanState.Added;
				return SubEntityPlanState.Moved;
			}
			if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
			{
				if (currentPlanLayer.IsPersistentIDInRemovedGeometry(subEntity.GetPersistentID()))
					return SubEntityPlanState.Removed;
				return SubEntityPlanState.NotInPlan;
			}
		}
		else if (planViewState == PlanViewState.Base)
		{
			if (currentPlanLayer == null) //Only show the base layer
			{
				if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
				else
					return SubEntityPlanState.NotShown;
			}
			if (!layerInPlan)
			{
				if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
				else
					return SubEntityPlanState.NotShown;
			}
			if (currentPlanLayer.IsPersistentIDInRemovedGeometry(subEntity.GetPersistentID()) || subEntity.Entity.Layer.IsDatabaseIDPreModified(subEntity.GetDatabaseID()))
				return SubEntityPlanState.NotInPlan;
			if (subEntity.Entity.Layer.IsPersisIDCurrentlyNew(subEntity.GetPersistentID()) || currentPlanLayer.IsDatabaseIDInNewGeometry(subEntity.GetDatabaseID()))
				return SubEntityPlanState.NotShown;
			if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
				return SubEntityPlanState.NotInPlan;
		}
		else if (planViewState == PlanViewState.Changes)
		{
			if (currentPlanLayer == null) //Only show the base layer
				return SubEntityPlanState.NotShown;
			if (!layerInPlan)
				return SubEntityPlanState.NotShown;
			if (currentPlanLayer.IsDatabaseIDInNewGeometry(subEntity.GetDatabaseID()))
			{
				if (!currentPlanLayer.BaseLayer.IsEntityTypeVisible(subEntity.Entity.EntityTypes))
					return SubEntityPlanState.NotShown;
				if (subEntity.Entity.Layer.IsPersisIDCurrentlyNew(subEntity.GetPersistentID()))
					return SubEntityPlanState.Added;
				return SubEntityPlanState.Moved;
			}
			if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()) && currentPlanLayer.IsPersistentIDInRemovedGeometry(subEntity.GetPersistentID()))
				return SubEntityPlanState.Removed;
		}
		else //PlanViewState.Time
		{
			if (subEntity.Entity.Layer.IsIDInActiveGeometry(subEntity.GetDatabaseID()))
				return SubEntityPlanState.NotInPlan;
			else
				return SubEntityPlanState.NotShown;
		}
		return SubEntityPlanState.NotShown;
	}

	public static void ViewPlanOnMap(Plan plan)
	{
		foreach (PlanLayer planLayer in plan.PlanLayers)
			LayerManager.ShowLayer(planLayer.BaseLayer);

		CameraManager.Instance.ZoomToBounds(plan.GetBounds());
	}

	public static void ViewPlanLayerOnMap(PlanLayer planLayer)
	{
		LayerManager.ShowLayer(planLayer.BaseLayer);
		CameraManager.Instance.ZoomToBounds(planLayer.GetBounds());
	}
	
	public static void RequestForceUnlockPlan(Plan plan)
	{
		plan.AttemptUnlock(true);
	}

	public static void StartEditingLayer(PlanLayer planLayer)
	{
        LayerManager.SetNonReferenceLayers(new HashSet<AbstractLayer>() { planLayer.BaseLayer }, false, true);
		LayerManager.ShowLayer(planLayer.BaseLayer);
		PlanDetails.LayersTab.StartEditingLayer(planLayer);
	}

	public static int GetPlanCount()
	{
		return plans.Count;
	}

	public static Plan GetPlanAtIndex(int index)
	{
		return plans[index];
	}

	public static Plan GetPlanWithID(int planID)
	{
		foreach (Plan plan in plans)
		{
			if (plan.ID == planID)
			{
				return plan;
			}
		}
		return null;
	}

	public static List<Plan> GetAllPlansFrom(int month)
	{
		List<Plan> result = new List<Plan>();
		for (int i = plans.Count - 1; i >= 0; i--)
		{
			if (plans[i].StartTime < month)
				break;
			result.Add(plans[i]);
		}
		return result;
	}

	/// <summary>
	/// Returns plan layers for a base layer from a specific month onwards
	/// </summary>
	/// <param name="baseLayer">The base layer we need to get the geometry from</param>
	/// <param name="planStartTime">Exclusive from what date on we want to get the layers</param>
	/// <param name="onlyInfluencingPlans">Only plans that are in the influencing state</param>
	/// <returns></returns>
	public static List<PlanLayer> GetPlanLayersForBaseLayerFrom(AbstractLayer baseLayer, int planStartTime, bool onlyInfluencingPlans)
	{
		List<PlanLayer> result = new List<PlanLayer>(32);
		//Iterate forwards so the list is in order from first occuring layer to last occuring layer. This helps us with checks in the future
		for (int i = 0; i < plans.Count; ++i)
		{
			Plan plan = plans[i];
			if (plan.StartTime <= planStartTime ||
				(onlyInfluencingPlans && !plan.InInfluencingState))
			{
				continue;
			}

			PlanLayer planLayer = plan.GetPlanLayerForLayer(baseLayer);
			if (planLayer != null)
			{
				result.Add(planLayer);
			}
		}
		return result;
	}

	public static Plan FindFirstPlanChangingGeometry(int fromMonth, int entityPersistentId, AbstractLayer baseLayer)
	{
		Plan result = null;
		for (int i = 0; i < plans.Count; ++i)
		{
			Plan plan = plans[i];
			if (plan.StartTime <= fromMonth)
			{
				continue;
			}

			PlanLayer planLayer = plan.GetPlanLayerForLayer(baseLayer);
			if (planLayer != null)
			{
				if (planLayer.IsPersistentIDInNewGeometry(entityPersistentId) ||
					planLayer.IsPersistentIDInRemovedGeometry(entityPersistentId))
				{
					result = plan;
					break;
				}
			}
		}
		return result;
	}

	public static PlanLayer GetPlanLayer(int ID)
	{
		if (planLayers.ContainsKey(ID))
			return planLayers[ID];
		else
			return null;
	}

	public static bool RemovePlanLayer(PlanLayer planLayer)
	{
		return planLayers.Remove(planLayer.ID);
	}

	public static void AddEnergyGrid(EnergyGrid energyGrid)
	{
		energyGrids[energyGrid.GetDatabaseID()] = energyGrid;
	}

	public static EnergyGrid GetEnergyGrid(int ID)
	{
		return energyGrids[ID];
	}

	public static bool RemoveEnergyGridr(EnergyGrid energyGrid)
	{
		return energyGrids.Remove(energyGrid.GetDatabaseID());
	}

	/// <summary>
	/// Called whenever a new month starts
	/// </summary>
	/// <param name="newMonth">month that just started</param>
	public static void MonthTick(int newMonth)
	{
		//Advance time on layers (merging approved ones) 
		foreach (AbstractLayer layer in LayerManager.GetAllValidLayers())
			layer.AdvanceTimeTo(newMonth);
	}

	/// <summary>
	/// Returns a list of energy grids that are active right before the given plan would be implemented.
	/// </summary>
	/// <param name="plan"> Plan before which grids are calced </param>
	/// <param name="removedGridIds"> Persistent IDs of grids that have been removed in at this plan's point</param>
	/// <param name="includePlanItself"> Is the given plan included </param>
	/// <param name="forDisplaying"> Is the given plan included even if it's in design</param>
	/// <returns></returns>
	public static List<EnergyGrid> GetEnergyGridsBeforePlan(Plan plan, out HashSet<int> removedGridIds, EnergyGrid.GridColor color, bool includePlanItself = false, bool forDisplaying = false)
	{
		List<EnergyGrid> result = new List<EnergyGrid>();
		removedGridIds = new HashSet<int>();
		HashSet<int> ignoredGridIds = new HashSet<int>();
		HashSet<int> previousGridIDsLookingFor = new HashSet<int>();

		//Find the index of the given plan
		int planIndex = 0;
		for (; planIndex < plans.Count; planIndex++)
			if (plans[planIndex] == plan)
				break;

		//Handle plan itself if conditions are met
		if (includePlanItself && plan.energyPlan && plan.energyGrids != null && (plan.InInfluencingState || (forDisplaying && plan.State == Plan.PlanState.DESIGN)))
		{
			foreach (EnergyGrid grid in plan.energyGrids)
			{
				if (!grid.MatchesColor(color))
					continue;
				if (grid.persistentID == -1 || (!removedGridIds.Contains(grid.persistentID) && !ignoredGridIds.Contains(grid.persistentID)))
				{
					result.Add(grid);
					ignoredGridIds.Add(grid.persistentID);
				}
			}
			removedGridIds.UnionWith(plans[planIndex].removedGrids);
			if (forDisplaying)
				previousGridIDsLookingFor = new HashSet<int>(plans[planIndex].removedGrids);
		}

		//Add all grids whose persistentID is not in ignoredgrids
		for (int i = planIndex - 1; i >= 0; i--)
		{
			if (plans[i].energyPlan && plans[i].InInfluencingState)
			{
				foreach (EnergyGrid grid in plans[i].energyGrids)
				{
					if (!grid.MatchesColor(color))
						continue;
					if (previousGridIDsLookingFor.Contains(grid.persistentID))
					{
						//If we were looking for this persis ID, add it even if in ignored or removed
						result.Add(grid);
						ignoredGridIds.Add(grid.persistentID);
						previousGridIDsLookingFor.Remove(grid.persistentID);
					}
					else if (grid.persistentID == -1 || (!removedGridIds.Contains(grid.persistentID) && !ignoredGridIds.Contains(grid.persistentID)))
					{
						result.Add(grid);
						ignoredGridIds.Add(grid.persistentID);
					}
				}
				removedGridIds.UnionWith(plans[i].removedGrids);
			}
		}

		return result;
	}

	public static List<EnergyGrid> GetEnergyGridsBeforePlan(Plan plan, EnergyGrid.GridColor color, bool includePlanItself = false, bool forDisplaying = false)
	{
		HashSet<int> ignoredGridIds;
		return GetEnergyGridsBeforePlan(plan, out ignoredGridIds, color, includePlanItself, forDisplaying);
	}

	public static List<EnergyGrid> GetEnergyGridsAtTime(int time, EnergyGrid.GridColor color)
	{
		if (plans.Count == 0)
		{
			return new List<EnergyGrid>(0);
		}

		for (int i = 0; i < plans.Count; i++)
			if (plans[i].StartTime > time)
				return GetEnergyGridsBeforePlan(plans[i], color);

		return GetEnergyGridsBeforePlan(plans[plans.Count - 1], color, true);
	}

	public static FishingDistributionSet GetFishingDistributionForPreviousPlan(Plan referencePlan)
	{
		FishingDistributionSet result = new FishingDistributionSet(initialFishingValues);
		foreach(Plan plan in plans)
		{
			if (plan.ID == referencePlan.ID)
			{
				break;
			}
			else
			{
				if (plan.ecologyPlan && plan.fishingDistributionDelta != null)
				{
					result.ApplyValues(plan.fishingDistributionDelta);
				}
			}
		}

		return result;
	}

	public static FishingDistributionSet GetFishingDistributionAtTime(int timeMonth)
	{
		FishingDistributionSet result = new FishingDistributionSet(initialFishingValues);
		foreach (Plan plan in plans)
		{
            if (plan.StartTime > timeMonth)
            {
                break;
            }

            if (plan.State == Plan.PlanState.IMPLEMENTED && plan.ecologyPlan && plan.fishingDistributionDelta != null)
			{
				result.ApplyValues(plan.fishingDistributionDelta);
			}
        }

		return result;
	}

	/////////////////////////////////////////
	// EVENT HANDLERS, MOSTLY FOR UI STUFF //
	/////////////////////////////////////////

	private static void PlanAdded(Plan plan)
	{
		//Add planLayers to manager, but don't add to UI individually (done in a batch by plan)
		foreach (PlanLayer planLayer in plan.PlanLayers)
			PlanLayerAdded(plan, planLayer, false);

		//Show plan if it isnt a hidden plan
		if (plan.StartTime >= 0 || TeamManager.IsGameMaster)
		{
			PlansMonitor.AddPlan(plan);
			if (plan.ShouldBeVisibleInTimeline)
			{
				SetPlanUnseenChanges(plan, true);
				PlansTimeline.AddNewPlan(plan);
			}
		}
	}

	public static void UpdatePlanInUI(Plan plan, bool nameOrDescriptionChanged, bool timeChanged, bool stateChanged, bool layersChanged, bool typeChanged, bool forceMonitorUpdate, int oldTime, Plan.PlanState oldState, bool inTimelineBefore)
	{
		bool timeLineUpdated = false;
        bool inTimelineNow = plan.ShouldBeVisibleInTimeline;

		if (nameOrDescriptionChanged)
		{
			PlanDetails.UpdateNameAndDescription(plan);
			if (planViewing == plan && !Main.InEditMode && !Main.EditingPlanDetailsContent)
				InterfaceCanvas.Instance.activePlanWindow.UpdateNameAndDate();			
		}
		if (stateChanged)
		{
			//Didn't see icon before, should see now
			if (!inTimelineBefore && inTimelineNow)
			{
				PlansTimeline.AddNewPlan(plan);
				timeLineUpdated = true;
			}
			//Saw plan before, shouldn't see now
			else if (inTimelineBefore && !inTimelineNow)
			{
				PlansTimeline.RemoveExistingPlan(plan, oldTime);
				timeLineUpdated = true;
			}
		}

		//Update edit button availability in active plan window
		if((stateChanged || layersChanged) && planViewing == plan)
			InterfaceCanvas.Instance.activePlanWindow.UpdateEditButtonActivity();

		if (timeChanged)
		{
			//Plan didnt change influencing state and should be visible to this client: update
			if (!timeLineUpdated && inTimelineNow)
				PlansTimeline.UpdatePlan(plan, oldTime);
			PlanDetails.ChangeDate(plan);
			if (planViewing == plan && !Main.InEditMode && !Main.EditingPlanDetailsContent)
			{
				InterfaceCanvas.Instance.activePlanWindow.UpdateNameAndDate();							
				InterfaceCanvas.Instance.timeBar.UpdatePlanViewing();
				LayerManager.UpdateVisibleLayersToPlan(plan);
			}
		}
		if (stateChanged || timeChanged || nameOrDescriptionChanged || forceMonitorUpdate)
		{
			PlansMonitor.UpdatePlan(plan, nameOrDescriptionChanged, timeChanged, stateChanged);
			SetPlanUnseenChanges(plan, plan.ShouldBeVisibleInUI);
		}

		//These changes don't require a general update, only plandetails if they are being viewed
		if (plan == PlanDetails.GetSelectedPlan())
		{
            if (!plan.ShouldBeVisibleInUI)
                PlanDetails.SelectPlan(null);
            else
            {
                if (stateChanged)
                    PlanDetails.UpdateStatus();
                if (typeChanged)
                    PlanDetails.UpdateTabAvailability();
            }
			PlanDetails.UpdateTabContent();
		}
	}

	public static void PlanLockUpdated(Plan plan)
	{
		PlansMonitor.SetLockIcon(plan, plan.IsLocked);
		if((Main.InEditMode && Main.CurrentlyEditingPlan == plan) || (Main.EditingPlanDetailsContent && PlanDetails.GetSelectedPlan() == plan))
		{
			PlanDetails.instance.CancelEditingContent();
			DialogBoxManager.instance.NotificationWindow("Plan Unexpectedly Unlocked", "Plan has been unlocked by an external party. All changes have been discarded.", null);
		}
	}

    public static bool UserHasPlanLocked(int sessionID)
    {
        foreach (Plan plan in plans)
            if (plan.LockedBy == sessionID)
                return true;
        return false;
    }

	public static void SetPlanUnseenChanges(Plan plan, bool unseenChanges)
	{
		if (unseenChanges)
		{
			//Check if viewing in plansdetails
			if (PlanDetails.IsOpen && PlanDetails.GetSelectedPlan() == plan)
				return;

			if(!unseenPlanChanges.Contains(plan))
				unseenPlanChanges.Add(plan);
			PlansMonitor.SetPlanUnseenChanges(plan, unseenChanges);
			PlansMonitor.SetUnseenChangesCounter(unseenPlanChanges.Count);
		}
		else
		{
			if (unseenPlanChanges.Contains(plan))
				unseenPlanChanges.Remove(plan);
			PlansMonitor.SetPlanUnseenChanges(plan, unseenChanges);
			PlansMonitor.SetUnseenChangesCounter(unseenPlanChanges.Count);
		}
	}

	public static void PlanLayerAdded(Plan plan, PlanLayer addedLayer, bool addToUI = true)
	{
		planLayers[addedLayer.ID] = addedLayer;
		IssueManager.instance.InitialiseIssuesForPlanLayer(addedLayer);
		if (addToUI)
			PlansMonitor.AddPlanLayer(plan, addedLayer);

		//Sets entities active and redraws if the layer is visible
		//LayerManager.UpdateLayerToPlan(addedLayer.BaseLayer, plan, plan == planViewing);
	}

	public static void PlanLayerRemoved(Plan plan, PlanLayer removedLayer)
	{
		PlansMonitor.RemovePlanLayer(plan, removedLayer);
		IssueManager.instance.DeleteIssuesForPlanLayer(removedLayer);
		//HidePlanLayer(removedLayer);
		RemovePlanLayer(removedLayer);
	}

	public static void LoadFishingFleets(JObject melConfig)
	{
		fishingFleets = new List<string>();
		try
		{
			JEnumerable<JToken> results = melConfig["fishing"].Children();
			foreach (JToken token in results)
				fishingFleets.Add(token.ToObject<FishingFleet>().name);
			initialFishingMapping = melConfig["initialFishingMapping"].ToObject<float>();
			fishingDisplayScale = melConfig["fishingDisplayScale"].ToObject<float>();
		}
		catch
		{
			Debug.Log("Fishing fleets json does not match expected format.");
		}
		
		//We can only start loading the fishing values when the fleets have been loaded.
		LoadInitialFishingValues();
	}

	private static void LoadInitialFishingValues()
	{
		NetworkForm form = new NetworkForm();
		ServerCommunication.DoRequest<List<FishingObject>>(Server.GetInitialFishingValues(), form, LoadInitialFishingValuesCallback);
	}

	private static void LoadInitialFishingValuesCallback(List<FishingObject> fishing)
	{
		initialFishingValues = new FishingDistributionDelta(fishing);
	}

    public static void ViewPlanWithIDWhenReceived(int targetPlanID)
    {
        bool found = false;
        foreach (Plan plan in plans)
        {
            if (plan.ID == targetPlanID)
            {
                found = true;
                ShowPlan(plan);
                PlanDetails.SelectPlan(plan);
            }
        }

        if(!found)
            planToViewOnUpdate = targetPlanID;
    }

    public static void CheckIfExpectedplanReceived()
    {
        if (planToViewOnUpdate == -1)
            return;

        foreach (Plan plan in plans)
        {
            if (plan.ID == planToViewOnUpdate)
            {
                planToViewOnUpdate = -1;
                ShowPlan(plan);
                PlanDetails.SelectPlan(plan);
            }
        }
    }

	//public static List<Plan> DetermineFuturePlanEnergyOverlap(Plan plan)
	//{
	//	List<Plan> result = new List<Plan>();
	//	int planIndex = 0;
	//	for (; planIndex < plans.Count; planIndex++)
	//		if (plans[planIndex].ID == plan.ID)
	//			break;

	//	planIndex++;
	//	for (; planIndex < plans.Count; planIndex++)
	//	{
	//		bool matchFound = false;
	//		foreach (EnergyGrid grid in plans[planIndex].energyGrids)
	//		{
	//			if (plan.removedGrids.Contains(grid.persistentID))
	//			{
	//				matchFound = true;
	//				break;
	//			}
	//		}
	//		if (!matchFound && plan.removedGrids.Overlaps(plans[planIndex].removedGrids))
	//		{
	//			matchFound = true;
	//		}
	//		if (matchFound)
	//			result.Add(plans[planIndex]);
	//	}

	//	return result;
	//}

	//public static List<Plan> DetermineFuturePlanEnergyDependency(Plan plan)
	//{
	//	List<Plan> result = new List<Plan>();
	//	//For all grids we changed the geometry of, invalidate later instances of that plan
	//	int planIndex = 0;
	//	for (; planIndex < plans.Count; planIndex++)
	//		if (plans[planIndex].ID == plan.ID)
	//			break;

	//	//For every grid
	//	foreach (EnergyGrid grid in plan.energyGrids)
	//	{
	//		if (grid.distributionOnly)
	//			continue;
	//		//For every future plan
	//		for (int i = planIndex + 1; i < plans.Count; i++)
	//		{
	//			//If grids overlap, put error in plan
	//			bool matchFound = plans[i].removedGrids.Contains(grid.persistentID);
	//			if (!matchFound)
	//			{
	//				foreach (EnergyGrid otherGrid in plans[i].energyGrids)
	//				{
	//					if (otherGrid.persistentID == grid.persistentID)
	//					{
	//						matchFound = true;
	//						break;
	//					}
	//				}
	//			}
	//			if (matchFound)
	//				result.Add(plans[i]);
	//		}
	//	}
	//	return result;
	//}
}

public class FishingFleet
{
	public string name;
	public float scalar;
}