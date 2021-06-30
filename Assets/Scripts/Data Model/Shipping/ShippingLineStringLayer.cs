using System.Collections.Generic;
using UnityEngine;

namespace Data_Model.Shipping
{
	public class ShippingLineStringLayer : LineStringLayer
	{
		public ShippingLineStringLayer(LayerMeta layerMeta, List<SubEntityObject> layerObjects) 
			: base(layerMeta, layerObjects)
		{
		}

		public override Entity CreateEntity(SubEntityObject obj)
		{
			return new ShippingLineStringEntity(this, obj);
		}

		public override Entity CreateEntity(PlanLayer planLayer, List<EntityType> entityType)
		{
			return new ShippingLineStringEntity(this, planLayer, entityType);
		}

        public override SubEntity GetSubEntityAt(Vector2 position)
		{
			float defaultWidth = 1.0f;
			EntityPropertyMetaData laneWidthMetaData = FindPropertyMetaDataByName(ShippingLineStringEntity.SHIPPING_LANE_WIDTH_META_KEY);
			if (laneWidthMetaData != null)
			{
				defaultWidth = Util.ParseToFloat(laneWidthMetaData.DefaultValue, 1.0f);
			}

            SubEntity result = null;
            float closestDistance = float.MaxValue;

            float maxDistance = VisualizationUtil.GetSelectMaxDistance();
            Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

            foreach (LineStringEntity entity in activeEntities)
            {
                List<LineStringSubEntity> subEntities = entity.GetSubEntities();
				foreach (LineStringSubEntity subEntity in subEntities)
				{
					if (subEntity.planState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.BoundingBox))
					{
						//This is not particularly performant, but I blame designers.
						//Despite insistence on shipping lane width being visual only, it turned out not to be.....

						float width = defaultWidth;
						if (subEntity.Entity.DoesPropertyExist(ShippingLineStringEntity.SHIPPING_LANE_WIDTH_META_KEY))
							width = Util.ParseToFloat(subEntity.Entity.GetMetaData(ShippingLineStringEntity.SHIPPING_LANE_WIDTH_META_KEY), defaultWidth);
						width *= 0.5f;
						float dist = subEntity.DistanceToPoint(position) - width;
						if (dist < 0)
							return subEntity;
						if (dist < closestDistance)
						{
							result = subEntity;
							closestDistance = dist;
						}
					}
				}
			}

            //None found close enough
			if (closestDistance > maxDistance)
			{
				return null;
			}

			return result;
        }
    }
}
