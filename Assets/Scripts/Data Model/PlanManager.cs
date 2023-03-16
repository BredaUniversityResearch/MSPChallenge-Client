using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

namespace MSP2050.Scripts
{
	public class PlanManager : MonoBehaviour
	{
		public enum PlanViewState { All, Base, Changes, Time };

		private static PlanManager Singleton;
		public static PlanManager Instance
		{
			get
			{
				if (Singleton == null)
					Singleton = FindObjectOfType<PlanManager>();
				return Singleton;
			}
		}

		private List<Plan> m_plans = new List<Plan>();
		public List<Plan> Plans => m_plans;

		public delegate void PlansEventDelegate(Plan a_plan);
		public delegate void PlansUpdateEventDelegate(Plan a_plan, int a_oldTime);
		public event PlansEventDelegate OnPlanVisibleInUIEvent;
		public event PlansUpdateEventDelegate OnPlanUpdateInUIEvent;
		public event PlansUpdateEventDelegate OnPlanHideInUIEvent;
		public event PlansEventDelegate OnViewingPlanChanged;

		//Viewing & Viewstates
		[FormerlySerializedAs("planViewState")]
		[HideInInspector] public PlanViewState m_planViewState = PlanViewState.All;
		public Plan m_planViewing;
		[FormerlySerializedAs("timeViewing")]
		[HideInInspector] public int m_timeViewing = -1; //Used if planViewing is null. -1 is current time.
		private bool m_ignoreRedrawOnViewStateChange = false;

		private void Start()
		{
			if (Singleton != null && Singleton != this)
				Destroy(this);
			else
				Singleton = this;
		}

		private void OnDestroy()
		{
			Singleton = null;
		}

		public Plan ProcessReceivedPlan(PlanObject a_planObject, Dictionary<AbstractLayer, int> a_layerUpdateTimes)
		{
			int planID = a_planObject.id;
			Plan targetPlan = GetPlanWithID(planID);

			if (targetPlan != null)
			{
				targetPlan.UpdatePlan(a_planObject, a_layerUpdateTimes);
				if(!InterfaceCanvas.Instance.plansList.ContainsPlan(targetPlan))
					AddPlanToUI(targetPlan);
			}
			else
			{
				targetPlan = new Plan(a_planObject, a_layerUpdateTimes);
				AddPlan(targetPlan);
				AddPlanToUI(targetPlan);
			}

			return targetPlan;
		}

		public void AddPlan(Plan a_newPlan)
		{
			if (m_plans.Count == 0)
			{
				m_plans.Add(a_newPlan);
				return;
			}

			for (int i = 0; i < m_plans.Count; i++)
				if (m_plans[i].StartTime > a_newPlan.StartTime)
				{
					m_plans.Insert(i, a_newPlan);
					return;
				}

			m_plans.Add(a_newPlan);
		}

		public void RemovePlan(Plan a_plan)
		{
			m_plans.Remove(a_plan);
		}

		public void UpdatePlanTime(Plan a_updatedPlan)
		{
			m_plans.Remove(a_updatedPlan);
			AddPlan(a_updatedPlan);
		}

		public void SetPlanViewState(PlanViewState a_state, bool a_redraw = true)
		{
			bool needsRedraw = a_redraw && (!m_ignoreRedrawOnViewStateChange && m_planViewState != a_state);
			m_planViewState = a_state;
			if (needsRedraw)
				LayerManager.Instance.RedrawVisibleLayers();
		}

		public void ShowWorldAt(int a_time)
		{
			if (m_timeViewing == a_time || m_planViewing != null)
				return;
			if (a_time == -1)
				LayerManager.Instance.UpdateVisibleLayersToBase();
			else
				LayerManager.Instance.UpdateVisibleLayersToTime(a_time);
			m_timeViewing = a_time;
		}

		public void ShowPlan(Plan a_plan)
		{
			if (Main.InEditMode)
				return;
			
			if (a_plan.State == Plan.PlanState.DELETED)
			{
				//Ask if player wants to return plan to design (and change time if required)
				Plan targetPlan = a_plan; //cache plan for callbacks
				if(a_plan.RequiresTimeChange)
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
				m_planViewing = a_plan;
				m_timeViewing = -1;
				InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Plan, false);//Needs to be done before redraw
				LayerManager.Instance.UpdateVisibleLayersToPlan(a_plan);
				InterfaceCanvas.Instance.activePlanWindow.SetToPlan(a_plan);
				IssueManager.Instance.SetIssueInstancesToPlan(a_plan);
				if (OnViewingPlanChanged != null)
				{
					OnViewingPlanChanged.Invoke(a_plan);
				}
			}
		}

		void ShowArchivedPlan(Plan a_plan)
		{
			InterfaceCanvas.ShowNetworkingBlocker();
			a_plan.AttemptLock((a_lockedPlan) =>
			{
				if (a_lockedPlan.RequiresTimeChange)
				{
					InterfaceCanvas.HideNetworkingBlocker();
					m_planViewing = a_lockedPlan;
					m_timeViewing = -1;
					InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Normal, false);//Needs to be done before redraw
					LayerManager.Instance.UpdateVisibleLayersToBase();
					InterfaceCanvas.Instance.activePlanWindow.SetToPlan(a_lockedPlan);
					if (OnViewingPlanChanged != null)
					{
						OnViewingPlanChanged.Invoke(a_lockedPlan);
					}
				}
				else
				{
					AP_StateSelect.SubmitPlanRecovery(a_lockedPlan);
					if (OnViewingPlanChanged != null)
					{
						OnViewingPlanChanged.Invoke(null);
					}
				}
			}, null);
		}

		public void HideCurrentPlan(bool a_updateLayers = true)
		{
			if (Main.InEditMode)
				return;

			m_planViewing = null;

			//Doesnt have to redraw as we'll do so when updating layers to base anyway
			m_ignoreRedrawOnViewStateChange = true;
			TimeBar.instance.SetViewMode(PlanViewState.All);
			m_ignoreRedrawOnViewStateChange = false;

			if (a_updateLayers)
				LayerManager.Instance.UpdateVisibleLayersToBase();
			InterfaceCanvas.Instance.activePlanWindow.CloseWindow();
			InterfaceCanvas.Instance.timeBar.SetViewMode(TimeBar.WorldViewMode.Normal, false);
			IssueManager.Instance.HidePlanIssueInstances();
			if (OnViewingPlanChanged != null)
			{
				OnViewingPlanChanged.Invoke(null);
			}
		}

		public SubEntityPlanState GetSubEntityPlanState(SubEntity a_subEntity)
		{
			//added, moved, removed, notinplan, notshown
			PlanLayer currentPlanLayer = a_subEntity.m_entity.Layer.CurrentPlanLayer();
			bool layerInPlan = m_planViewing == null || m_planViewing.IsLayerpartOfPlan(a_subEntity.m_entity.Layer);
			if(!a_subEntity.m_entity.Layer.Toggleable || (!a_subEntity.m_entity.Layer.m_editable && a_subEntity.m_entity.Layer.ActiveOnStart))
				return SubEntityPlanState.NotInPlan;

			if (m_planViewState == PlanViewState.All)
			{
				if (currentPlanLayer == null) //Only show the base layer
				{
					if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
						return SubEntityPlanState.NotInPlan;
					return SubEntityPlanState.NotShown;
				}
				if (!layerInPlan)
				{
					if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
						return SubEntityPlanState.NotInPlan;
					return SubEntityPlanState.NotShown;
				}
				if (currentPlanLayer.IsDatabaseIDInNewGeometry(a_subEntity.GetDatabaseID()))
				{
					if (!currentPlanLayer.BaseLayer.IsEntityTypeVisible(a_subEntity.m_entity.EntityTypes))
						return SubEntityPlanState.NotShown;
					if (a_subEntity.m_entity.Layer.IsPersisIDCurrentlyNew(a_subEntity.GetPersistentID()))
						return SubEntityPlanState.Added;
					return SubEntityPlanState.Moved;
				}
				if (!a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotShown;
				if (currentPlanLayer.IsPersistentIDInRemovedGeometry(a_subEntity.GetPersistentID()))
					return SubEntityPlanState.Removed;
				return SubEntityPlanState.NotInPlan;
			}
			if (m_planViewState == PlanViewState.Base)
			{
				if (currentPlanLayer == null) //Only show the base layer
				{
					if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
						return SubEntityPlanState.NotInPlan;
					return SubEntityPlanState.NotShown;
				}
				if (!layerInPlan)
				{
					if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
						return SubEntityPlanState.NotInPlan;
					return SubEntityPlanState.NotShown;
				}
				if (currentPlanLayer.IsPersistentIDInRemovedGeometry(a_subEntity.GetPersistentID()) || a_subEntity.m_entity.Layer.IsDatabaseIDPreModified(a_subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
				if (a_subEntity.m_entity.Layer.IsPersisIDCurrentlyNew(a_subEntity.GetPersistentID()) || currentPlanLayer.IsDatabaseIDInNewGeometry(a_subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotShown;
				if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
			}
			else if (m_planViewState == PlanViewState.Changes)
			{
				if (currentPlanLayer == null) //Only show the base layer
					return SubEntityPlanState.NotShown;
				if (!layerInPlan)
					return SubEntityPlanState.NotShown;
				if (currentPlanLayer.IsDatabaseIDInNewGeometry(a_subEntity.GetDatabaseID()))
				{
					if (!currentPlanLayer.BaseLayer.IsEntityTypeVisible(a_subEntity.m_entity.EntityTypes))
						return SubEntityPlanState.NotShown;
					if (a_subEntity.m_entity.Layer.IsPersisIDCurrentlyNew(a_subEntity.GetPersistentID()))
						return SubEntityPlanState.Added;
					return SubEntityPlanState.Moved;
				}
				if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()) && currentPlanLayer.IsPersistentIDInRemovedGeometry(a_subEntity.GetPersistentID()))
					return SubEntityPlanState.Removed;
			}
			else //PlanViewState.Time
			{
				if (a_subEntity.m_entity.Layer.IsIDInActiveGeometry(a_subEntity.GetDatabaseID()))
					return SubEntityPlanState.NotInPlan;
				return SubEntityPlanState.NotShown;
			}
			return SubEntityPlanState.NotShown;
		}

		public int GetPlanCount()
		{
			return m_plans.Count;
		}

		public Plan GetPlanAtIndex(int a_index)
		{
			return m_plans[a_index];
		}

		public Plan GetPlanWithID(int a_planID)
		{
			foreach (Plan plan in m_plans)
			{
				if (plan.ID == a_planID)
				{
					return plan;
				}
			}
			return null;
		}

		public List<Plan> GetAllPlansFrom(int a_month)
		{
			List<Plan> result = new List<Plan>();
			for (int i = m_plans.Count - 1; i >= 0; i--)
			{
				if (m_plans[i].StartTime < a_month)
					break;
				result.Add(m_plans[i]);
			}
			return result;
		}

		/// <summary>
		/// Returns plan layers for a base layer from a specific month onwards
		/// </summary>
		/// <param name="a_baseLayer">The base layer we need to get the geometry from</param>
		/// <param name="a_planStartTime">Exclusive from what date on we want to get the layers</param>
		/// <param name="a_onlyInfluencingPlans">Only plans that are in the influencing state</param>
		/// <returns></returns>
		public List<PlanLayer> GetPlanLayersForBaseLayerFrom(AbstractLayer a_baseLayer, int a_planStartTime, bool a_onlyInfluencingPlans)
		{
			List<PlanLayer> result = new List<PlanLayer>(32);
			//Iterate forwards so the list is in order from first occuring layer to last occuring layer. This helps us with checks in the future
			for (int i = 0; i < m_plans.Count; ++i)
			{
				Plan plan = m_plans[i];
				if (plan.StartTime <= a_planStartTime ||
					(a_onlyInfluencingPlans && !plan.InInfluencingState))
				{
					continue;
				}

				PlanLayer planLayer = plan.GetPlanLayerForLayer(a_baseLayer);
				if (planLayer != null)
				{
					result.Add(planLayer);
				}
			}
			return result;
		}

		/// <summary>
		/// Called whenever a new month starts
		/// </summary>
		/// <param name="a_newMonth">month that just started</param>
		public static void MonthTick(int a_newMonth)
		{
			//Advance time on layers (merging approved ones) 
			foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
				layer.AdvanceTimeTo(a_newMonth);
		}

		private void AddPlanToUI(Plan a_plan)
		{
			//Show plan if it isnt a hidden plan
			if (a_plan.StartTime < 0 && !SessionManager.Instance.AreWeGameMaster)
				return;
			InterfaceCanvas.Instance.plansList.AddPlanToList(a_plan);
			if (a_plan.ShouldBeVisibleInTimeline)
			{
				OnPlanVisibleInUIEvent(a_plan);
			}
		}

		public void OnPlanInfluencingChanged(Plan a_plan, bool a_nowInfluencing)
		{
			//if editing, check if edited plan affected by this change, redo energy backup and distr
			if(Main.InEditMode && a_plan.StartTime <= Main.CurrentlyEditingPlan.StartTime)
			{
				InterfaceCanvas.Instance.activePlanWindow.OnPreviousPlanChangedInfluence();
			}
		}

		public void UpdatePlanInUI(Plan a_plan, bool a_stateChanged, int a_oldTime, bool a_inTimelineBefore)
		{
			bool inTimelineNow = a_plan.ShouldBeVisibleInTimeline;

			if (a_stateChanged)
			{
				//Didn't see icon before, should see now
				if (!a_inTimelineBefore && inTimelineNow)
				{
					OnPlanVisibleInUIEvent(a_plan);
				}
				//Saw plan before, shouldn't see now
				else if (a_inTimelineBefore && !inTimelineNow)
				{
					OnPlanHideInUIEvent(a_plan, a_oldTime);
				}
			}

			//Update edit button availability in active plan window
			if (m_planViewing == a_plan && !Main.InEditMode)
			{
				InterfaceCanvas.Instance.activePlanWindow.RefreshContent();
				if (!a_plan.ShouldBeVisibleInUI)
				{
					HideCurrentPlan();
				}
				InterfaceCanvas.Instance.timeBar.UpdatePlanViewing();
				LayerManager.Instance.UpdateVisibleLayersToPlan(a_plan);
			}

			InterfaceCanvas.Instance.plansList.UpdatePlan(a_plan);
			OnPlanUpdateInUIEvent(a_plan, a_oldTime);
		}

		public bool UserHasPlanLocked(int a_sessionID)
		{
			foreach (Plan plan in m_plans)
				if (plan.LockedBy == a_sessionID)
					return true;
			return false;
		}

		public void BeginPlanCreation()
		{
			if (Main.InEditMode || Main.Instance.PreventPlanChange)
				return;
			if(m_planViewing != null)
				HideCurrentPlan();
			InterfaceCanvas.Instance.activePlanWindow.SetToPlan(null);
		}

		public void ForceSetPlanViewing(Plan a_plan)
		{
			m_planViewing = a_plan;
			m_timeViewing = -1;
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