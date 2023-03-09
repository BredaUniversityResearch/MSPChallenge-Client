using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	class ShippingLineStringEntity : LineStringEntity
	{
		public const string SHIPPING_DIRECTION_META_KEY = "ShippingDirection";
		public const string SHIPPING_LANE_WIDTH_META_KEY = "ShippingWidth";

		public const string DIRECTION_DEFAULT = DIRECTION_BIDIRECTIONAL;
		private const string DIRECTION_FORWARD = "Forward";
		private const string DIRECTION_REVERSE = "Reverse";
		private const string DIRECTION_BIDIRECTIONAL = "Bidirectional";

		public static string CycleToNextDirection(string a_direction)
		{
			string result;
			if (a_direction == DIRECTION_BIDIRECTIONAL)
			{
				result = DIRECTION_FORWARD;
			}
			else if (a_direction == DIRECTION_FORWARD)
			{
				result = DIRECTION_REVERSE;
			}
			else
			{
				result = DIRECTION_BIDIRECTIONAL;
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

            EntityPropertyMetaData propertyMeta = Layer.FindPropertyMetaDataByName(SHIPPING_LANE_WIDTH_META_KEY);
            float defaultValue = 1f;
            if(propertyMeta != null)
            {
                defaultValue = Util.ParseToFloat(propertyMeta.DefaultValue, 1f);
            }
            if (DoesPropertyExist(SHIPPING_LANE_WIDTH_META_KEY))
			{
				a_settings.LineWidth = Util.ParseToFloat(GetMetaData(SHIPPING_LANE_WIDTH_META_KEY), defaultValue);
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
			if (DoesPropertyExist(SHIPPING_DIRECTION_META_KEY))
			{
				string directionMetaData = GetMetaData(SHIPPING_DIRECTION_META_KEY);
				if (directionMetaData == DIRECTION_BIDIRECTIONAL)
				{
					a_settings.LineIcon = null; //No icon.
				}
				else if (directionMetaData == DIRECTION_FORWARD)
				{
					a_settings.LineIcon = "ShippingLaneUnidirectional";
				}
				else if (directionMetaData == DIRECTION_REVERSE)
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
