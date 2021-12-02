using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPI
{
    class CountryKPICollectionEnergy
    {
        private Dictionary<int, KPIValueCollectionEnergy> energyKPIs; //Energy KPIs per country id.

        public CountryKPICollectionEnergy()
        {
            energyKPIs = new Dictionary<int, KPIValueCollectionEnergy>();
        }

        public void AddKPIForCountry(int country)
        {
            energyKPIs.Add(country, new KPIValueCollectionEnergy(country));
        }

        public void SetupKPIValues(KPICategoryDefinition[] kpiDefinitions, int numberOfKpiMonths)
        {
            foreach (KeyValuePair<int, KPIValueCollectionEnergy> kvp in energyKPIs)
            {
                kvp.Value.SetupKPIValues(kpiDefinitions, numberOfKpiMonths);
            }
        }

        public void ProcessReceivedKPIEnergyData(EnergyKPIObject[] updateData)
        {
            // <month, <grid_id, grid_data>>
            Dictionary<int, Dictionary<int, GridActualAndWasted>> parsedUpdateData = new Dictionary<int, Dictionary<int, GridActualAndWasted>>();

            //Parse data into a more convenient format.
            foreach (EnergyKPIObject data in updateData)
            {
                Dictionary<int, GridActualAndWasted> monthData;
                if (parsedUpdateData.TryGetValue(data.month, out monthData))
                {
                    GridActualAndWasted currentGridData;
                    if (monthData.TryGetValue(data.grid, out currentGridData))
                    {
                        if (currentGridData.socketActual.ContainsKey(data.country))
                            currentGridData.socketActual[data.country] += data.actual;
                        else
                            currentGridData.socketActual.Add(data.country, data.actual);
                        currentGridData.totalReceived += data.actual;
                    }
                    else
                    {
                        monthData.Add(data.grid, new GridActualAndWasted(data.country, data.actual));
                    }
                }
                else
                {
                    parsedUpdateData.Add(data.month, new Dictionary<int, GridActualAndWasted> { { data.grid, new GridActualAndWasted(data.country, data.actual) } });
                }
            }

            int highestMonthProcessed = -1;
            while (parsedUpdateData.Count > 0)
            {
                //Find the next month for which the KPIs can be updated
                int lowestKey = int.MaxValue;
                foreach (int key in parsedUpdateData.Keys)                
                    lowestKey = Mathf.Min(lowestKey, key);

                //Update grids to month
                AssignActualAndWastedToGrids(parsedUpdateData[lowestKey]);

                //Update KPIs for individual countries
                foreach(KeyValuePair<int, KPIValueCollectionEnergy> kvp in energyKPIs)
                    kvp.Value.UpdateKPIValuesForMonth(lowestKey, parsedUpdateData[lowestKey]);
                parsedUpdateData.Remove(lowestKey);

                highestMonthProcessed = lowestKey;
            }

            //Notify all KPIs we have finished updating
            if (highestMonthProcessed != -1)
            {
                foreach (KeyValuePair<int, KPIValueCollectionEnergy> kvp in energyKPIs)
                    kvp.Value.FinishedUpdatingKPI(highestMonthProcessed);
				EnergyGridReceivedEvent.Invoke();
			}
        }

        void AssignActualAndWastedToGrids(Dictionary<int, GridActualAndWasted> gridDataForMonth)
        {
            foreach (KeyValuePair<int, GridActualAndWasted> gridData in gridDataForMonth)
            {
                EnergyGrid associatedGrid = PlanManager.GetEnergyGrid(gridData.Key);
                gridData.Value.wasted = associatedGrid.AvailablePower - gridData.Value.totalReceived;

                //Make socket power negative if it has been sent
                foreach (KeyValuePair<int, CountryEnergyAmount> kvp in associatedGrid.energyDistribution.distribution)
                {
                    if (kvp.Value.expected < 0)
                        gridData.Value.socketActual[kvp.Key] = -gridData.Value.socketActual[kvp.Key];
                }

                //Determine the actual sourcepower that was used
                foreach (EnergyPointSubEntity source in associatedGrid.sources)
                {
                    if (gridData.Value.sourceActual.ContainsKey(source.Entity.Country))
                        gridData.Value.sourceActual[source.Entity.Country] += source.UsedCapacity;
                    else
                        gridData.Value.sourceActual.Add(source.Entity.Country, source.UsedCapacity);
                }

                //Assign the actual and wasted values to its grid. This will be overwritten in later updates.
                associatedGrid.actualAndWasted = gridData.Value;
            }
        }

        public KPIValueCollectionEnergy GetKPIForCountry(int country)
        {
            if(energyKPIs.ContainsKey(country))
                return energyKPIs[country];
            return null;
        }
    }
}
