using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class SimulationLogicMEL : ASimulationLogic
	{
		static SimulationLogicMEL m_instance;
		public static SimulationLogicMEL Instance => m_instance;

		private KPIValueCollection m_sharedEcologyKPI;
		private Dictionary<int, KPIValueCollection> m_ecologyKPIs;
		private SimulationSettingsMEL m_config;

		//Has to be stored separately because PropertyMetaData PolicyType differs from PropertyName...
		private string m_seasonalClosureGMPName;
		private string m_BufferZoneGMPName;

		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateMEL data = (SimulationUpdateMEL)a_data;
			ReceiveEcologyKPIUpdate(data.kpi);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			m_instance = this;

			m_config = (SimulationSettingsMEL)a_settings;

			Main.Instance.OnFinishedLoadingLayers += CreateEcologyKPIs; //Requires layers to be imported first
		}
		public override void Destroy()
		{
			m_instance = null;
		}

		public override List<KPIValueCollection> GetKPIValuesForCountry(int a_countryId = -1)
		{
			if(a_countryId >= 0)
				return new List<KPIValueCollection> { m_sharedEcologyKPI, m_ecologyKPIs[a_countryId] };
			return new List<KPIValueCollection> { m_sharedEcologyKPI };
		}

		public void CreateEcologyKPIs()
		{
			List<KPICategoryDefinition> categoryDefinitions = m_config.content["ecologyCategories"].ToObject<List<KPICategoryDefinition>>();
			
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
					if (property.PolicyType == PolicyManager.SEASONAL_CLOSURE_POLICY_NAME) 
					{
						hasGeomPolicy = true;
						m_seasonalClosureGMPName = property.PropertyName;
					}
					else if(property.PolicyType == PolicyManager.BUFFER_ZONE_POLICY_NAME)
					{
						hasGeomPolicy = true;
						m_BufferZoneGMPName = property.PropertyName;
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
							valueDisplayName = layerType.Name,
							unit = "km2",
							valueDependentCountry = KPIValue.CountrySpecific
						});
					}
				}
				newCat.valueDefinitions = values.ToArray();
				categoryDefinitions.Add(newCat);
			}

			m_sharedEcologyKPI = new KPIValueCollection();
			m_sharedEcologyKPI.SetupKPIValues(categoryDefinitions.ToArray(), SessionManager.Instance.MspGlobalData.session_end_month);
			m_sharedEcologyKPI.OnKPIValuesReceivedAndProcessed += OnEcologyKPIReceivedNewMonth;
			m_config = null;
		}

		public void ReceiveEcologyKPIUpdate(KPIObject[] a_objects)
		{
			m_sharedEcologyKPI.ProcessReceivedKPIData(a_objects);
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
			for (int month = a_previousMostRecentMonth + 1; month <= a_mostRecentMonth; ++month)
			{
				int currentMonthNorm = month % 12;
				state.AdvanceStateToMonth(month);

				float[] sizeByGear = new float[PolicyLogicFishing.Instance.GetGearTypes().Length];
				foreach (Entity t in state.baseGeometry)
				{
					float baseArea = t.GetRestrictionAreaSurface();

					//Check seasonal closure policy, add area if relevant
					if(t.TryGetMetaData(m_seasonalClosureGMPName, out string seasonalClosureString))
					{
						PolicyGeometryDataSeasonalClosure policyData = new PolicyGeometryDataSeasonalClosure(seasonalClosureString);
						foreach(var bansByGear in policyData.fleets)
						{
							//Check if geometry policy contains any bans for gear for the current month
							bool banned = false;
							foreach(var bansByCountry in bansByGear.Value)
							{
								if(bansByCountry.Value.MonthSet(currentMonthNorm))
								{
									banned = true;
									break;
								}
							}
							if(banned)
							{
								sizeByGear[bansByGear.Key] += baseArea;
							}
						}
					}

					//Check buffer zone policy, add area if relevgant
					if (t.TryGetMetaData(m_BufferZoneGMPName, out string bufferZoneString))
					{
						PolicyGeometryDataBufferZone policyData = new PolicyGeometryDataBufferZone(bufferZoneString);
						if (policyData.radius >= 0.001f)
						{
							float bufferArea = ((PolygonEntity)t).GetOffsetArea(policyData.radius) - baseArea;
							foreach (var bansByGear in policyData.fleets)
							{
								//Check if geometry policy contains any bans for gear for the current month
								bool banned = false;
								foreach (var bansByCountry in bansByGear.Value)
								{
									if (bansByCountry.Value.MonthSet(currentMonthNorm))
									{
										banned = true;
										break;
									}
								}
								if (banned)
								{
									sizeByGear[bansByGear.Key] += bufferArea;
								}
							}
						}
					}
				}

				for(int j = 0; j < sizeByGear.Length; j++)
				{
					a_valueCollection.TryUpdateKPIValue($"{a_layer.FileName}_{PolicyLogicFishing.Instance.GetGearName(j)}", month, sizeByGear[j]);
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