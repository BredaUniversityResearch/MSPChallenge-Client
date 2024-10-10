using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class PolicyLogicFishing : APolicyLogic
	{
		static PolicyLogicFishing m_instance;
		public static PolicyLogicFishing Instance => m_instance;

		FishingDistributionDelta m_fishingBackup;
		bool m_wasFishingPlanBeforeEditing;
		bool m_requireAllApproval = true;
		bool m_nationalFleets;
		FleetInfo m_fleetInfo;
		float m_defaultFishingEffort = 1f;
		float m_fishingDisplayScale = 100f;
		FishingDistributionDelta m_initialFishingDistribution;

		public bool NationalFleets => m_nationalFleets;
		public float FishingDisplayScale => m_fishingDisplayScale;

		public override void Initialise(APolicyData a_settings, PolicyDefinition a_definition)
		{
			base.Initialise(a_settings, a_definition);
			PolicySettingsFishing settings = (PolicySettingsFishing)a_settings;
			m_requireAllApproval = settings.all_country_approval;
			m_fleetInfo = settings.fleet_info;
			m_defaultFishingEffort = settings.default_fishing_effort;
			m_fishingDisplayScale = settings.fishing_display_scale;
			m_nationalFleets = m_fleetInfo.fleets[0].country_id >= 0;
			m_instance = this;

			//Set initial fishing values, using default value if undefined
			m_initialFishingDistribution = new FishingDistributionDelta();
			foreach (CountryFleetInfo countryFleet in m_fleetInfo.fleets)
			{ 
				if(m_nationalFleets)
				{
					if(countryFleet.initial_fishing_distribution != null) 
						m_initialFishingDistribution.SetFishingEffort(countryFleet.gear_type, countryFleet.country_id, countryFleet.initial_fishing_distribution[0].effort_weight);
					else
						m_initialFishingDistribution.SetFishingEffort(countryFleet.gear_type, countryFleet.country_id, m_defaultFishingEffort);
				}
				else
				{
					if (countryFleet.initial_fishing_distribution != null)
					{
						foreach(InitialFishingDistribution initial in countryFleet.initial_fishing_distribution)
						{
							m_initialFishingDistribution.SetFishingEffort(countryFleet.gear_type, initial.country_id, initial.effort_weight);
						}
						foreach (Team team in SessionManager.Instance.GetTeams())
						{
							if (!team.IsManager && !m_initialFishingDistribution.HasCountryGearValue(countryFleet.gear_type, team.ID))
								m_initialFishingDistribution.SetFishingEffort(countryFleet.gear_type, team.ID, m_defaultFishingEffort);
						}
					}
					else
					{	
						foreach (Team team in SessionManager.Instance.GetTeams())
						{
							if(!team.IsManager)
								m_initialFishingDistribution.SetFishingEffort(countryFleet.gear_type, team.ID, m_defaultFishingEffort);
						}
					}
				}
			}
		}

		public override void Destroy()
		{
			m_instance = null;
		}

		public List<CountryFleetInfo> GetFleetsForGear(int a_gearId)
		{
			List<CountryFleetInfo> result = new List<CountryFleetInfo>();
			for (int i = 0; i < m_fleetInfo.fleets.Length; i++)
			{
				if (m_fleetInfo.fleets[i].gear_type == a_gearId)
					result.Add(m_fleetInfo.fleets[i]);
			}
			return result;
		}

		public List<CountryFleetInfo> GetFleetsForCountry(int a_countryId)
		{
			List<CountryFleetInfo> result = new List<CountryFleetInfo>();
			for (int i = 0; i < m_fleetInfo.fleets.Length; i++)
			{
				if (m_fleetInfo.fleets[i].country_id == a_countryId)
					result.Add(m_fleetInfo.fleets[i]);
			}
			return result;
		}

		public int GetFleetId(int a_countryId, int a_gearId)
		{
			for(int i = 0; i < m_fleetInfo.fleets.Length; i++)
			{
				if (m_fleetInfo.fleets[i].gear_type == a_gearId && m_fleetInfo.fleets[i].country_id == a_countryId)
					return i;
			}
			Debug.LogError($"No fleet found for country id: {a_countryId}, gear id: {a_gearId}");
			return -1;
		}

		public CountryFleetInfo GetFleetInfo(int a_fleetId)
		{
			return m_fleetInfo.fleets[a_fleetId];
		}

		public CountryFleetInfo[] GetAllFleetInfo()
		{
			return m_fleetInfo.fleets;
		}

		public string[] GetGearTypes()
		{
			return m_fleetInfo.gear_types;
		}

		public string GetGearName(int a_gearType)
		{
			if (a_gearType < 0)
				return "Unknown gear";
			return m_fleetInfo.gear_types[a_gearType];
		}

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan, EPolicyUpdateStage a_stage)
		{
			if (a_stage == APolicyLogic.EPolicyUpdateStage.General)
			{
				PolicyUpdateFishingPlan updateData = (PolicyUpdateFishingPlan)a_data;
				if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(updateData.policy_type, out PolicyPlanDataFishing planData))
				{
					planData.fishingDistributionDelta = new FishingDistributionDelta(updateData.fishing);
				}
				else
				{
					a_plan.AddPolicyData(new PolicyPlanDataFishing(this)
					{
						fishingDistributionDelta = updateData.fishing != null ? new FishingDistributionDelta(updateData.fishing) : new FishingDistributionDelta() //If null, it cant pick the right constructor automatically
					});
				}
			}
		}

		public override void HandleGeneralUpdate(APolicyData a_data, EPolicyUpdateStage a_stage) 
		{ }

		public override void StopEditingPlan(Plan a_plan) 
		{ }

		public override void RemoveFromPlan(Plan a_plan)
		{
			a_plan.Policies.Remove(PolicyManager.FISHING_POLICY_NAME);
		}

		public override void StartEditingPlan(Plan a_plan) 
		{
			if(a_plan == null)
			{
				m_wasFishingPlanBeforeEditing = false;
			}
			else if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var data))
			{
				m_wasFishingPlanBeforeEditing = true;
				m_fishingBackup = data.fishingDistributionDelta;
				data.fishingDistributionDelta = data.fishingDistributionDelta.Clone();
			}
			else
			{
				m_wasFishingPlanBeforeEditing = false;
			}
		}

		public override void RestoreBackupForPlan(Plan a_plan) 
		{
			if (m_wasFishingPlanBeforeEditing)
			{
				if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var data))
				{
					data.fishingDistributionDelta = m_fishingBackup;
				}
				else
				{
					a_plan.AddPolicyData(new PolicyPlanDataFishing(this) { fishingDistributionDelta = m_fishingBackup });
				}
			}
			else
			{
				RemoveFromPlan(a_plan);
			}
		}

		public override void SubmitChangesToPlan(Plan a_plan, BatchRequest a_batch) 
		{
			if (a_plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var data))
			{
				SetGeneralPolicyData(a_plan, new EmptyPolicyPlanData(PolicyManager.FISHING_POLICY_NAME), a_batch);
				data.fishingDistributionDelta.SubmitToServer(a_plan.GetDataBaseOrBatchIDReference(), a_batch);
			}
			else if(m_wasFishingPlanBeforeEditing)
			{
				DeleteGeneralPolicyData(a_plan, PolicyManager.FISHING_POLICY_NAME, a_batch);
				JObject dataObject = new JObject();
				dataObject.Add("plan", a_plan.GetDataBaseOrBatchIDReference());
				a_batch.AddRequest(Server.DeleteFishingFromPlan(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
		}

		public override void GetRequiredApproval(APolicyPlanData a_planData, Plan a_plan, Dictionary<int, EPlanApprovalState> a_approvalStates, Dictionary<int, List<IApprovalReason>> a_approvalReasons, ref EApprovalType a_requiredApprovalLevel, bool a_reasonOnly)
		{
			PolicyPlanDataFishing planData = (PolicyPlanDataFishing)a_planData;

			if (m_requireAllApproval)
			{
				if (planData.fishingDistributionDelta.HasDistributionValues())
				{
					foreach (KeyValuePair<int, Team> kvp in SessionManager.Instance.GetTeamsByID())
					{
						if (!kvp.Value.IsManager && kvp.Value.ID != a_plan.Country)
						{
							if (!a_reasonOnly && !a_approvalStates.ContainsKey(kvp.Value.ID))
								a_approvalStates.Add(kvp.Value.ID, EPlanApprovalState.Maybe);

							if (a_approvalReasons.TryGetValue(kvp.Value.ID, out var reasons))
								reasons.Add(new ApprovalReasonFishingPolicy(-1, true));
							else
								a_approvalReasons.Add(kvp.Value.ID, new List<IApprovalReason> { new ApprovalReasonFishingPolicy(-1, true) });
						}
					}
				}
			}
			else
			{
				foreach (KeyValuePair<int, Dictionary<int, float>> fishingFleets in planData.fishingDistributionDelta.GetValuesByGear())
				{
					foreach (KeyValuePair<int, float> fishingValues in fishingFleets.Value)
					{
						if (a_approvalReasons.TryGetValue(fishingValues.Key, out var reasons))
							reasons.Add(new ApprovalReasonFishingPolicy(fishingFleets.Key));
						else
							a_approvalReasons.Add(fishingValues.Key, new List<IApprovalReason> { new ApprovalReasonFishingPolicy(fishingFleets.Key) });

						if (!a_reasonOnly && !a_approvalStates.ContainsKey(fishingValues.Key))
						{
							a_approvalStates.Add(fishingValues.Key, EPlanApprovalState.Maybe);
						}
					}
				}
			}
		}

		public override void AddToPlan(Plan a_plan)
		{
			a_plan.AddPolicyData(new PolicyPlanDataFishing(this) { fishingDistributionDelta = new FishingDistributionDelta() });
		}

		public FishingDistributionSet GetFishingDistributionForPreviousPlan(Plan referencePlan)
		{
			FishingDistributionSet result = new FishingDistributionSet(m_initialFishingDistribution);
			foreach (Plan plan in PlanManager.Instance.Plans)
			{
				if (plan.ID == referencePlan.ID)
				{
					break;
				}
				else
				{
					if (plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var fishingData) && fishingData.fishingDistributionDelta != null)
					{
						result.ApplyValues(fishingData.fishingDistributionDelta);
					}
				}
			}

			return result;
		}

		public FishingDistributionSet GetFishingDistributionAtTime(int timeMonth)
		{
			FishingDistributionSet result = new FishingDistributionSet(m_initialFishingDistribution);
			foreach (Plan plan in PlanManager.Instance.Plans)
			{
				if (plan.StartTime > timeMonth)
				{
					break;
				}

				if (plan.State == Plan.PlanState.IMPLEMENTED && plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var fishingData) && fishingData.fishingDistributionDelta != null)
				{
					result.ApplyValues(fishingData.fishingDistributionDelta);
				}
			}

			return result;
		}
	}
}