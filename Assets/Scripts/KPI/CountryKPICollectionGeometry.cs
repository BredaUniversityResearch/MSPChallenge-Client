using System.Collections.Generic;

namespace MSP2050.Scripts
{
    class CountryKPICollectionGeometry : CountryKPICollection<KPIValueCollectionGeometry>
	{
        private int mostRecentMonth = -1;

        public override void SetupKPIValues(KPICategoryDefinition[] kpiDefinitions, int numberOfKpiMonths)
        {
			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (!team.IsManager)
				{
					AddKPIForCountry(team.ID);
				}
			}
			//Collection for all countries together
			AddKPIForCountry(0);

			List<KPICategoryDefinition> layerCategories = new List<KPICategoryDefinition>(LayerManager.Instance.GetLayerCount());
            foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
            {
                if (layer.m_editable)
                {
                    string layerUnit = GetUnitForLayerType(layer);
                    KPICategoryDefinition layerCategory = new KPICategoryDefinition
                    {
                        categoryName = layer.FileName,
                        categoryDisplayName = layer.ShortName,
                        categoryColor = layer.m_entityTypes[0].DrawSettings.PolygonColor,
                        categoryValueType = EKPICategoryValueType.Manual,
                        unit = layerUnit
                    };

                    List<KPIValueDefinition> entityTypeCategories = new List<KPIValueDefinition>(layer.m_entityTypes.Count);
                    foreach (KeyValuePair<int, EntityType> type in layer.m_entityTypes)
                    {
                        KPIValueDefinition value = new KPIValueDefinition
                        {
                            valueName = GetKPIValueNameForEntityType(layer, type.Value),
                            valueDisplayName = type.Value.Name,
                            unit = layerUnit,
                            valueColor = type.Value.DrawSettings.PolygonColor
                        };
                        entityTypeCategories.Add(value);
                    }

                    layerCategory.valueDefinitions = entityTypeCategories.ToArray();
                    layerCategories.Add(layerCategory);
                }
            }

            KPICategoryDefinition[] layerCategoryArray = layerCategories.ToArray();

            foreach (KeyValuePair<int, KPIValueCollectionGeometry> kvp in KPIsPerCountry)
            {
                kvp.Value.SetupKPIValues(layerCategoryArray, numberOfKpiMonths);
            }
        }

        public void CalculateKPIValues(int newMonth)
        {
            foreach (AbstractLayer layer in LayerManager.Instance.GetAllLayers())
            {
                if (layer.m_editable)
                {
                    LayerState state = layer.GetLayerStateAtTime(mostRecentMonth);
                    for (int monthId = mostRecentMonth + 1; monthId <= newMonth; ++monthId)
                    {
                        state.AdvanceStateToMonth(monthId);
                        foreach(var kvp in KPIsPerCountry)
                            kvp.Value.UpdateLayerValues(layer, state, monthId);
                    }
                }
            }

            if(newMonth > mostRecentMonth)
                mostRecentMonth = newMonth;
            foreach (var kvp in KPIsPerCountry)
                kvp.Value.KPIUpdateComplete(newMonth);
        }

        public static string GetKPIValueNameForEntityType(AbstractLayer layer, EntityType entityType)
        {
            return string.Format("{0}/{1}", layer.FileName, entityType.Name);
        }

        public static string GetUnitForLayerType(AbstractLayer layer)
        {
			LayerManager.EGeoType layerGeoType = layer.GetGeoType();

			string result;
            switch (layerGeoType)
            {
                case LayerManager.EGeoType.Point:
                    result = "Points";
                    break;
                case LayerManager.EGeoType.Line:
                    result = "km";
                    break;
                case LayerManager.EGeoType.Polygon:
                    result = "km<sup>2</sup>";
                    break;
                default:
                    UnityEngine.Debug.LogError($"Layer {layer.ShortName} cannot be used for KPI calculations. Please check this layer's geotype variable in the configuration file, and make sure the layer is not editable");
					result = "";
                    break;
            }

            return result;
        }
    }
}
