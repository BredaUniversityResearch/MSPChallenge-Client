using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data_Model.Shipping
{
	class ShippingLineStringEntity : LineStringEntity
	{
		public const string SHIPPING_DIRECTION_META_KEY = "ShippingDirection";
		public const string SHIPPING_LANE_WIDTH_META_KEY = "ShippingWidth";

		public const string DIRECTION_DEFAULT = DIRECTION_BIDIRECTIONAL;
		private const string DIRECTION_FORWARD = "Forward";
		private const string DIRECTION_REVERSE = "Reverse";
		private const string DIRECTION_BIDIRECTIONAL = "Bidirectional";

		public static string CycleToNextDirection(string direction)
		{
			string result;
			if (direction == DIRECTION_BIDIRECTIONAL)
			{
				result = DIRECTION_FORWARD;
			}
			else if (direction == DIRECTION_FORWARD)
			{
				result = DIRECTION_REVERSE;
			}
			else
			{
				result = DIRECTION_BIDIRECTIONAL;
			}
			return result;
		}

		public ShippingLineStringEntity(LineStringLayer layer, SubEntityObject layerObject) 
			: base(layer, layerObject)
		{
		}

		public ShippingLineStringEntity(LineStringLayer layer, PlanLayer planLayer, List<EntityType> entityType)
			: base(layer, planLayer, entityType)
		{
		}

		public override void OverrideDrawSettings(SubEntityDrawMode drawMode, ref SubEntityDrawSettings settings, ref bool meshDirtyFromOverride)
		{
			base.OverrideDrawSettings(drawMode, ref settings, ref meshDirtyFromOverride);

			settings = settings.GetClone();
			SetShippingDirectionIcon(drawMode, settings);
			SetShippingLineIconCount(drawMode, settings);
			settings.FixedWidth = true;

            EntityPropertyMetaData propertyMeta = Layer.FindPropertyMetaDataByName(SHIPPING_LANE_WIDTH_META_KEY);
            float defaultValue = 1f;
            if(propertyMeta != null)
            {
                defaultValue = Util.ParseToFloat(propertyMeta.DefaultValue, 1f);
            }
            if (DoesPropertyExist(SHIPPING_LANE_WIDTH_META_KEY))
			{
				settings.LineWidth = Util.ParseToFloat(GetMetaData(SHIPPING_LANE_WIDTH_META_KEY), defaultValue);
			}
			else
			{
				settings.LineWidth = defaultValue;
			}

			//Force recreate of line string so the icons get drawn correctly.
			meshDirtyFromOverride = true;
		}

		private void SetShippingDirectionIcon(SubEntityDrawMode drawMode, SubEntityDrawSettings settings)
		{
			if (DoesPropertyExist(SHIPPING_DIRECTION_META_KEY))
			{
				string directionMetaData = GetMetaData(SHIPPING_DIRECTION_META_KEY);
				if (directionMetaData == DIRECTION_BIDIRECTIONAL)
				{
					settings.LineIcon = null; //No icon.
				}
				else if (directionMetaData == DIRECTION_FORWARD)
				{
					settings.LineIcon = "ShippingLaneUnidirectional";
				}
				else if (directionMetaData == DIRECTION_REVERSE)
				{
					settings.LineIcon = "ShippingLaneUnidirectionalReverse";
				}
				else
				{
					Debug.Log("Could associate direction " + directionMetaData + " with a valid icon");
				}

				settings.LineIconColor = Main.SelConfig.directionality_icon_color;
			}
			else
			{
				settings.LineIcon = null;
			}
		}

		private void SetShippingLineIconCount(SubEntityDrawMode drawMode, SubEntityDrawSettings settings)
		{
			if (drawMode == SubEntityDrawMode.Selected || drawMode == SubEntityDrawMode.Hover)
			{
				//Draw one icon for each segment please.
				settings.LineIconCount = -1;
			}
			else
			{
				settings.LineIconCount = 1;
			}
		}
	}
}
