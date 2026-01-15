using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class KPIValueCollectionMUP: KPIValueCollection
	{
		public void KPIUpdateComplete(int newMonth)
		{
			OnNewKpiDataReceived(newMonth);
		}

		public void UpdateLayerValues(PolygonLayer layer, LayerState state, int month)
		{
			//TODO: Make specific to MUP, get geometry policy to figure out uses
			float total = 0;
			Dictionary<EntityType, float> totalByEntityType = new Dictionary<EntityType, float>(layer.m_entityTypes.Count);
			foreach (EntityType type in layer.m_entityTypes.Values)
			{
				totalByEntityType.Add(type, 0.0f);
			}
			
			foreach (Entity entity in state.baseGeometry)
            {
                if (countryId != 0 && entity.Country != countryId)
                    continue;

				PolygonSubEntity subEntity = (PolygonSubEntity)entity.GetSubEntity(0);
				float subEntityTotal = subEntity.SurfaceAreaSqrKm; ;

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
