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
		private Dictionary<int, EnergyGrid> m_energyGrids = new Dictionary<int, EnergyGrid>();
		private List<PointLayer> m_energyPointLayers = new List<PointLayer>();
		public LineStringLayer m_energyCableLayerGreen;
		public LineStringLayer m_energyCableLayerGrey;
		public List<AbstractLayer> m_energyLayers = new List<AbstractLayer>(); //Does not include sourcepolygonpoints
		public Dictionary<int, int> m_sourceCountries = new Dictionary<int, int>();
		public Dictionary<int, SubEntity> m_energySubEntities;

		AP_Energy m_apEnergy;

		//Editing backups
		bool m_wasEnergyPlanBeforeEditing;
		PolicyPlanDataEnergy m_backup;
		List<EnergyGrid> m_energyGridsBeforePlan;
		List<EnergyLineStringSubEntity> m_removedCables;

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
				if (a_layer.m_greenEnergy)
					m_energyCableLayerGreen = a_layer as LineStringLayer;
				else
					m_energyCableLayerGrey = a_layer as LineStringLayer;
			}
			if (a_layer.m_editingType != AbstractLayer.EditingType.Normal && a_layer.m_editingType != AbstractLayer.EditingType.SourcePolygonPoint)
				m_energyLayers.Add(a_layer);
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
					a_plan.AddPolicyData(new PolicyPlanDataEnergy(this)
					{
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
				//TODO CHECK: This used to be after grid updates, check if this causes issues
				foreach (EnergyConnectionObject connection in updateData.connections)
				{
					UpdateConnection(connection);
				}
			}
		}

		public override void AddToPlan(Plan a_plan)
		{
			AddToPlan(a_plan, true);
		}

		public void AddToPlan(Plan a_plan, bool a_altersEnergyDistribution)
		{
			a_plan.AddPolicyData(new PolicyPlanDataEnergy(this) {
				altersEnergyDistribution = a_altersEnergyDistribution,
				energyGrids = new List<EnergyGrid>(),
				removedGrids = new HashSet<int>()
			});
		}

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.Policies.Remove(PolicyManager.ENERGY_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan)
		{
			m_removedCables = ForceEnergyLayersActiveUpTo(a_plan);
			m_energyGridsBeforePlan = GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);
			if (a_plan == null)
			{
				m_wasEnergyPlanBeforeEditing = false;
				m_backup = null;
			}
			else if (a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data))
			{
				m_wasEnergyPlanBeforeEditing = true;
				m_backup = new PolicyPlanDataEnergy(this)
				{
					energyGrids = new List<EnergyGrid>(data.energyGrids),
					removedGrids = new HashSet<int>(data.removedGrids),
					altersEnergyDistribution = data.altersEnergyDistribution,
					energyError = data.energyError
				};

				RecalculateGridsInEditedPlan(a_plan);
			}
			else
			{
				m_wasEnergyPlanBeforeEditing = false;
			}
		}

		public void RecalculateGridsInEditedPlan(Plan a_plan)
		{
			if (a_plan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data))
			{
				//Reset plan's grids
				List<EnergyGrid> oldGridsInPlan = data.energyGrids;
				data.removedGrids = new HashSet<int>();
				data.energyGrids = new List<EnergyGrid>();

				foreach (EnergyGrid grid in m_energyGridsBeforePlan)
					data.removedGrids.Add(grid.m_persistentID);

				foreach (AbstractLayer layer in m_energyLayers)
				{
					if (layer.m_editingType == AbstractLayer.EditingType.Socket)
					{
						//Add results of the (changed/updated) grids on the socket layer to the existing ones
						data.energyGrids.AddRange(layer.DetermineChangedGridsInPlan(a_plan, oldGridsInPlan, m_energyGridsBeforePlan, data.removedGrids)); //Updates removedgrids
					}
				}
			}
		}

		public override bool CalculateEffectsOfEditing(Plan a_plan)
		{
			RecalculateGridsInEditedPlan(a_plan);
			if (a_plan.Policies.ContainsKey(PolicyManager.ENERGY_POLICY_NAME) && !string.IsNullOrEmpty(SessionManager.Instance.MspGlobalData.windfarm_data_api_url))
			{
				int nextTempID = -1;
				Dictionary<int, SubEntity> energyEntities = new Dictionary<int, SubEntity>();
				foreach (PlanLayer planLayer in a_plan.PlanLayers)
				{
					if (planLayer.BaseLayer.m_editingType == AbstractLayer.EditingType.SourcePolygon)
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
					se.SetPropertiesToGeoJsonFeature(feature);
				}

			}
			a_plan.AddSystemMessage("Levelized cost of energy for windfarms in plan: " + totalCost.ToString("N0") + " €/MWh");
			InterfaceCanvas.Instance.activePlanWindow.OnDelayedPolicyEffectCalculated();
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
			if(m_removedCables != null)
				RestoreRemovedCables(m_removedCables);
			if (m_wasEnergyPlanBeforeEditing)
			{
				a_plan.SetPolicyData(m_backup);
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
				SetGeneralPolicyData(a_plan, new EmptyPolicyPlanData(PolicyManager.ENERGY_POLICY_NAME), a_batch);

				// Add new grids (not distributions/sockets/sources yet)
				foreach (EnergyGrid grid in data.energyGrids)
					grid.SubmitEmptyGridOrName(a_batch); //TODO CHECK: won't this submit empty grids even if they already exist on the server?
				// Delete previously added grids no longer in this plan
				foreach (int gridID in GetGridsRemovedFromPlanSinceBackup(data))
					EnergyGrid.SubmitGridDeletionToServer(gridID, a_batch);

				SubmitRemovedGrids(a_plan, data, a_batch);
				SubmitEnergyError(a_plan, false, false, a_batch);

				//Submit grid distributions
				foreach (EnergyGrid grid in data.energyGrids)
					grid.SubmitEnergyDistribution(a_batch);
				//Submit removed (unconnected) cables
				foreach (EnergyLineStringSubEntity cable in m_removedCables)
					cable.SubmitDelete(a_batch);

				JObject dataObject = new JObject();
				dataObject.Add("id", a_plan.GetDataBaseOrBatchIDReference());
				dataObject.Add("alters_energy_distribution", data.altersEnergyDistribution ? 1 : 0);
				a_batch.AddRequest(Server.SetPlanEnergyDistribution(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
			else if(m_wasEnergyPlanBeforeEditing)
			{
				DeleteGeneralPolicyData(a_plan, PolicyManager.ENERGY_POLICY_NAME, a_batch);
				JObject dataObject = new JObject();
				dataObject.Add("plan", a_plan.GetDataBaseOrBatchIDReference());
				a_batch.AddRequest(Server.DeleteEnergyFromPlan(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
		}

		public override void StopEditingPlan(Plan a_plan)
		{
			m_backup = null;
			m_energyGridsBeforePlan = null;
			m_removedCables = null;
		}

		public override void GetIssueText(APolicyPlanData a_planData, List<string> a_issueText)
		{
			if(((PolicyPlanDataEnergy)a_planData).energyError)
			{
				a_issueText.Add("The energy distribution has been invalidated and must be recalculated. To do this move the plan to the design state, start editing and accept. Note that editing might change energy cables and distributions to repair the plan, make sure to check these.");
			}
		}

		public override int GetMaximumIssueSeverityAndCount(APolicyPlanData a_planData, out ERestrictionIssueType a_severity)
		{
			if (((PolicyPlanDataEnergy)a_planData).energyError)
			{
				a_severity = ERestrictionIssueType.Error;
				return 1;
			}
			else return base.GetMaximumIssueSeverityAndCount(a_planData, out a_severity);
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
					//if no energy layers in plan, remove energy policy data
					if(a_plan.GetPlanLayerForLayer(m_energyCableLayerGreen) == null && a_plan.GetPlanLayerForLayer(m_energyCableLayerGrey) == null)
						RemoveFromPlan(a_plan);
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
			if (m_energyCableLayerGreen != null || m_energyCableLayerGrey != null)
			{
				foreach (AbstractLayer energyLayer in m_energyLayers)
				{
					energyLayer.ResetCurrentGrids();
					energyLayer.SetEntitiesActiveUpTo(plan);
				}

				List<EnergyGrid> grids = GetEnergyGridsAtTime(plan.StartTime, EnergyGrid.GridColor.Either);
				if (m_energyCableLayerGreen != null)
				{
					m_energyCableLayerGreen.ActivateCableLayerConnections();
					Dictionary<int, List<DirectionalConnection>> network = m_energyCableLayerGreen.GetCableNetworkForPlan(plan);
					foreach (EnergyGrid grid in grids)
						if (grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
				if (m_energyCableLayerGrey != null)
				{
					m_energyCableLayerGrey.ActivateCableLayerConnections();
					Dictionary<int, List<DirectionalConnection>> network = m_energyCableLayerGrey.GetCableNetworkForPlan(plan);
					foreach (EnergyGrid grid in grids)
						if (!grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
			}
		}

		void OnUpdateVisibleLayersToBase()
		{
			if (m_energyCableLayerGreen != null || m_energyCableLayerGrey != null)
			{
				foreach (AbstractLayer energyLayer in m_energyLayers)
					energyLayer.ResetCurrentGrids();
			}
		}

		void OnUpdateVisibleLayersToTime(int month)
		{
			if (m_energyCableLayerGreen != null || m_energyCableLayerGrey != null)
			{
				foreach (AbstractLayer energyLayer in m_energyLayers)
					energyLayer.ResetCurrentGrids();

				List<EnergyGrid> grids = GetEnergyGridsAtTime(month, EnergyGrid.GridColor.Either);
				if (m_energyCableLayerGreen != null)
				{
					Dictionary<int, List<DirectionalConnection>> network = m_energyCableLayerGreen.GetCableNetworkAtTime(month);
					foreach (EnergyGrid grid in grids)
						if (grid.IsGreen)
							grid.SetAsCurrentGridForContent(network);
				}
				if (m_energyCableLayerGrey != null)
				{
					Dictionary<int, List<DirectionalConnection>> network = m_energyCableLayerGrey.GetCableNetworkAtTime(month);
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
				if(a_deleted == null)
					planData.removedGrids = new HashSet<int>();
				else
					planData.removedGrids = a_deleted;
				planData.energyGrids = new List<EnergyGrid>();
				if (a_newGrids != null)
				{
					foreach (GridObject obj in a_newGrids)
						planData.energyGrids.Add(new EnergyGrid(obj, a_plan));
				}
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
				point1 = (tempSubEnt as EnergyPolygonSubEntity).m_sourcePoint;
			else
				point1 = tempSubEnt as EnergyPointSubEntity;

			tempSubEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(endID);
			if (tempSubEnt == null) return;
			else if (tempSubEnt is EnergyPolygonSubEntity)
				point2 = (tempSubEnt as EnergyPolygonSubEntity).m_sourcePoint;
			else
				point2 = tempSubEnt as EnergyPointSubEntity;

			Connection conn1 = new Connection(cable, point1, true);
			Connection conn2 = new Connection(cable, point2, false);

			//Cables store connections and attach them to points when editing starts		
			if (cable.Connections.Count > 1)
				cable.Connections.Clear(); //We already had connections previously, but it has been updated so clear the old one
			cable.AddConnection(conn1);
			cable.AddConnection(conn2);
		}

		/// <summary>
		/// Duplicates (value copies) the given energy grid and adds it to this plan.
		/// </summary>
		public static EnergyGrid DuplicateEnergyGridToPlan(EnergyGrid a_gridToDuplicate, Plan a_plan)
		{
			EnergyGrid duplicate = new EnergyGrid(a_gridToDuplicate, a_plan);
			duplicate.m_distributionOnly = true;

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

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
		{
			PolicyPlanDataEnergy planData = (PolicyPlanDataEnergy)a_planData;

			//Determine countries affected by removed grids
			List<EnergyGrid> energyGridsBeforePlan = GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);

			HashSet<int> countriesAffectedByRemovedGrids = new HashSet<int>();
			foreach (EnergyGrid grid in energyGridsBeforePlan)
			{
				if (planData.removedGrids.Contains(grid.m_persistentID))
				{
					foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.m_energyDistribution.m_distribution)
					{
						if (a_approvalReasons.TryGetValue(countryAmount.Key, out var reasons))
							reasons.Add(new ApprovalReasonEnergyPolicy(grid, true));
						else
							a_approvalReasons.Add(countryAmount.Key, new List<IApprovalReason> { new ApprovalReasonEnergyPolicy(grid, true) });

						if (!countriesAffectedByRemovedGrids.Contains(countryAmount.Key))
							countriesAffectedByRemovedGrids.Add(countryAmount.Key);
					}
				}
			}

			//Removed grids
			if (!a_reasonOnly && countriesAffectedByRemovedGrids != null)
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
					foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.m_energyDistribution.m_distribution)
					{
						if (a_approvalReasons.TryGetValue(countryAmount.Key, out var reasons))
							reasons.Add(new ApprovalReasonEnergyPolicy(grid, false));
						else
							a_approvalReasons.Add(countryAmount.Key, new List<IApprovalReason> { new ApprovalReasonEnergyPolicy(grid, false) });


						if (!a_reasonOnly && !a_approvalStates.ContainsKey(countryAmount.Key))
						{
							a_approvalStates.Add(countryAmount.Key, EPlanApprovalState.Maybe);
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
		public List<EnergyGrid> GetEnergyGridsBeforePlan(Plan a_plan, out HashSet<int> a_removedGridIds, EnergyGrid.GridColor a_color, out Dictionary<int, GridEnergyDistribution> a_previousDistributions, bool a_includePlanItself = false, bool a_forDisplaying = false)
		{
			List<Plan> plans = PlanManager.Instance.Plans;

			a_removedGridIds = new HashSet<int>();
			a_previousDistributions = new Dictionary<int, GridEnergyDistribution>();
			List<EnergyGrid> result = new List<EnergyGrid>();
			HashSet<int> ignoredGridIds = new HashSet<int>();
			HashSet<int> removedButDisplayedIds = new HashSet<int>();

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
					if (grid.m_persistentID == -1 || (!a_removedGridIds.Contains(grid.m_persistentID) && !ignoredGridIds.Contains(grid.m_persistentID)))
					{
						result.Add(grid);
						ignoredGridIds.Add(grid.m_persistentID);
						if(grid.m_persistentID != -1)
						{
							a_previousDistributions.Add(grid.m_persistentID, null);
						}
					}
				}
				a_removedGridIds.UnionWith(energyData.removedGrids);
				if (a_forDisplaying)
					removedButDisplayedIds = new HashSet<int>(energyData.removedGrids);
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
						if (removedButDisplayedIds.Contains(grid.m_persistentID))
						{
							//If we were looking for this persis ID, add it even if in ignored or removed
							result.Add(grid);
							ignoredGridIds.Add(grid.m_persistentID);
							removedButDisplayedIds.Remove(grid.m_persistentID);
						}
						else if(a_previousDistributions.TryGetValue(grid.m_persistentID, out var value) && value == null)
						{
							a_previousDistributions[grid.m_persistentID] = grid.m_energyDistribution;
						}
						else if (grid.m_persistentID == -1 || (!a_removedGridIds.Contains(grid.m_persistentID) && !ignoredGridIds.Contains(grid.m_persistentID)))
						{
							result.Add(grid);
							ignoredGridIds.Add(grid.m_persistentID);
						}
					}
					if (planEnergyData.removedGrids != null)
					{
						a_removedGridIds.UnionWith(planEnergyData.removedGrids);
					}
				}
			}

			return result;
		}

		public List<EnergyGrid> GetEnergyGridsBeforePlan(Plan plan, EnergyGrid.GridColor color, out Dictionary<int, GridEnergyDistribution> a_previousDistributions, bool includePlanItself = false, bool forDisplaying = false)
		{
			HashSet<int> ignoredGridIds;
			return GetEnergyGridsBeforePlan(plan, out ignoredGridIds, color, out a_previousDistributions, includePlanItself, forDisplaying);
		}

		public List<EnergyGrid> GetEnergyGridsBeforePlan(Plan plan, EnergyGrid.GridColor color, bool includePlanItself = false, bool forDisplaying = false)
		{
			HashSet<int> ignoredGridIds;
			Dictionary<int, GridEnergyDistribution> previousDistributions;
			return GetEnergyGridsBeforePlan(plan, out ignoredGridIds, color, out previousDistributions, includePlanItself, forDisplaying);
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
			m_energyGrids[energyGrid.GetDatabaseID()] = energyGrid;
		}

		public EnergyGrid GetEnergyGrid(int ID)
		{
			if (!m_energyGrids.ContainsKey(ID))
			{
				Debug.LogError("Retrieving on non-existing key: " + ID);
				Debug.LogError("Keys available: " + string.Join(", ", m_energyGrids.Keys));
			}
			return m_energyGrids[ID];
		}

		public List<EnergyLineStringSubEntity> ForceEnergyLayersActiveUpTo(Plan plan)
		{
			//Call setactiveupto on all energy layers not yet active and clear connections
			foreach (AbstractLayer energyLayer in m_energyLayers)
			{
				if (!plan.IsLayerpartOfPlan(energyLayer))
					energyLayer.SetEntitiesActiveUpTo(plan);
				energyLayer.ResetEnergyConnections();
			}

			List<EnergyLineStringSubEntity> cablesToRemove = new List<EnergyLineStringSubEntity>();

			//Have the cable layer activate all connections that are present in the current state
			if (m_energyCableLayerGreen != null)
			{
				if (plan.GetPlanLayerForLayer(m_energyCableLayerGreen) != null) //Only remove invalid cables if the plan contains a cable layer
				{
					List<EnergyLineStringSubEntity> newCablesToRemove = m_energyCableLayerGreen.RemoveInvalidCables();
					if (newCablesToRemove != null)
						cablesToRemove = newCablesToRemove;
				}
				m_energyCableLayerGreen.ActivateCableLayerConnections();
			}
			if (m_energyCableLayerGrey != null)
			{
				if (plan.GetPlanLayerForLayer(m_energyCableLayerGrey) != null) //Only remove invalid cables if the plan contains a cable layer
				{
					List<EnergyLineStringSubEntity> newCablesToRemove = m_energyCableLayerGrey.RemoveInvalidCables();
					if (newCablesToRemove != null && newCablesToRemove.Count > 0)
						cablesToRemove.AddRange(newCablesToRemove);
				}
				m_energyCableLayerGrey.ActivateCableLayerConnections();
			}
			return cablesToRemove;
		}

		public void RestoreRemovedCables(List<EnergyLineStringSubEntity> removedCables)
		{
			if (m_energyCableLayerGreen != null)
			{
				m_energyCableLayerGreen.RestoreInvalidCables(removedCables);
			}
			if (m_energyCableLayerGrey != null)
			{
				m_energyCableLayerGrey.RestoreInvalidCables(removedCables);
			}
		}

		public bool CheckForInvalidCables(Plan a_plan)
		{
			foreach (PlanLayer planLayer in a_plan.PlanLayers)
			{
				//Check all new geometry in cable layers
				if (planLayer.BaseLayer.IsEnergyLineLayer() && planLayer.GetNewGeometryCount() > 0)
				{
					//Create layer states for energy layers of a marching color, ignoring the cable layer
					Dictionary<AbstractLayer, LayerState> energyLayerStates = new Dictionary<AbstractLayer, LayerState>();
					foreach (AbstractLayer energyLayer in PolicyLogicEnergy.Instance.m_energyLayers)
						if (energyLayer.m_greenEnergy == planLayer.BaseLayer.m_greenEnergy && energyLayer.m_id != planLayer.BaseLayer.m_id)
							energyLayerStates.Add(energyLayer, energyLayer.GetLayerStateAtPlan(a_plan));

					foreach (Entity entity in planLayer.GetNewGeometry())
					{
						//Check the 2 connections for valid points
						EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)entity.GetSubEntity(0);
						foreach (Connection conn in cable.Connections)
						{
							bool found = false;
							AbstractLayer targetPointLayer = conn.point.m_sourcePolygon == null ? conn.point.m_entity.Layer : conn.point.m_sourcePolygon.m_entity.Layer;
							foreach (Entity existingEntity in energyLayerStates[targetPointLayer].baseGeometry)
							{
								if (existingEntity.DatabaseID == conn.point.GetDatabaseID())
								{
									found = true;
									break;
								}
							}
							if (!found)
								return true;
						}
					}
				}
			}
			return false;
		}

		public void AddEnergyPointLayer(PointLayer layer)
		{
			m_energyPointLayers.Add(layer);
		}

		public List<PointLayer> GetCenterPointLayers()
		{
			List<PointLayer> result = new List<PointLayer>();
			foreach (PointLayer layer in m_energyPointLayers)
				if (layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
					result.Add(layer);
			return result;
		}

		public EnergyPointSubEntity GetEnergyPointAtPosition(Vector3 pos)
		{
			foreach (PointLayer p in m_energyPointLayers)
				if (LayerManager.Instance.LayerIsVisible(p) || (p.m_sourcePolyLayer != null && LayerManager.Instance.LayerIsVisible(p.m_sourcePolyLayer)))
					foreach (SubEntity e in p.GetSubEntitiesAt(pos))
						if (e is EnergyPointSubEntity)
							return (e as EnergyPointSubEntity);
			return null;
		}

		public void RemoveEnergySubEntityReference(int ID)
		{
			if (ID == -1)
				return;
			if (m_energySubEntities != null)
				m_energySubEntities.Remove(ID);
		}
		public void AddEnergySubEntityReference(int ID, SubEntity subent)
		{
			if (ID == -1)
				return;
			if (m_energySubEntities == null)
				m_energySubEntities = new Dictionary<int, SubEntity>();
			if (!m_energySubEntities.ContainsKey(ID))
				m_energySubEntities.Add(ID, subent);
		}
		public SubEntity GetEnergySubEntityByID(int ID, bool getSourcePointIfPoly = false)
		{
			SubEntity result = null;
			if (m_energySubEntities != null)
				m_energySubEntities.TryGetValue(ID, out result);
			if (getSourcePointIfPoly && result is EnergyPolygonSubEntity)
				result = ((EnergyPolygonSubEntity)result).m_sourcePoint;
			return result;
		}

		List<int> GetGridsRemovedFromPlanSinceBackup(PolicyPlanDataEnergy a_data)
		{
			List<int> result = new List<int>();
			if (m_backup == null || m_backup.energyGrids == null)
				return result;
			bool found;
			foreach (EnergyGrid oldGrid in m_backup.energyGrids)
			{
				found = false;
				foreach (EnergyGrid newGrid in a_data.energyGrids)
				{
					if (newGrid.GetDatabaseID() == oldGrid.GetDatabaseID())
					{
						found = true;
						break;
					}
				}
				if (!found)
					result.Add(oldGrid.GetDatabaseID());
			}
			return result;
		}

		public override void EditedPlanTimeChanged(Plan a_plan) 
		{
			m_removedCables = ForceEnergyLayersActiveUpTo(a_plan);
			m_energyGridsBeforePlan = GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);
			if (m_apEnergy != null && m_apEnergy.IsOpen)
			{
				m_apEnergy.RefreshContent(a_plan);
			}
			else
				RecalculateGridsInEditedPlan(a_plan);
		}

		public override void PreviousPlanChangedInfluence(Plan a_plan) 
		{
			m_removedCables = ForceEnergyLayersActiveUpTo(a_plan);
			m_energyGridsBeforePlan = GetEnergyGridsBeforePlan(a_plan, EnergyGrid.GridColor.Either);
			if (m_apEnergy != null && m_apEnergy.IsOpen)
			{
				m_apEnergy.RefreshContent(a_plan);
			}
			else
				RecalculateGridsInEditedPlan(a_plan);
		}

		public void RegisterAPEnergy(AP_Energy a_apEnergy)
		{
			m_apEnergy = a_apEnergy;
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