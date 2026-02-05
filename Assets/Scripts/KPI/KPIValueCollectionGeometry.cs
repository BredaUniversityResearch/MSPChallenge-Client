using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class KPIValueCollectionGeometry: KPIValueCollection
	{
		public void KPIUpdateComplete(int newMonth)
		{
			OnNewKpiDataReceived(newMonth);
		}

		public void UpdateLayerValues(AbstractLayer layer, LayerState state, int month)
		{
			float total = 0;
			Dictionary<EntityType, float> totalByEntityType = new Dictionary<EntityType, float>(layer.m_entityTypes.Count);
			foreach (EntityType type in layer.m_entityTypes.Values)
			{
				totalByEntityType.Add(type, 0.0f);
			}
			
			LayerManager.EGeoType layerGeoType = layer.GetGeoType();
			foreach (Entity entity in state.baseGeometry)
            {
                if (countryId != 0 && entity.Country != countryId)
                    continue;

				SubEntity subEntity = entity.GetSubEntity(0);
				float subEntityTotal;
				switch (layerGeoType)
				{
				case LayerManager.EGeoType.Polygon:
					PolygonSubEntity polygonEntity = (PolygonSubEntity)subEntity;
					subEntityTotal = polygonEntity.SurfaceAreaSqrKm;
					break;
				case LayerManager.EGeoType.Line:
					LineStringSubEntity lineEntity = (LineStringSubEntity)subEntity;
					subEntityTotal = lineEntity.LineLengthKm;
					break;
				case LayerManager.EGeoType.Point:
					subEntityTotal = 1.0f;
					break;
				default:
					UnityEngine.Debug.LogError("Unimplemented geotype " + layerGeoType + " for KPIValueCollectionGeometry for value calculation");
					subEntityTotal = 0.0f;
					break;
				}

				total += subEntityTotal;
				foreach (EntityType type in subEntity.m_entity.EntityTypes)
				{
					float entityTypeValue;
					totalByEntityType.TryGetValue(type, out entityTypeValue);
					entityTypeValue += subEntityTotal;
					totalByEntityType[type] = entityTypeValue;
				}
			}

			TryUpdateKPIValue(layer.FileName, month, total);
			foreach (KeyValuePair<EntityType, float> entityTypeValue in totalByEntityType)
			{
				TryUpdateKPIValue(CountryKPICollectionGeometry.GetKPIValueNameForEntityType(layer, entityTypeValue.Key), month, entityTypeValue.Value);
			}
		}
	}
}
