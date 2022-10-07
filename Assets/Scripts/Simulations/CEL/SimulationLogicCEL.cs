using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicCEL : ASimulationLogic
	{
		private CountryKPICollectionEnergy m_energyKPIs = new CountryKPICollectionEnergy();
		
		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateCEL data = (SimulationUpdateCEL)a_data;
			m_energyKPIs.ProcessReceivedKPIEnergyData(data.kpi);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			//Currently in Server.GetCELConfig()
			CreateEnergyKPIs();

			SimulationSettingsCEL config = (SimulationSettingsCEL)a_settings;
			Sprite greenSprite = config.green_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.green_centerpoint_sprite);
			Sprite greySprite = config.grey_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.grey_centerpoint_sprite);
			Color greenColor = Util.HexToColor(config.green_centerpoint_color);
			Color greyColor = Util.HexToColor(config.grey_centerpoint_color);

			foreach (PointLayer layer in PolicyLogicEnergy.Instance.GetCenterPointLayers())
			{
				layer.EntityTypes[0].DrawSettings.PointColor = layer.greenEnergy ? greenColor : greyColor;
				layer.EntityTypes[0].DrawSettings.PointSprite = layer.greenEnergy ? greenSprite : greySprite;
				layer.EntityTypes[0].DrawSettings.PointSize = layer.greenEnergy ? config.green_centerpoint_size : config.grey_centerpoint_size;
			}
		}

		public override void Destroy()
		{ }

		public void CreateEnergyKPIs()
		{
			//Initialise KPIs
			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (!team.IsManager)
				{
					m_energyKPIs.AddKPIForCountry(team.ID);
				}
			}
			//Collection for all countries together
			m_energyKPIs.AddKPIForCountry(0);

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

			m_energyKPIs.SetupKPIValues(new[] { productionCategory, usedCategory, sharedCategory, wastedCategory, areaCategory, investmentCategory }, SessionManager.Instance.MspGlobalData.session_end_month);

		}

		public override KPIValueCollection GetKPIValuesForCountry(int a_countryId = -1)
		{
			return m_energyKPIs.GetKPIForCountry(a_countryId);
		}
	}
}