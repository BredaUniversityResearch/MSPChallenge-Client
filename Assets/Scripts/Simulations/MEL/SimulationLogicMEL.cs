using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	public class SimulationLogicMEL : ASimulationLogic
	{
		static SimulationLogicMEL m_instance;
		public static SimulationLogicMEL Instance => m_instance;

		private KPIValueCollection m_ecologyKPI;
		public List<string> fishingFleets;
		public float initialFishingMapping;
		public float fishingDisplayScale;
		public FishingDistributionDelta initialFishingValues;

		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateMEL data = (SimulationUpdateMEL)a_data;
			ReceiveEcologyKPIUpdate(data.kpi);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			m_instance = this;
			//Currently in Server.GetMELConfig()

			SimulationSettingsMEL config = (SimulationSettingsMEL)a_settings;

			CreateEcologyKPIs(config.content);
			LoadFishingFleets(config.content);
		}
		public override void Destroy()
		{
			m_instance = null;
		}

		public override KPIValueCollection GetKPIValuesForCountry(int a_countryId = -1)
		{
			return m_ecologyKPI;
		}

		public void CreateEcologyKPIs(JObject a_melConfig)
		{
			KPICategoryDefinition[] categoryDefinitions = a_melConfig["ecologyCategories"].ToObject<KPICategoryDefinition[]>();
			m_ecologyKPI = new KPIValueCollection();
			m_ecologyKPI.SetupKPIValues(categoryDefinitions, SessionManager.Instance.MspGlobalData.session_end_month);
			m_ecologyKPI.OnKPIValuesReceivedAndProcessed += OnEcologyKPIReceivedNewMonth;
		}

		public void ReceiveEcologyKPIUpdate(KPIObject[] a_objects)
		{
			m_ecologyKPI.ProcessReceivedKPIData(a_objects);
		}

		private void OnEcologyKPIReceivedNewMonth(KPIValueCollection a_valueCollection, int a_previousMostRecentMonth, int a_mostRecentMonth)
		{
			foreach (AbstractLayer layer in LayerManager.Instance.protectedAreaLayers)
			{
				LayerState state = layer.GetLayerStateAtTime(a_previousMostRecentMonth);
				for (int i = a_previousMostRecentMonth + 1; i <= a_mostRecentMonth; ++i)
				{
					state.AdvanceStateToMonth(i);

					Dictionary<EntityType, float> sizeByEntityType = new Dictionary<EntityType, float>(layer.EntityTypes.Count);
					foreach (EntityType layerType in layer.EntityTypes.Values)
					{
						//Make sure we initialize all the types otherwise the KPIs wont add values in for these new months.
						sizeByEntityType.Add(layerType, 0.0f);
					}

					foreach (Entity t in state.baseGeometry)
					{
						foreach (EntityType entityType in t.EntityTypes)
						{
							float restrictionSize;
							sizeByEntityType.TryGetValue(entityType, out restrictionSize);
							restrictionSize += t.GetRestrictionAreaSurface();
							sizeByEntityType[entityType] = restrictionSize;
						}
					}

					foreach (KeyValuePair<EntityType, float> sizeForEntityType in sizeByEntityType)
					{
						a_valueCollection.TryUpdateKPIValue(sizeForEntityType.Key.Name, i, sizeForEntityType.Value);
					}
				}
			}

			//Todo move this to it's own MonoBehaviour and trigger this OnMonthAdvanced.
			KPIRoot ecologyKPIRoot = InterfaceCanvas.Instance.KPIEcology;
			ecologyKPIRoot.groups.SetBarsToFishing(GetFishingDistributionAtTime(a_mostRecentMonth));
		}

		public void LoadFishingFleets(JObject melConfig)
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

			initialFishingValues = null;
		}

		private void SetInitialFishingValuesFromPlans()
		{
			if (initialFishingValues != null)
			{
				return; // already set.
			}
			foreach (Plan plan in PlanManager.Instance.Plans)
			{
				if (plan.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var fishingData) && fishingData.fishingDistributionDelta != null)
				{
					foreach (KeyValuePair<string, Dictionary<int, float>> values in fishingData.fishingDistributionDelta.GetValuesByFleet())
					{
						var fleetName = values.Key;
						if (initialFishingValues != null && initialFishingValues.HasFinishingValue(fleetName))
						{
							continue; // already there, skip it
						}
						// gonna set fishing values, make sure initialFishingValues is initialised. Assuming a fleet always has values
						if (initialFishingValues == null)
						{
							initialFishingValues = new FishingDistributionDelta();
						}
						foreach (var item in values.Value)
						{
							initialFishingValues.SetFishingValue(fleetName, item.Key, item.Value); // add it the initial value
						}
					}
				}
			}
		}

		public FishingDistributionSet GetFishingDistributionForPreviousPlan(Plan referencePlan)
		{
			SetInitialFishingValuesFromPlans();
			FishingDistributionSet result = new FishingDistributionSet(initialFishingValues);
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
			SetInitialFishingValuesFromPlans();
			FishingDistributionSet result = new FishingDistributionSet(initialFishingValues);
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

	public class FishingFleet
	{
		public string name;
		public float scalar;
	}
}