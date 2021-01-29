using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPI
{
    class CountryKPICollectionGeometry
    {
        private Dictionary<int, KPIValueCollectionGeometry> geometryKPIs; //Geometry KPIs per country id.
        private int mostRecentMonth = -1;

        public CountryKPICollectionGeometry()
        {
            geometryKPIs = new Dictionary<int, KPIValueCollectionGeometry>();
        }

        public void AddKPIForCountry(int country)
        {
            geometryKPIs.Add(country, new KPIValueCollectionGeometry(country));
        }

        public void SetupKPIValues(KPICategoryDefinition[] kpiDefinitions, int numberOfKpiMonths)
        {
            List<KPICategoryDefinition> layerCategories = new List<KPICategoryDefinition>(LayerManager.GetLayerCount());
            foreach (AbstractLayer layer in LayerManager.GetAllValidLayers())
            {
                if (layer.Editable)
                {
                    string layerUnit = GetUnitForLayerType(layer);
                    KPICategoryDefinition layerCategory = new KPICategoryDefinition
                    {
                        categoryName = layer.FileName,
                        categoryDisplayName = layer.ShortName,
                        categoryColor = layer.EntityTypes[0].DrawSettings.PolygonColor,
                        categoryValueType = EKPICategoryValueType.Manual,
                        unit = layerUnit
                    };

                    List<KPIValueDefinition> entityTypeCategories = new List<KPIValueDefinition>(layer.EntityTypes.Count);
                    foreach (KeyValuePair<int, EntityType> type in layer.EntityTypes)
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

            foreach (KeyValuePair<int, KPIValueCollectionGeometry> kvp in geometryKPIs)
            {
                kvp.Value.SetupKPIValues(layerCategoryArray, numberOfKpiMonths);
            }
        }

        public void CalculateKPIValues(int newMonth)
        {
            foreach (AbstractLayer layer in LayerManager.GetAllValidLayers())
            {
                if (layer.Editable)
                {
                    LayerState state = layer.GetLayerStateAtTime(mostRecentMonth);
                    for (int monthId = mostRecentMonth + 1; monthId <= newMonth; ++monthId)
                    {
                        state.AdvanceStateToMonth(monthId);
                        foreach(var kvp in geometryKPIs)
                            kvp.Value.UpdateLayerValues(layer, state, monthId);
                    }
                }
            }

            if(newMonth > mostRecentMonth)
                mostRecentMonth = newMonth;
            foreach (var kvp in geometryKPIs)
                kvp.Value.KPIUpdateComplete(newMonth);
        }

        public KPIValueCollectionGeometry GetKPIForCountry(int country)
        {
            if (geometryKPIs.ContainsKey(country))
                return geometryKPIs[country];
            return null;
        }

        public static string GetKPIValueNameForEntityType(AbstractLayer layer, EntityType entityType)
        {
            return string.Format("{0}/{1}", layer.FileName, entityType.Name);
        }

        public static string GetUnitForLayerType(AbstractLayer layer)
        {
			LayerManager.GeoType layerGeoType = layer.GetGeoType();

			string result;
            switch (layerGeoType)
            {
                case LayerManager.GeoType.point:
                    result = "Points";
                    break;
                case LayerManager.GeoType.line:
                    result = "km";
                    break;
                case LayerManager.GeoType.polygon:
                    result = "km2";
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
