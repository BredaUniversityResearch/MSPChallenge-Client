using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	class ShippingLineStringEntity : LineStringEntity
	{
		public const string ShippingDirectionMetaKey = "ShippingDirection";
		public const string ShippingLaneWidthMetaKey = "ShippingWidth";

		public const string DirectionDefault = DirectionBidirectional;
		private const string DirectionForward = "Forward";
		private const string DirectionReverse = "Reverse";
		private const string DirectionBidirectional = "Bidirectional";

		public static string CycleToNextDirection(string a_direction)
		{
			string result;
			if (a_direction == DirectionBidirectional)
			{
				result = DirectionForward;
			}
			else if (a_direction == DirectionForward)
			{
				result = DirectionReverse;
			}
			else
			{
				result = DirectionBidirectional;
			}
			return result;
		}

		public ShippingLineStringEntity(LineStringLayer a_layer, SubEntityObject a_layerObject) 
			: base(a_layer, a_layerObject)
		{
		}

		public ShippingLineStringEntity(LineStringLayer a_layer, PlanLayer a_planLayer, List<EntityType> a_entityType)
			: base(a_layer, a_planLayer, a_entityType)
		{
		}

		public override void OverrideDrawSettings(SubEntityDrawMode a_drawMode, ref SubEntityDrawSettings a_settings, ref bool a_meshDirtyFromOverride)
		{
			base.OverrideDrawSettings(a_drawMode, ref a_settings, ref a_meshDirtyFromOverride);

			a_settings = a_settings.GetClone();
			SetShippingDirectionIcon(a_drawMode, a_settings);
			SetShippingLineIconCount(a_drawMode, a_settings);
			a_settings.FixedWidth = true;

            EntityPropertyMetaData propertyMeta = Layer.FindPropertyMetaDataByName(ShippingLaneWidthMetaKey);
            float defaultValue = 1f;
            if(propertyMeta != null)
            {
                defaultValue = Util.ParseToFloat(propertyMeta.DefaultValue, 1f);
            }
            if (DoesPropertyExist(ShippingLaneWidthMetaKey))
			{
				a_settings.LineWidth = Util.ParseToFloat(GetMetaData(ShippingLaneWidthMetaKey), defaultValue);
			}
			else
			{
				a_settings.LineWidth = defaultValue;
			}

			//Force recreate of line string so the icons get drawn correctly.
			a_meshDirtyFromOverride = true;
		}

		private void SetShippingDirectionIcon(SubEntityDrawMode a_drawMode, SubEntityDrawSettings a_settings)
		{
			if (DoesPropertyExist(ShippingDirectionMetaKey))
			{
				string directionMetaData = GetMetaData(ShippingDirectionMetaKey);
				if (directionMetaData == DirectionBidirectional)
				{
					a_settings.LineIcon = null; //No icon.
				}
				else if (directionMetaData == DirectionForward)
				{
					a_settings.LineIcon = "ShippingLaneUnidirectional";
				}
				else if (directionMetaData == DirectionReverse)
				{
					a_settings.LineIcon = "ShippingLaneUnidirectionalReverse";
				}
				else
				{
					Debug.Log("Could associate direction " + directionMetaData + " with a valid icon");
				}

				//settings.LineIconColor = Main.Instance.SelConfig.directionality_icon_color;
				if(SimulationManager.Instance.TryGetSettings(SimulationManager.SEL_SIM_NAME, out var selSettings))
				{
					a_settings.LineIconColor = ((SimulationSettingsSEL)selSettings).directionality_icon_color;
				}
			}
			else
			{
				a_settings.LineIcon = null;
			}
		}

		private void SetShippingLineIconCount(SubEntityDrawMode a_drawMode, SubEntityDrawSettings a_settings)
		{
			if (a_drawMode == SubEntityDrawMode.Selected || a_drawMode == SubEntityDrawMode.Hover)
			{
				//Draw one icon for each segment please.
				a_settings.LineIconCount = -1;
			}
			else
			{
				a_settings.LineIconCount = 1;
			}
		}
	}
}
