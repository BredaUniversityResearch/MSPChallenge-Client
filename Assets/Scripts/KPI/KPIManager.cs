using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using KPI;

public static class KPIManager
{
	private static KPIValueCollection ecologyKPI;
	private static CountryKPICollectionShipping shippingKPI;
	private static CountryKPICollectionEnergy energyKPIs = new CountryKPICollectionEnergy();
	private static CountryKPICollectionGeometry geometryKPIs = new CountryKPICollectionGeometry();

	static KPIManager()
	{
		Main.OnFinishedLoadingLayers += CreateGeometryKPI;
	}

	public static void CreateEnergyKPIs()
	{
		if (Main.IsSimulationConfigured(ESimulationType.CEL))
		{
			//Initialise KPIs
			foreach (Team team in TeamManager.GetTeams())
			{
				if (!team.IsManager)
				{
                    energyKPIs.AddKPIForCountry(team.ID);
				}
			}
            //Collection for all countries together
            energyKPIs.AddKPIForCountry(0);

            KPICategoryDefinition productionCategory = new KPICategoryDefinition
			{
				categoryColor = Color.magenta,
				categoryName = "Production",
				unit = ValueConversionCollection.UNIT_WATT,
				valueDefinitions = new[]
				{
					new KPIValueDefinition {valueName = "Green production", valueColor = Color.magenta, unit = ValueConversionCollection.UNIT_WATT},
					new KPIValueDefinition {valueName = "Grey production", valueColor = Color.magenta, unit = ValueConversionCollection.UNIT_WATT},
				}
			};
			KPICategoryDefinition usedCategory = new KPICategoryDefinition
			{
				categoryColor = Color.yellow,
				categoryName = "Used",
				unit = ValueConversionCollection.UNIT_WATT,
				valueDefinitions = new[]
				{
					new KPIValueDefinition {valueName = "Green used", valueColor = Color.yellow, unit = ValueConversionCollection.UNIT_WATT},
					new KPIValueDefinition {valueName = "Grey used", valueColor = Color.yellow, unit = ValueConversionCollection.UNIT_WATT},
				}
			};

			KPICategoryDefinition sharedCategory = new KPICategoryDefinition
			{
				categoryColor = Color.green,
				categoryName = "Shared",
				unit = ValueConversionCollection.UNIT_WATT,
				valueDefinitions = new[]
				{
					new KPIValueDefinition {valueName = "Green shared", valueColor = Color.green, unit = ValueConversionCollection.UNIT_WATT},
					new KPIValueDefinition {valueName = "Grey shared", valueColor = Color.green, unit = ValueConversionCollection.UNIT_WATT},
				}
			};

			KPICategoryDefinition wastedCategory = new KPICategoryDefinition
			{
				categoryColor = Color.green,
				categoryName = "Wasted",
				unit = ValueConversionCollection.UNIT_WATT,
				valueDefinitions = new[]
				{
					new KPIValueDefinition {valueName = "Green wasted", valueColor = Color.green, unit = ValueConversionCollection.UNIT_WATT},
					new KPIValueDefinition {valueName = "Grey wasted", valueColor = Color.green, unit = ValueConversionCollection.UNIT_WATT},
				}
			};

			KPICategoryDefinition areaCategory = new KPICategoryDefinition
			{
				categoryColor = Color.blue,
				categoryName = "Area",
				unit = "KM2",
				valueDefinitions = new KPIValueDefinition[EnergyKPI.allEnergyTypes.Length]
			};

			KPICategoryDefinition investmentCategory = new KPICategoryDefinition
			{
				categoryColor = Color.cyan,
				categoryName = "Investment",
				unit = "€/MWh",
				valueDefinitions = new KPIValueDefinition[EnergyKPI.allEnergyTypes.Length]
			};

			for (int i = 0; i < EnergyKPI.allEnergyTypes.Length; ++i)
			{
				EnergyKPI.EnergyType type = EnergyKPI.allEnergyTypes[i];

				areaCategory.valueDefinitions[i] = new KPIValueDefinition { valueName = type + " area", valueColor = Color.blue, unit = "KM2" };
				investmentCategory.valueDefinitions[i] = new KPIValueDefinition { valueName = type + " investment", valueColor = Color.cyan, unit = "€/MWh" };
			}

            energyKPIs.SetupKPIValues(new[] { productionCategory, usedCategory, sharedCategory, wastedCategory, areaCategory, investmentCategory }, Main.MspGlobalData.session_end_month);			
		}
	}

	public static void CreateEcologyKPIs(JObject melConfig)
	{
		if (Main.IsSimulationConfigured(ESimulationType.MEL))
		{
			KPICategoryDefinition[] categoryDefinitions = melConfig["ecologyCategories"].ToObject<KPICategoryDefinition[]>();
			ecologyKPI = new KPIValueCollection();
			ecologyKPI.SetupKPIValues(categoryDefinitions, Main.MspGlobalData.session_end_month);
			ecologyKPI.OnKPIValuesReceivedAndProcessed += OnEcologyKPIReceivedNewMonth;
		}
	}

	internal static void CreateShippingKPIBars(KPICategoryDefinition[] categories)
	{
		if (Main.IsSimulationConfigured(ESimulationType.SEL))
		{
			shippingKPI = new CountryKPICollectionShipping();
			shippingKPI.SetupKPIValues(categories, Main.MspGlobalData.session_end_month);
		}
	}

	public static void ReceiveEcologyKPIUpdate(EcologyKPIObject[] objects)
	{
		if (Main.IsSimulationConfigured(ESimulationType.MEL))
		{
			ecologyKPI.ProcessReceivedKPIData(objects);
		}
	}

	private static void OnEcologyKPIReceivedNewMonth(KPIValueCollection valueCollection, int previousMostRecentMonth, int mostRecentMonth)
	{
		foreach (AbstractLayer layer in LayerManager.protectedAreaLayers)
		{
			LayerState state = layer.GetLayerStateAtTime(previousMostRecentMonth);
			for (int i = previousMostRecentMonth + 1; i <= mostRecentMonth; ++i)
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
					valueCollection.TryUpdateKPIValue(sizeForEntityType.Key.Name, i, sizeForEntityType.Value);
				}
			}
		}

		//Todo move this to it's own MonoBehaviour and trigger this OnMonthAdvanced.
		KPIRoot ecologyKPIRoot = InterfaceCanvas.Instance.KPIEcology;
		ecologyKPIRoot.groups.SetBarsToFishing(PlanManager.GetFishingDistributionAtTime(mostRecentMonth));
	}

	public static void ReceiveEnergyKPIUpdate(EnergyKPIObject[] updateData)
	{
        energyKPIs.ProcessReceivedKPIEnergyData(updateData);		
	}

	public static void ReceiveShippingKPIUpdate(EcologyKPIObject[] shippingData)
	{
		if (shippingKPI != null)
		{
			shippingKPI.ProcessReceivedKPIData(shippingData);
		}
	}

	public static KPIValueCollection GetKPIValuesForCategory(EKPICategory targetCategory, int countryId = -1)
	{
		KPIValueCollection result = null;
		switch (targetCategory)
		{
		case EKPICategory.Ecology:
			result = ecologyKPI;
			break;
		case EKPICategory.Energy:
            result = energyKPIs.GetKPIForCountry(countryId);
			break;
		case EKPICategory.Shipping:
			if (shippingKPI != null)
			{
				result = shippingKPI.GetKPIForCountry(countryId);
			}
			break;
		case EKPICategory.Geometry:
			result = geometryKPIs.GetKPIForCountry(countryId);
			break;
		default:
			Debug.LogError("Unimplemented KPI Category for GetKPIValuesForCategory " + targetCategory);
			break;
		}

		return result;
	}

	private static void CreateGeometryKPI()
	{
        foreach (Team team in TeamManager.GetTeams())
        {
            if (!team.IsManager)
            {
                geometryKPIs.AddKPIForCountry(team.ID);
            }
        }
        //Collection for all countries together
        geometryKPIs.AddKPIForCountry(0);
        geometryKPIs.SetupKPIValues(null, Main.MspGlobalData.session_end_month);
		GameState.OnCurrentMonthChanged += UpdateGeometryKPI;
	}

	private static void UpdateGeometryKPI(int oldMonth, int newMonth)
	{
		geometryKPIs.CalculateKPIValues(newMonth);
	}
}