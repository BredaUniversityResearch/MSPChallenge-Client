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
			foreach (AbstractLayer layer in LayerManager.Instance.m_protectedAreaLayers)
			{
				LayerState state = layer.GetLayerStateAtTime(a_previousMostRecentMonth);
				for (int i = a_previousMostRecentMonth + 1; i <= a_mostRecentMonth; ++i)
				{
					state.AdvanceStateToMonth(i);

					Dictionary<EntityType, float> sizeByEntityType = new Dictionary<EntityType, float>(layer.m_entityTypes.Count);
					foreach (EntityType layerType in layer.m_entityTypes.Values)
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

			//TODO move this to it's own MonoBehaviour and trigger this OnMonthAdvanced?
			InterfaceCanvas.Instance.KPIEcologyGroups.SetBarsToFishing(PolicyLogicFishing.Instance.GetFishingDistributionAtTime(a_mostRecentMonth));
		}
	}

	public class FishingFleet
	{
		public string name;
		public float scalar;
	}
}