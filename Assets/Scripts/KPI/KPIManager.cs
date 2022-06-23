using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPIManager : MonoBehaviour
	{
		private static KPIManager singleton;
		public static KPIManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<KPIManager>();
				return singleton;
			}
		}

		private KPIValueCollection ecologyKPI;
		private CountryKPICollectionShipping shippingKPI;
		private CountryKPICollectionEnergy energyKPIs = new CountryKPICollectionEnergy();
		private CountryKPICollectionGeometry geometryKPIs = new CountryKPICollectionGeometry();

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
			Main.OnFinishedLoadingLayers += CreateGeometryKPI;
		}

		void OnDestroy()
		{
			singleton = null;
		}

		public void CreateEnergyKPIs()
		{
			if (Main.IsSimulationConfigured(ESimulationType.CEL))
			{
				//Initialise KPIs
				foreach (Team team in SessionManager.Instance.GetTeams())
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
					unit = "km<sup>2</sup>",
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

					areaCategory.valueDefinitions[i] = new KPIValueDefinition { valueName = type + " area", valueColor = Color.blue, unit = "km<sup>2</sup>" };
					investmentCategory.valueDefinitions[i] = new KPIValueDefinition { valueName = type + " investment", valueColor = Color.cyan, unit = "€/MWh" };
				}

				energyKPIs.SetupKPIValues(new[] { productionCategory, usedCategory, sharedCategory, wastedCategory, areaCategory, investmentCategory }, SessionManager.Instance.MspGlobalData.session_end_month);			
			}
		}

		public void CreateEcologyKPIs(JObject melConfig)
		{
			if (Main.IsSimulationConfigured(ESimulationType.MEL))
			{
				KPICategoryDefinition[] categoryDefinitions = melConfig["ecologyCategories"].ToObject<KPICategoryDefinition[]>();
				ecologyKPI = new KPIValueCollection();
				ecologyKPI.SetupKPIValues(categoryDefinitions, SessionManager.Instance.MspGlobalData.session_end_month);
				ecologyKPI.OnKPIValuesReceivedAndProcessed += OnEcologyKPIReceivedNewMonth;
			}
		}

		internal void CreateShippingKPIBars(KPICategoryDefinition[] categories)
		{
			if (Main.IsSimulationConfigured(ESimulationType.SEL))
			{
				shippingKPI = new CountryKPICollectionShipping();
				shippingKPI.SetupKPIValues(categories, SessionManager.Instance.MspGlobalData.session_end_month);
			}
		}

		public void ReceiveEcologyKPIUpdate(EcologyKPIObject[] objects)
		{
			if (Main.IsSimulationConfigured(ESimulationType.MEL))
			{
				ecologyKPI.ProcessReceivedKPIData(objects);
			}
		}

		private void OnEcologyKPIReceivedNewMonth(KPIValueCollection valueCollection, int previousMostRecentMonth, int mostRecentMonth)
		{
			foreach (AbstractLayer layer in LayerManager.Instance.protectedAreaLayers)
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
			ecologyKPIRoot.groups.SetBarsToFishing(PlanManager.Instance.GetFishingDistributionAtTime(mostRecentMonth));
		}

		public void ReceiveEnergyKPIUpdate(EnergyKPIObject[] updateData)
		{
			energyKPIs.ProcessReceivedKPIEnergyData(updateData);		
		}

		public void ReceiveShippingKPIUpdate(EcologyKPIObject[] shippingData)
		{
			if (shippingKPI != null)
			{
				shippingKPI.ProcessReceivedKPIData(shippingData);
			}
		}

		public KPIValueCollection GetKPIValuesForCategory(EKPICategory targetCategory, int countryId = -1)
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

		private void CreateGeometryKPI()
		{
			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (!team.IsManager)
				{
					geometryKPIs.AddKPIForCountry(team.ID);
				}
			}
			//Collection for all countries together
			geometryKPIs.AddKPIForCountry(0);
			geometryKPIs.SetupKPIValues(null, SessionManager.Instance.MspGlobalData.session_end_month);
			TimeManager.Instance.OnCurrentMonthChanged += UpdateGeometryKPI;
		}

		private void UpdateGeometryKPI(int oldMonth, int newMonth)
		{
			geometryKPIs.CalculateKPIValues(newMonth);
		}
	}
}