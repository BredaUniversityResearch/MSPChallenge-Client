using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicCEL : ASimulationLogic
	{
		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateCEL data = (SimulationUpdateCEL)a_data;
			KPIManager.Instance.ReceiveEnergyKPIUpdate(data.kpi);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			//Currently in Server.GetCELConfig()

			SimulationSettingsCEL config = (SimulationSettingsCEL)a_settings;
			Sprite greenSprite = config.green_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.green_centerpoint_sprite);
			Sprite greySprite = config.grey_centerpoint_sprite == null ? null : Resources.Load<Sprite>(AbstractLayer.POINT_SPRITE_ROOT_FOLDER + config.grey_centerpoint_sprite);
			Color greenColor = Util.HexToColor(config.green_centerpoint_color);
			Color greyColor = Util.HexToColor(config.grey_centerpoint_color);

			foreach (PointLayer layer in LayerManager.Instance.GetCenterPointLayers())
			{
				layer.EntityTypes[0].DrawSettings.PointColor = layer.greenEnergy ? greenColor : greyColor;
				layer.EntityTypes[0].DrawSettings.PointSprite = layer.greenEnergy ? greenSprite : greySprite;
				layer.EntityTypes[0].DrawSettings.PointSize = layer.greenEnergy ? config.green_centerpoint_size : config.grey_centerpoint_size;
			}
		}

	}
}