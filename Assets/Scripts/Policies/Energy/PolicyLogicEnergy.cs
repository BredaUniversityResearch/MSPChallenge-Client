using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reactive.Joins;

namespace MSP2050.Scripts
{
	public class PolicyLogicEnergy : APolicyLogic
	{
		static PolicyLogicEnergy m_instance;
		public static PolicyLogicEnergy Instance => m_instance;

		private Dictionary<int, EnergyGrid> energyGrids = new Dictionary<int, EnergyGrid>();
		private List<PointLayer> energyPointLayers = new List<PointLayer>();
		public LineStringLayer energyCableLayerGreen;
		public LineStringLayer energyCableLayerGrey;
		public List<AbstractLayer> energyLayers = new List<AbstractLayer>(); //Does not include sourcepolygonpoints
		public Dictionary<int, int> sourceCountries = new Dictionary<int, int>();
		public Dictionary<int, SubEntity> energySubEntities;

		public override void Initialise(APolicyData a_settings)
		{
			m_instance = this;
		}
		public override void Destroy()
		{
			m_instance = null;
		}

		public override void HandlePlanUpdate(APolicyData a_planUpdateData, Plan a_plan, EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				PolicyUpdateEnergyPlan updateData = (PolicyUpdateEnergyPlan)a_planUpdateData;
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
			else if (a_stage == APolicyLogic.EPolicyUpdateStage.PreKPI)
			{
				PolicyUpdateEnergyPlan updateData = (PolicyUpdateEnergyPlan)a_planUpdateData;
				UpdateGrids(a_plan, updateData.deleted_grids, updateData.grids);
			}
		}

		public override void HandleGeneralUpdate(APolicyData a_updateData, EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.PreKPI)
			{
				PolicyUpdateEnergy updateData = (PolicyUpdateEnergy)a_updateData;
				//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
				foreach (EnergyOutputObject outputUpdate in updateData.output)
				{
					UpdateOutput(outputUpdate);
				}
				//Run connection update before KPI update so cable networks are accurate in the KPIs
				//TODO: This used to be after grid updates, check if this causes issues
				foreach (EnergyConnectionObject connection in updateData.connections)
				{
					UpdateConnection(connection);
				}
			}
		}

		public override APolicyData FormatPlanData(Plan a_plan)
		{
			//TODO
			return null;
		}

		public override bool FormatGeneralData(out APolicyData a_data)
		{
			//TODO
			a_data = null;
			return false;
		}

		public override void UpdateAfterEditing(Plan a_plan)
		{ }

		public override bool HasError(APolicyData a_data)
		{
			return ((PolicyPlanDataEnergy)a_data).energyError;
		}

		public void UpdateGrids(Plan a_plan, HashSet<int> a_deleted, List<GridObject> a_newGrids)
		{
			//Don't update grids if we have the plan locked. Prevents updates while editing.
			if (!a_plan.IsLockedByUs && a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var planData))
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
				List<EnergyGrid> energyGridsBeforePlan = GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);

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
		/// <param name="a_plan"> Plan before which grids are calced </param>
		/// <param name="a_removedGridIds"> Persistent IDs of grids that have been removed in at this plan's point</param>
		/// <param name="a_includePlanItself"> Is the given plan included </param>
		/// <param name="a_forDisplaying"> Is the given plan included even if it's in design</param>
		/// <returns></returns>
		public List<EnergyGrid> GetEnergyGridsBeforePlan(Plan a_plan, out HashSet<int> a_removedGridIds, EnergyGrid.GridColor a_color, bool a_includePlanItself = false, bool a_forDisplaying = false)
		{
			List<Plan> plans = PlanManager.Instance.Plans;

			List<EnergyGrid> result = new List<EnergyGrid>();
			a_removedGridIds = new HashSet<int>();
			HashSet<int> ignoredGridIds = new HashSet<int>();
			HashSet<int> previousGridIDsLookingFor = new HashSet<int>();

			//Find the index of the given plan
			int planIndex = 0;
			for (; planIndex < plans.Count; planIndex++)
				if (plans[planIndex] == a_plan)
					break;

			//Handle plan itself if conditions are met
			if (a_includePlanItself && (a_plan.InInfluencingState || (a_forDisplaying && a_plan.State == Plan.PlanState.DESIGN)) && a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var energyData) && energyData.energyGrids != null)
			{
				foreach (EnergyGrid grid in energyData.energyGrids)
				{
					if (!grid.MatchesColor(a_color))
						continue;
					if (grid.persistentID == -1 || (!a_removedGridIds.Contains(grid.persistentID) && !ignoredGridIds.Contains(grid.persistentID)))
					{
						result.Add(grid);
						ignoredGridIds.Add(grid.persistentID);
					}
				}
				a_removedGridIds.UnionWith(energyData.removedGrids);
				if (a_forDisplaying)
					previousGridIDsLookingFor = new HashSet<int>(energyData.removedGrids);
			}

			//Add all grids whose persistentID is not in ignoredgrids
			for (int i = planIndex - 1; i >= 0; i--)
			{
				if (plans[i].InInfluencingState && plans[i].TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var planEnergyData) && planEnergyData.energyGrids != null)
				{
					foreach (EnergyGrid grid in planEnergyData.energyGrids)
					{
						if (!grid.MatchesColor(a_color))
							continue;
						if (previousGridIDsLookingFor.Contains(grid.persistentID))
						{
							//If we were looking for this persis ID, add it even if in ignored or removed
							result.Add(grid);
							ignoredGridIds.Add(grid.persistentID);
							previousGridIDsLookingFor.Remove(grid.persistentID);
						}
						else if (grid.persistentID == -1 || (!a_removedGridIds.Contains(grid.persistentID) && !ignoredGridIds.Contains(grid.persistentID)))
						{
							result.Add(grid);
							ignoredGridIds.Add(grid.persistentID);
						}
					}
					a_removedGridIds.UnionWith(planEnergyData.removedGrids);
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
			List<Plan> plans = PlanManager.Instance.Plans;
			if (plans.Count == 0)
			{
				return new List<EnergyGrid>(0);
			}

			for (int i = 0; i < plans.Count; i++)
				if (plans[i].StartTime > time)
					return GetEnergyGridsBeforePlan(plans[i], color);

			return GetEnergyGridsBeforePlan(plans[plans.Count - 1], color, true);
		}

		public void AddEnergyGrid(EnergyGrid energyGrid)
		{
			energyGrids[energyGrid.GetDatabaseID()] = energyGrid;
		}

		public EnergyGrid GetEnergyGrid(int ID)
		{
			if (!energyGrids.ContainsKey(ID))
			{
				Debug.LogError("Retrieving on non-existing key: " + ID);
				Debug.LogError("Keys available: " + string.Join(", ", energyGrids.Keys));
			}
			return energyGrids[ID];
		}

		public List<EnergyLineStringSubEntity> ForceEnergyLayersActiveUpTo(Plan plan)
		{
			//Call setactiveupto on all energy layers not yet active and clear connections
			foreach (AbstractLayer energyLayer in energyLayers)
			{
				if (!plan.IsLayerpartOfPlan(energyLayer))
					energyLayer.SetEntitiesActiveUpTo(plan);
				energyLayer.ResetEnergyConnections();
			}

			List<EnergyLineStringSubEntity> cablesToRemove = new List<EnergyLineStringSubEntity>();

			//Have the cable layer activate all connections that are present in the current state
			if (energyCableLayerGreen != null)
			{
				if (plan.GetPlanLayerForLayer(energyCableLayerGreen) != null) //Only remove invalid cables if the plan contains a cable layer
				{
					List<EnergyLineStringSubEntity> newCablesToRemove = energyCableLayerGreen.RemoveInvalidCables();
					if (newCablesToRemove != null)
						cablesToRemove = newCablesToRemove;
				}
				energyCableLayerGreen.ActivateCableLayerConnections();
			}
			if (energyCableLayerGrey != null)
			{
				if (plan.GetPlanLayerForLayer(energyCableLayerGrey) != null) //Only remove invalid cables if the plan contains a cable layer
				{
					List<EnergyLineStringSubEntity> newCablesToRemove = energyCableLayerGrey.RemoveInvalidCables();
					if (newCablesToRemove != null && newCablesToRemove.Count > 0)
						cablesToRemove.AddRange(newCablesToRemove);
				}
				energyCableLayerGrey.ActivateCableLayerConnections();
			}
			return cablesToRemove;
		}
	}
}