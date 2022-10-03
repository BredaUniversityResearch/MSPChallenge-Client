using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reactive.Joins;

namespace MSP2050.Scripts
{
	public class PolicyLogicEnergy : APolicyLogic
	{
		public override void Initialise(APolicyData a_settings)
		{ }

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan)
		{
			PolicyUpdateEnergyPlan updateData = (PolicyUpdateEnergyPlan)a_data;
			//Grids are handled later
			if (a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(updateData.policy_type, out PolicyPlanDataEnergy planData))
			{
				planData.energyError = updateData.energy_error == "1";
				planData.altersEnergyDistribution = updateData.alters_energy_distribution;
			}
			else
			{
				a_plan.AddPolicyData(new PolicyPlanDataEnergy()
				{
					policy_type = updateData.policy_type,
					altersEnergyDistribution = updateData.alters_energy_distribution,
					energyError = updateData.energy_error == "1"
				});
			}
		}

		public override void HandlePreKPIUpdate(APolicyData a_data)
		{
			//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
			foreach (EnergyOutputObject outputUpdate in a_Update.energy.output)
			{
				UpdateOutput(outputUpdate);
			}

			//Update grids
			if (a_Update.plan != null)
			{
				for (int i = 0; i < plans.Count; i++)
				{
					plans[i].UpdateGrids(a_Update.plan[i].deleted_grids, a_Update.plan[i].grids);
				}
			}

			//Run connection update before KPI update so cable networks are accurate in the KPIs
			foreach (EnergyConnectionObject connection in a_Update.energy.connections)
			{
				UpdateConnection(connection);
			}
		}

		public override void HandlePostKPIUpdate(APolicyData a_data)
		{
		}

		public override APolicyData FormatPlanData(Plan a_plan)
		{
			return null;
		}

		public override void UpdateAfterEditing(Plan a_plan)
		{ }

		public override bool HasError(APolicyData a_data)
		{
			return ((PolicyPlanDataEnergy)a_data).energyError;
		}

		public void UpdateGrids(APolicyData a_planData, Plan a_plan, HashSet<int> a_deleted, List<GridObject> a_newGrids)
		{
			//TODO: call this
			PolicyPlanDataEnergy planData = (PolicyPlanDataEnergy)a_planData;
			//Don't update grids if we have the plan locked. Prevents updates while editing.
			if (!a_plan.IsLockedByUs)
			{
				planData.removedGrids = a_deleted;
				planData.energyGrids = new List<EnergyGrid>();
				foreach (GridObject obj in a_newGrids)
					planData.energyGrids.Add(new EnergyGrid(obj, a_plan));
			}
		}

		private void UpdateOutput(EnergyOutputObject outputUpdate)
		{
			SubEntity tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(outputUpdate.id);
			if (tempSubEnt == null) return;
			IEnergyDataHolder energyObj = (IEnergyDataHolder)tempSubEnt;
			energyObj.UsedCapacity = outputUpdate.capacity;
			energyObj.Capacity = outputUpdate.maxcapacity;
			tempSubEnt.UpdateTextMeshText();
		}

		private void UpdateConnection(EnergyConnectionObject connection)
		{
			if (connection.active == "0")
				return;

			int startID = Util.ParseToInt(connection.start);
			int endID = Util.ParseToInt(connection.end);
			int cableID = Util.ParseToInt(connection.cable);
			string[] temp = connection.coords.Split(',');
			Vector3 firstCoord = new Vector2(Util.ParseToFloat(temp[0].Substring(1)), Util.ParseToFloat(temp[1].Substring(0, temp[1].Length - 1)));

			EnergyPointSubEntity point1;
			EnergyPointSubEntity point2;
			SubEntity tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(cableID);
			if (tempSubEnt == null) return;
			EnergyLineStringSubEntity cable = tempSubEnt as EnergyLineStringSubEntity;

			//Get the points, check if they reference to a polygon or point
			tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(startID);
			if (tempSubEnt == null) return;
			else if (tempSubEnt is EnergyPolygonSubEntity)
				point1 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
			else
				point1 = tempSubEnt as EnergyPointSubEntity;

			tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(endID);
			if (tempSubEnt == null) return;
			else if (tempSubEnt is EnergyPolygonSubEntity)
				point2 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
			else
				point2 = tempSubEnt as EnergyPointSubEntity;

			Connection conn1 = new Connection(cable, point1, true);
			Connection conn2 = new Connection(cable, point2, false);

			//Cables store connections and attach them to points when editing starts
			cable.AddConnection(conn1);
			cable.AddConnection(conn2);
			cable.SetEndPointsToConnections();
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.m_policies.Remove(PolicyManager.ENERGY_POLICY_NAME);
		}

		/// <summary>
		/// Duplicates (value copies) the given energy grid and adds it to this plan.
		/// </summary>
		public static EnergyGrid DuplicateEnergyGridToPlan(EnergyGrid a_gridToDuplicate, Plan a_plan)
		{
			EnergyGrid duplicate = new EnergyGrid(a_gridToDuplicate, a_plan);
			duplicate.distributionOnly = true;

			if (a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var planData))
			{
				planData.energyGrids.Add(duplicate);
			}
			else
			{
				Debug.LogError("Failed to duplicate grid to plan without energy data");
			}
			return duplicate;
		}

		public override void GetRequiredApproval(APolicyData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel)
		{
			if (a_requiredApprovalLevel < EApprovalType.AllCountries)
			{
				PolicyPlanDataEnergy planData = (PolicyPlanDataEnergy)a_planData;

				//Determine countries affected by removed grids
				List<EnergyGrid> energyGridsBeforePlan = PlanManager.Instance.GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);

				HashSet<int> countriesAffectedByRemovedGrids = new HashSet<int>();
				foreach (EnergyGrid grid in energyGridsBeforePlan)
					if (planData.removedGrids.Contains(grid.persistentID))
						foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.energyDistribution.distribution)
							if (!countriesAffectedByRemovedGrids.Contains(countryAmount.Key))
								countriesAffectedByRemovedGrids.Add(countryAmount.Key);

				//Removed grids
				if (countriesAffectedByRemovedGrids != null)
				{
					foreach (int i in countriesAffectedByRemovedGrids)
					{
						if (!a_approvalStates.ContainsKey(i))
						{
							a_approvalStates.Add(i, EPlanApprovalState.Maybe);
						}
					}
				}

				//Added grids
				if (planData.energyGrids != null)
				{
					foreach (EnergyGrid grid in planData.energyGrids)
					{
						foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.energyDistribution.distribution)
						{
							if (!a_approvalStates.ContainsKey(countryAmount.Key))
							{
								a_approvalStates.Add(countryAmount.Key, EPlanApprovalState.Maybe);
							}
						}
					}
				}
			}
		}


		/// <summary>
		/// Returns a list of energy grids that are active right before the given plan would be implemented.
		/// </summary>
		/// <param name="plan"> Plan before which grids are calced </param>
		/// <param name="removedGridIds"> Persistent IDs of grids that have been removed in at this plan's point</param>
		/// <param name="includePlanItself"> Is the given plan included </param>
		/// <param name="forDisplaying"> Is the given plan included even if it's in design</param>
		/// <returns></returns>
		public List<EnergyGrid> GetEnergyGridsBeforePlan(Plan plan, out HashSet<int> removedGridIds, EnergyGrid.GridColor color, bool includePlanItself = false, bool forDisplaying = false)
		{
			List<Plan> plans = PlanManager.Instance.Plans;

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

		public List<EnergyGrid> GetEnergyGridsBeforePlan(Plan plan, EnergyGrid.GridColor color, bool includePlanItself = false, bool forDisplaying = false)
		{
			HashSet<int> ignoredGridIds;
			return GetEnergyGridsBeforePlan(plan, out ignoredGridIds, color, includePlanItself, forDisplaying);
		}

		public List<EnergyGrid> GetEnergyGridsAtTime(int time, EnergyGrid.GridColor color)
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
	}
}