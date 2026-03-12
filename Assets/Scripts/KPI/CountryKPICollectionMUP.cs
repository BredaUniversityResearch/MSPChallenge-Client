using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
    class CountryKPICollectionMUP: CountryKPICollection<KPIValueCollectionMUP>
	{
        private int mostRecentMonth = -1;
		PolygonLayer m_MUPLayer;

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

            m_MUPLayer = (PolygonLayer)LayerManager.Instance.GetLayerByUniqueTags(new string[] { "Polygon", "Electricity", "Production", "MultiUse" });
            if (m_MUPLayer == null)
            {
                Debug.Log($"Missing multi-use platform layer, no layers found with tags \"Polygon\", \"Electricity\", \"Production\" and \"MultiUse\". Multi-use KPIs will not be calculated.");
                return;
            }

            KPICategoryDefinition layerCategory = new KPICategoryDefinition
            {
                categoryName = SimulationManager.MultiUse_KPI_NAME,
                categoryDisplayName = "Multi-use",
                categoryColor = m_MUPLayer.m_entityTypes[0].DrawSettings.PolygonColor,
                categoryValueType = EKPICategoryValueType.Manual,
                unit = "km<sup>2</sup>",
                valueDefinitions = new KPIValueDefinition[] { new KPIValueDefinition()
                {
                    valueName = KPIValueCollectionMUP.MUP_SAVED_KPI_NAME,
                    valueDisplayName = "Multi-use platform area saved",
                    unit = "km<sup>2</sup>",
                    valueColor = m_MUPLayer.m_entityTypes[0].DrawSettings.PolygonColor
                }}
			};

            KPICategoryDefinition[] layerCategoryArray = new KPICategoryDefinition[] { layerCategory };

            foreach (var kvp in KPIsPerCountry)
            {
                kvp.Value.SetupKPIValues(layerCategoryArray, numberOfKpiMonths);
            }
        }

        public void CalculateKPIValues(int newMonth)
        {
            if (m_MUPLayer == null)
                return;

            LayerState state = null;
            if(m_MUPLayer != null)
				state = m_MUPLayer.GetLayerStateAtTime(mostRecentMonth);
            for (int monthId = mostRecentMonth + 1; monthId <= newMonth; ++monthId)
            {
                state.AdvanceStateToMonth(monthId);
                foreach (var kvp in KPIsPerCountry)
                    kvp.Value.UpdateLayerValues(m_MUPLayer, state, monthId);
            }

            if (newMonth > mostRecentMonth)
                mostRecentMonth = newMonth;

            foreach (var kvp in KPIsPerCountry)
                kvp.Value.KPIUpdateComplete(newMonth);
        }
    }
}
