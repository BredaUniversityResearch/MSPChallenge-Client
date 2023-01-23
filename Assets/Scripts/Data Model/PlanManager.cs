using System.Collections.Generic;
using UnityEngine;
using System;

namespace MSP2050.Scripts
{
	public class PlanManager : MonoBehaviour
	{
		public enum PlanViewState { All, Base, Changes, Time };

		private static PlanManager singleton;
		public static PlanManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<PlanManager>();
				return singleton;
			}
		}

		private List<Plan> plans = new List<Plan>();
		public List<Plan> Plans => plans;

		public delegate void PlansEventDelegate(Plan plan);
		public delegate void PlansUpdateEventDelegate(Plan plan, int oldTime);
		public event PlansEventDelegate OnPlanVisibleInUIEvent;
		public event PlansUpdateEventDelegate OnPlanUpdateInUIEvent;
		public event PlansUpdateEventDelegate OnPlanHideInUIEvent;
		public event PlansEventDelegate OnViewingPlanChanged;

		//Viewing & Viewstates
		[HideInInspector] public PlanViewState planViewState = PlanViewState.All;
		[HideInInspector] public Plan planViewing;
		[HideInInspector] public int timeViewing = -1; //Used if planViewing is null. -1 is current time.
		private bool ignoreRedrawOnViewStateChange = false;

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
		}

		void OnDestroy()
		{
			singleton = null;
		}

		public Plan ProcessReceivedPlan(PlanObject planObject, Dictionary<AbstractLayer, int> layerUpdateTimes)
		{
			int planID = planObject.id;
			Plan targetPlan = GetPlanWithID(planID);

			if (targetPlan != null)
			{
				targetPlan.UpdatePlan(planObject, layerUpdateTimes);
				if(!InterfaceCanvas.Instance.plansList.ContainsPlan(targetPlan))
					AddPlanToUI(targetPlan);
			}
			else
			{
				targetPlan = new Plan(planObject, layerUpdateTimes);
				AddPlan(targetPlan);
				AddPlanToUI(targetPlan);
			}

			return targetPlan;
		}

		public void AddPlan(Plan newPlan)
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

		public void RemovePlan(Plan a_plan)
		{
			plans.Remove(a_plan);
		}

		public void UpdatePlanTime(Plan updatedPlan)
		{
			plans.Remove(updatedPlan);
			AddPlan(updatedPlan);
		}

		public void SetPlanViewState(PlanViewState state, bool redraw = true)
		{
			bool needsRedraw = redraw && (!ignoreRedrawOnViewStateChange && planViewState != state);
			planViewState = state;
			if (needsRedraw)
				LayerManager.Instance.RedrawVisibleLayers();
		}

		public void ShowWorldAt(int time)
		{
			if (timeViewing == time || planViewing != null)
				return;
			if (time == -1)
				LayerManager.Instance.UpdateVisibleLayersToBase();
			else
				LayerManager.Instance.UpdateVisibleLayersToTime(time);
			timeViewing = time;
		}

		public void ShowPlan(Plan plan)
		{
			if (Main.InEditMode)
				return;


			if (plan.State == Plan.PlanState.DELETED)
			{
				//Ask if player wants to return plan to design (and change time if required)
				Plan targetPlan = plan; //cache plan for callbacks
				if(plan.RequiresTimeChange)
				{
					DialogBoxManager.instance.NotificationWindow("Restore archived plan",
						"The selected plan has been archived and its construction start time has passed. To restore the plan, change its implementation date.",
						() => ShowArchivedPlan(targetPlan));
				}
				else
				{
					DialogBoxManager.instance.ConfirmationWindow("Restore archived plan",
						"The selected plan has been archived. Would you like to restore the plan to the Design state?",
						() => { }, () => ShowArchivedPlan(targetPlan));
				}
			}
			else
			{
				//InterfaceCanvas.Instance.viewTimeWindow.CloseWindow(false);
				planViewing = plan;
				timeViewing = -1;
				InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Plan, false);//Needs to be done before redraw
				LayerManager.Instance.UpdateVisibleLayersToPlan(plan);
				InterfaceCanvas.Instance.activePlanWindow.SetToPlan(plan);
				IssueManager.Instance.SetIssueInstancesToPlan(plan);
				if (OnViewingPlanChanged != null)
				{
					OnViewingPlanChanged.Invoke(plan);
				}
			}
		}

		void ShowArchivedPlan(Plan a_plan)
		{
			InterfaceCanvas.ShowNetworkingBlocker();
			a_plan.AttemptLock((lockedPlan) =>
			{
				if (lockedPlan.RequiresTimeChange)
				{
					InterfaceCanvas.HideNetworkingBlocker();
					planViewing = lockedPlan;
					timeViewing = -1;
					InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Normal, false);//Needs to be done before redraw
					LayerManager.Instance.UpdateVisibleLayersToBase();
					InterfaceCanvas.Instance.activePlanWindow.SetToPlan(lockedPlan);
					if (OnViewingPlanChanged != null)
					{
						OnViewingPlanChanged.Invoke(lockedPlan);
					}
				}
				else
				{
					AP_StateSelect.SubmitPlanRecovery(lockedPlan);
				}
			}, null);
		}

		public void HideCurrentPlan(bool updateLayers = true)
		{
			if (Main.InEditMode)
				return;

			planViewing = null;

			//Doesnt have to redraw as we'll do so when updating layers to base anyway
			ignoreRedrawOnViewStateChange = true;
			InterfaceCanvas.Instance.activePlanWindow.SetViewMode(PlanManager.PlanViewState.All);
			ignoreRedrawOnViewStateChange = false;

			if (updateLayers)
				LayerManager.Instance.UpdateVisibleLayersToBase();
			InterfaceCanvas.Instance.activePlanWindow.CloseWindow();
			InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Normal, false);
			IssueManager.Instance.HidePlanIssueInstances();
			if (OnViewingPlanChanged != null)
			{
				OnViewingPlanChanged.Invoke(null);
			}
		}

		public SubEntityPlanState GetSubEntityPlanState(SubEntity subEntity)
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

		public void ViewPlanOnMap(Plan plan)
		{
			foreach (PlanLayer planLayer in plan.PlanLayers)
				LayerManager.Instance.ShowLayer(planLayer.BaseLayer);

			CameraManager.Instance.ZoomToBounds(plan.GetBounds());
		}

		public void ViewPlanLayerOnMap(PlanLayer planLayer)
		{
			LayerManager.Instance.ShowLayer(planLayer.BaseLayer);
			CameraManager.Instance.ZoomToBounds(planLayer.GetBounds());
		}

		public int GetPlanCount()
		{
			return plans.Count;
		}

		public Plan GetPlanAtIndex(int index)
		{
			return plans[index];
		}

		public Plan GetPlanWithID(int planID)
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

		public List<Plan> GetAllPlansFrom(int month)
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
		public List<PlanLayer> GetPlanLayersForBaseLayerFrom(AbstractLayer baseLayer, int planStartTime, bool onlyInfluencingPlans)
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

		public Plan FindFirstPlanChangingGeometry(int fromMonth, int entityPersistentId, AbstractLayer baseLayer)
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


		/// <summary>
		/// Called whenever a new month starts
		/// </summary>
		/// <param name="newMonth">month that just started</param>
		public void MonthTick(int newMonth)
		{
			//Advance time on layers (merging approved ones) 
			foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
				layer.AdvanceTimeTo(newMonth);
		}


		/////////////////////////////////////////
		// EVENT HANDLERS, MOSTLY FOR UI STUFF //
		/////////////////////////////////////////

		private void AddPlanToUI(Plan plan)
		{
			//Show plan if it isnt a hidden plan
			if (plan.StartTime >= 0 || SessionManager.Instance.AreWeGameMaster)
			{
				InterfaceCanvas.Instance.plansList.AddPlanToList(plan);
				if (plan.ShouldBeVisibleInTimeline)
				{
					OnPlanVisibleInUIEvent(plan);
				}
			}
		}

		public void UpdatePlanInUI(Plan plan, bool stateChanged, int oldTime, bool inTimelineBefore)
		{
			bool inTimelineNow = plan.ShouldBeVisibleInTimeline;

			if (stateChanged)
			{
				//Didn't see icon before, should see now
				if (!inTimelineBefore && inTimelineNow)
				{
					OnPlanVisibleInUIEvent(plan);
				}
				//Saw plan before, shouldn't see now
				else if (inTimelineBefore && !inTimelineNow)
				{
					OnPlanHideInUIEvent(plan, oldTime);
				}
			}

			//Update edit button availability in active plan window
			if (planViewing == plan && !Main.InEditMode)
			{
				InterfaceCanvas.Instance.activePlanWindow.RefreshContent();
				if (!plan.ShouldBeVisibleInUI)
				{
					HideCurrentPlan();
				}
				InterfaceCanvas.Instance.timeBar.UpdatePlanViewing();
				LayerManager.Instance.UpdateVisibleLayersToPlan(plan);
			}

			InterfaceCanvas.Instance.plansList.UpdatePlan(plan);
			OnPlanUpdateInUIEvent(plan, oldTime);
		}

		public bool UserHasPlanLocked(int sessionID)
		{
			foreach (Plan plan in plans)
				if (plan.LockedBy == sessionID)
					return true;
			return false;
		}

		public void BeginPlanCreation()
		{
			if (Main.InEditMode || Main.Instance.PreventPlanChange)
				return;
			if(planViewing != null)
				HideCurrentPlan();
			InterfaceCanvas.Instance.activePlanWindow.SetToPlan(null);
		}

		public void ForceSetPlanViewing(Plan a_plan)
		{
			planViewing = a_plan;
			timeViewing = -1;
			InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Plan, false);//Needs to be done before redraw
			LayerManager.Instance.UpdateVisibleLayersToPlan(a_plan);
			IssueManager.Instance.SetIssueInstancesToPlan(a_plan);
			if(OnViewingPlanChanged != null)
			{
				OnViewingPlanChanged.Invoke(a_plan);
			}
		}
		
	}
}