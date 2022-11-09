using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using GeoJSON.Net.Feature;

namespace MSP2050.Scripts
{
	public class PolicyLogicEnergy : APolicyLogic
	{
		static PolicyLogicEnergy m_instance;
		public static PolicyLogicEnergy Instance => m_instance;

		//References
		private Dictionary<int, EnergyGrid> energyGrids = new Dictionary<int, EnergyGrid>();
		private List<PointLayer> energyPointLayers = new List<PointLayer>();
		public LineStringLayer energyCableLayerGreen;
		public LineStringLayer energyCableLayerGrey;
		public List<AbstractLayer> energyLayers = new List<AbstractLayer>(); //Does not include sourcepolygonpoints
		public Dictionary<int, int> sourceCountries = new Dictionary<int, int>();
		public Dictionary<int, SubEntity> energySubEntities;

		//Editing backups
		bool m_wasEnergyPlanBeforeEditing;
		List<EnergyGrid> energyGridBackup;
		List<EnergyGrid> energyGridsBeforePlan;
		HashSet<int> energyGridRemovedBackup;
		List<EnergyLineStringSubEntity> removedCables;

		public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
		{
			base.Initialise(a_settings, a_definition);
			m_instance = this;
			LayerManager.Instance.OnLayerLoaded += OnLayerLoaded;
			LayerManager.Instance.OnVisibleLayersUpdatedToBase += OnUpdateVisibleLayersToBase;
			LayerManager.Instance.OnVisibleLayersUpdatedToPlan += OnUpdateVisibleLayersToPlan;
			LayerManager.Instance.OnVisibleLayersUpdatedToTime += OnUpdateVisibleLayersToTime;
		}

		public override void Destroy()
		{
			m_instance = null;
		}

		void OnLayerLoaded(AbstractLayer a_layer)
		{
			if (a_layer.IsEnergyLineLayer())
			{
				if (a_layer.greenEnergy)
					energyCableLayerGreen = a_layer as LineStringLayer;
				else
					energyCableLayerGrey = a_layer as LineStringLayer;
			}
			if (a_layer.editingType != AbstractLayer.EditingType.Normal && a_layer.editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				energyLayers.Add(a_layer);
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
						logic = this,
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

		public override void AddToPlan(Plan a_plan)
		{
			AddToPlan(a_plan, false);
		}

		void AddToPlan(Plan a_plan, bool a_altersEnergyDistribution)
		{
			//TODO: add base energy data
			a_plan.AddPolicyData(new PolicyPlanDataEnergy() { logic = this, altersEnergyDistribution = a_altersEnergyDistribution });
			//TODO: check removed cables, grids before plan etc.
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.m_policies.Remove(PolicyManager.ENERGY_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan)
		{
			if(a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data))
			{ 
				m_wasEnergyPlanBeforeEditing = true;
				energyGridBackup = data.energyGrids;
				energyGridRemovedBackup = data.removedGrids;
			
				//Reset plan's grids
				List<EnergyGrid> oldGrids = data.energyGrids;
				data.removedGrids = new HashSet<int>();
				data.energyGrids = new List<EnergyGrid>();

				foreach (EnergyGrid grid in energyGridsBeforePlan)
					data.removedGrids.Add(grid.persistentID);

				foreach (AbstractLayer layer in energyLayers)
				{
					if (layer.editingType == AbstractLayer.EditingType.Socket)
					{
						//Add results of the grids on the socket layer to the existing ones
						data.energyGrids.AddRange(layer.DetermineGrids(a_plan, oldGrids, energyGridsBeforePlan, data.removedGrids, out data.removedGrids));
						//TODO: this adds all energygrids to the plan while that shouldnt happen!
					}
				}
			}
			else
			{
				m_wasEnergyPlanBeforeEditing = false;
			}
			removedCables = ForceEnergyLayersActiveUpTo(a_plan);
			energyGridsBeforePlan = GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);
		}

		public override bool CalculateEffectsOfEditing(Plan a_plan) 
		{
			if (a_plan.m_policies.ContainsKey(PolicyManager.ENERGY_POLICY_NAME) && !string.IsNullOrEmpty(SessionManager.Instance.MspGlobalData.windfarm_data_api_url))
			{
				int nextTempID = -1;
				Dictionary<int, SubEntity> energyEntities = new Dictionary<int, SubEntity>();
				foreach (PlanLayer planLayer in a_plan.PlanLayers)
				{
					if (planLayer.BaseLayer.editingType == AbstractLayer.EditingType.SourcePolygon)
					{
						//Ignores removed geometry
						foreach (Entity entity in planLayer.GetNewGeometry())
						{
							//Because entities might be newly created and not have IDs, use temporary IDs.
							int id = entity.DatabaseID;
							if (id < 0)
								id = nextTempID--;

							energyEntities.Add(id, entity.GetSubEntity(0));
						}
					}
				}
				ServerCommunication.Instance.DoExternalAPICall<FeatureCollection>(SessionManager.Instance.MspGlobalData.windfarm_data_api_url, energyEntities, (result) => ExternalEnergyEffectsReturned(a_plan, result, energyEntities), ExternalEnergyEffectsFailed);
				return true;
			}
			return false;
		}

		void ExternalEnergyEffectsReturned(Plan a_plan, FeatureCollection a_collection, Dictionary<int, SubEntity> a_passedEnergyEntities)
		{
			double totalCost = 0;
			foreach (Feature feature in a_collection.Features)
			{
				SubEntity se;
				if (a_passedEnergyEntities.TryGetValue(int.Parse(feature.Id), out se))
				{
					object cost;
					if (feature.Properties.TryGetValue("levelized_cost_of_energy", out cost) && cost != null)
					{
						totalCost += (double)cost;
					}
					se.SetPropertiesToGeoJSONFeature(feature);
				}

			}
			a_plan.AddSystemMessage("Levelized cost of energy for windfarms in plan: " + totalCost.ToString("N0") + " €/MWh");
		}

		void ExternalEnergyEffectsFailed(ARequest request, string message)
		{
			if (request.retriesRemaining > 0)
			{
				Debug.LogError($"External API call failed, message: {message}. Retrying {request.retriesRemaining} more times.");
				ServerCommunication.Instance.RetryRequest(request);
			}
			else
			{
				Debug.LogError($"External API call failed, message: {message}. Using built in alternative.");
				InterfaceCanvas.Instance.activePlanWindow.OnDelayedPolicyEffectCalculated();
			}
		}

		public override void RestoreBackupForPlan(Plan a_plan)
		{
			if(removedCables != null)
				RestoreRemovedCables(removedCables);
			if(m_wasEnergyPlanBeforeEditing)
			{ 
				//TODO
			
			}
			else
			{
				RemoveFromPlan(a_plan);
			}
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch)
		{
			if (a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data))
			{
				// Add new grids (not distributions/sockets/sources yet)
				foreach (EnergyGrid grid in data.energyGrids)
					grid.SubmitEmptyGridToServer(a_batch);
				// Delete previously added grids no longer in this plan
				foreach (int gridID in GetGridsRemovedFromPlanSinceBackup(data))
					EnergyGrid.SubmitGridDeletionToServer(gridID, a_batch);

				SubmitRemovedGrids(a_plan, data, a_batch);
				SubmitEnergyError(a_plan, false, false, a_batch);

				//Submit grid distributions
				foreach (EnergyGrid grid in data.energyGrids)
					grid.SubmitEnergyDistribution(a_batch);
				//Submit removed (unconnected) cables
				foreach (EnergyLineStringSubEntity cable in removedCables)
					cable.SubmitDelete(a_batch);
			}
			else if(m_wasEnergyPlanBeforeEditing)
			{
				JObject dataObject = new JObject();
				dataObject.Add("plan", a_plan.GetDataBaseOrBatchIDReference());
				a_batch.AddRequest(Server.DeleteEnergyFromPlan(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
		}

		public override void StopEditingPlan(Plan a_plan)
		{
			energyGridBackup = null;
			energyGridsBeforePlan = null;
			energyGridRemovedBackup = null;
			removedCables = null;
		}


		public override bool ShowPolicyToggled(APolicyPlanData a_planData)
		{
			return ((PolicyPlanDataEnergy)a_planData).altersEnergyDistribution;
		}

		public override void SetPolicyToggled(Plan a_plan, bool a_value)
		{
			if(a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var energyData))
			{
				energyData.altersEnergyDistribution = a_value;
				if(!a_value)
				{
					//TODO: if no energy layers, remove energy policy data
				}
			}
			else if(a_value)
			{
				AddToPlan(a_plan, true);
			}
		}

		public override bool HasError(APolicyPlanData a_data)
		{
			return ((PolicyPlanDataEnergy)a_data).energyError;
		}

		void OnUpdateVisibleLayersToPlan(Plan plan)
		{
			if (energyCableLayerGreen != null || energyCableLayerGrey != null)
			{
				foreach (AbstractLayer energyLayer in energyLayers)
					energyLayer.ResetCurrentGrids();

				List<EnergyGrid> grids = GetEnergyGridsAtTime(plan.StartTime, EnergyGrid.GridColor.Either);
				if (energyCableLayerGreen != null)
				{
					Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGreen.GetCableNetworkForPlan(plan);
					foreach (EnergyGrid grid in grids)
						if (grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
				if (energyCableLayerGrey != null)
				{
					Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGrey.GetCableNetworkForPlan(plan);
					foreach (EnergyGrid grid in grids)
						if (!grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
			}
		}

		void OnUpdateVisibleLayersToBase()
		{
			if (energyCableLayerGreen != null || energyCableLayerGrey != null)
			{
				foreach (AbstractLayer energyLayer in energyLayers)
					energyLayer.ResetCurrentGrids();
			}
		}

		void OnUpdateVisibleLayersToTime(int month)
		{
			if (energyCableLayerGreen != null || energyCableLayerGrey != null)
			{
				foreach (AbstractLayer energyLayer in energyLayers)
					energyLayer.ResetCurrentGrids();

				List<EnergyGrid> grids = GetEnergyGridsAtTime(month, EnergyGrid.GridColor.Either);
				if (energyCableLayerGreen != null)
				{
					Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGreen.GetCableNetworkAtTime(month);
					foreach (EnergyGrid grid in grids)
						if (grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
				if (energyCableLayerGrey != null)
				{
					Dictionary<int, List<DirectionalConnection>> network = energyCableLayerGrey.GetCableNetworkAtTime(month);
					foreach (EnergyGrid grid in grids)
						if (!grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
			}
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
			SubEntity tempSubEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(outputUpdate.id);
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
			SubEntity tempSubEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(cableID);
			if (tempSubEnt == null) return;
			EnergyLineStringSubEntity cable = tempSubEnt as EnergyLineStringSubEntity;

			//Get the points, check if they reference to a polygon or point
			tempSubEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(startID);
			if (tempSubEnt == null) return;
			else if (tempSubEnt is EnergyPolygonSubEntity)
				point1 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
			else
				point1 = tempSubEnt as EnergyPointSubEntity;

			tempSubEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(endID);
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

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, ref EApprovalType a_requiredApprovalLevel)
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

		public void RestoreRemovedCables(List<EnergyLineStringSubEntity> removedCables)
		{
			if (energyCableLayerGreen != null)
			{
				energyCableLayerGreen.RestoreInvalidCables(removedCables);
			}
			if (energyCableLayerGrey != null)
			{
				energyCableLayerGrey.RestoreInvalidCables(removedCables);
			}
		}

		public void AddEnergyPointLayer(PointLayer layer)
		{
			energyPointLayers.Add(layer);
		}

		public List<PointLayer> GetCenterPointLayers()
		{
			List<PointLayer> result = new List<PointLayer>();
			foreach (PointLayer layer in energyPointLayers)
				if (layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
					result.Add(layer);
			return result;
		}

		public EnergyPointSubEntity GetEnergyPointAtPosition(Vector3 pos)
		{
			foreach (PointLayer p in energyPointLayers)
				if (LayerManager.Instance.LayerIsVisible(p) || (p.sourcePolyLayer != null && LayerManager.Instance.LayerIsVisible(p.sourcePolyLayer)))
					foreach (SubEntity e in p.GetSubEntitiesAt(pos))
						if (e is EnergyPointSubEntity)
							return (e as EnergyPointSubEntity);
			return null;
		}

		public void RemoveEnergySubEntityReference(int ID)
		{
			if (ID == -1)
				return;
			if (energySubEntities != null)
				energySubEntities.Remove(ID);
		}
		public void AddEnergySubEntityReference(int ID, SubEntity subent)
		{
			if (ID == -1)
				return;
			if (energySubEntities == null)
				energySubEntities = new Dictionary<int, SubEntity>();
			if (!energySubEntities.ContainsKey(ID))
				energySubEntities.Add(ID, subent);
		}
		public SubEntity GetEnergySubEntityByID(int ID, bool getSourcePointIfPoly = false)
		{
			SubEntity result = null;
			if (energySubEntities != null)
				energySubEntities.TryGetValue(ID, out result);
			if (getSourcePointIfPoly && result is EnergyPolygonSubEntity)
				result = ((EnergyPolygonSubEntity)result).sourcePoint;
			return result;
		}

		List<int> GetGridsRemovedFromPlanSinceBackup(PolicyPlanDataEnergy a_data)
		{
			List<int> result = new List<int>();
			if (energyGridBackup == null)
				return result;
			bool found;
			foreach (EnergyGrid oldGrid in energyGridBackup)
			{
				found = false;
				foreach (EnergyGrid newGrid in a_data.energyGrids)
					if (newGrid.GetDatabaseID() == oldGrid.GetDatabaseID())
					{
						found = true;
						break;
					}
				if (!found)
					result.Add(oldGrid.GetDatabaseID());
			}
			return result;
		}

		public void SubmitEnergyError(Plan a_plan, bool a_value, bool a_checkDependencies, BatchRequest a_batch)
		{
			JObject dataObject = new JObject();
			dataObject.Add("id", a_plan.GetDataBaseOrBatchIDReference());
			dataObject.Add("error", a_value ? 1 : 0);
			dataObject.Add("check_dependent_plans", a_checkDependencies ? 1 : 0);
			a_batch.AddRequest(Server.SetEnergyError(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_ERROR);
		}

		public void SubmitRemovedGrids(Plan a_plan, PolicyPlanDataEnergy a_data, BatchRequest a_batch)
		{
			JObject dataObject = new JObject();
			dataObject.Add("plan", a_plan.GetDataBaseOrBatchIDReference());
			if (a_data.removedGrids != null && a_data.removedGrids.Count > 0)
				dataObject.Add("delete", JToken.FromObject(a_data.removedGrids));
			a_batch.AddRequest(Server.SetPlanRemovedGrids(), dataObject, BatchRequest.BATCH_GROUP_PLAN_GRID_CHANGE);
		}
	}
}