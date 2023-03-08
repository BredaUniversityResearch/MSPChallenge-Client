using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
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

		protected override Entity CreateEntity(PlanLayer planLayer, List<EntityType> entityType)
		{
			return new ShippingLineStringEntity(this, planLayer, entityType);
		}

        public override SubEntity GetSubEntityAt(Vector2 position)
		{
			float defaultWidth = 1.0f;
			EntityPropertyMetaData laneWidthMetaData = FindPropertyMetaDataByName(ShippingLineStringEntity.ShippingLaneWidthMetaKey);
			if (laneWidthMetaData != null)
			{
				defaultWidth = Util.ParseToFloat(laneWidthMetaData.DefaultValue, 1.0f);
			}

            SubEntity result = null;
            float closestDistance = float.MaxValue;

            float maxDistance = VisualizationUtil.Instance.GetSelectMaxDistance();
            Rect positionBounds = new Rect(position - Vector2.one * maxDistance, Vector2.one * maxDistance * 2);

            foreach (LineStringEntity entity in m_activeEntities)
            {
                List<LineStringSubEntity> subEntities = entity.GetSubEntities();
				foreach (LineStringSubEntity subEntity in subEntities)
				{
					if (subEntity.PlanState != SubEntityPlanState.NotShown && positionBounds.Overlaps(subEntity.m_boundingBox))
					{
						//This is not particularly performant, but I blame designers.
						//Despite insistence on shipping lane width being visual only, it turned out not to be.....

						float width = defaultWidth;
						if (subEntity.m_entity.DoesPropertyExist(ShippingLineStringEntity.ShippingLaneWidthMetaKey))
							width = Util.ParseToFloat(subEntity.m_entity.GetMetaData(ShippingLineStringEntity.ShippingLaneWidthMetaKey), defaultWidth);
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
