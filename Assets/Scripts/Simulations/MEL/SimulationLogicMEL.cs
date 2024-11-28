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
			List<KPICategoryDefinition> categoryDefinitions = a_melConfig["ecologyCategories"].ToObject<List<KPICategoryDefinition>>();
			
			//Add categories & values for protected area layers
			foreach (AbstractLayer layer in LayerManager.Instance.m_protectedAreaLayers)
			{
				KPICategoryDefinition newCat = new KPICategoryDefinition()
				{
					categoryName = layer.FileName,
					categoryDisplayName = layer.ShortName,
					categoryValueType = EKPICategoryValueType.Sum,
					unit = "km2",
					valueColorScheme = EKPIValueColorScheme.ProceduralColor
				};
				List<KPIValueDefinition> values = new List<KPIValueDefinition>();

				//Does the layer have seasonal closure or buffer zones? Then use those.
				bool hasGeomPolicy = false;
				foreach(var property in layer.m_propertyMetaData)
				{
					if (property.PolicyType == PolicyManager.SEASONAL_CLOSURE_POLICY_NAME ||
						property.PolicyType == PolicyManager.BUFFER_ZONE_POLICY_NAME)
					{
						hasGeomPolicy = true;
						break;
					}
				}

				if (hasGeomPolicy)
				{
					foreach(string gear in PolicyLogicFishing.Instance.GetGearTypes())
					{
						values.Add(new KPIValueDefinition() { 
							valueName = $"{layer.FileName}_{gear}",
							valueDisplayName = "Protection against " + gear,
							unit = "km2",
							valueDependentCountry = KPIValue.CountrySpecific
						});
					}
				}
				else
				{
					foreach (EntityType layerType in layer.m_entityTypes.Values)
					{
						values.Add(new KPIValueDefinition()
						{
							valueName = $"{layer.FileName}_{layerType.Name}",
							valueDisplayName = "Protection against " + layerType.Name,
							unit = "km2",
							valueDependentCountry = KPIValue.CountrySpecific
						});
					}
				}
				newCat.valueDefinitions = values.ToArray();
				categoryDefinitions.Add(newCat);
			}

			m_ecologyKPI = new KPIValueCollection();
			m_ecologyKPI.SetupKPIValues(categoryDefinitions.ToArray(), SessionManager.Instance.MspGlobalData.session_end_month);
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
				bool hasGeomPolicy = false;
				foreach (var property in layer.m_propertyMetaData)
				{
					if (property.PolicyType == PolicyManager.SEASONAL_CLOSURE_POLICY_NAME ||
						property.PolicyType == PolicyManager.BUFFER_ZONE_POLICY_NAME)
					{
						hasGeomPolicy = true;
						break;
					}
				}
				if (hasGeomPolicy)
					UpdateProtectionKPIForPolicyLayer(layer, a_valueCollection, a_previousMostRecentMonth, a_mostRecentMonth);
				else
					UpdateProtectionKPIForTypedLayer(layer, a_valueCollection, a_previousMostRecentMonth, a_mostRecentMonth);
			}

			InterfaceCanvas.Instance.KPIEcologyGroups.SetBarsToFishing(PolicyLogicFishing.Instance.GetFishingDistributionAtTime(a_mostRecentMonth));
		}

		private void UpdateProtectionKPIForTypedLayer(AbstractLayer a_layer, KPIValueCollection a_valueCollection, int a_previousMostRecentMonth, int a_mostRecentMonth)
		{
			LayerState state = a_layer.GetLayerStateAtTime(a_previousMostRecentMonth);
			for (int i = a_previousMostRecentMonth + 1; i <= a_mostRecentMonth; ++i)
			{
				state.AdvanceStateToMonth(i);

				Dictionary<EntityType, float> sizeByEntityType = new Dictionary<EntityType, float>(a_layer.m_entityTypes.Count);
				foreach (EntityType layerType in a_layer.m_entityTypes.Values)
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
					a_valueCollection.TryUpdateKPIValue($"{a_layer.FileName}_{sizeForEntityType.Key.Name}", i, sizeForEntityType.Value);
				}
			}
		}

		private void UpdateProtectionKPIForPolicyLayer(AbstractLayer a_layer, KPIValueCollection a_valueCollection, int a_previousMostRecentMonth, int a_mostRecentMonth)
		{
			LayerState state = a_layer.GetLayerStateAtTime(a_previousMostRecentMonth);
			for (int i = a_previousMostRecentMonth + 1; i <= a_mostRecentMonth; ++i)
			{
				state.AdvanceStateToMonth(i);

				Dictionary<string, float> sizeByGear = new Dictionary<string, float>(PolicyLogicFishing.Instance.GetGearTypes().Length);
				foreach (string gear in PolicyLogicFishing.Instance.GetGearTypes())
				{
					//Make sure we initialize all the types otherwise the KPIs wont add values in for these new months.
					sizeByGear.Add(gear, 0.0f);
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
					a_valueCollection.TryUpdateKPIValue($"{a_layer.FileName}_{sizeForEntityType.Key.Name}", i, sizeForEntityType.Value);
				}
			}
		}
	}

	public class FishingFleet
	{
		public string name;
		public float scalar;
	}
}