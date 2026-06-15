using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
    class CountryKPICollectionEnergy : CountryKPICollection<KPIValueCollectionEnergy>
    {

        public void ProcessReceivedKPIEnergyData(KPIObjectEnergy[] updateData)
        {
            // <month, <grid_id, grid_data>>
            Dictionary<int, Dictionary<int, GridActualAndWasted>> parsedUpdateData = new Dictionary<int, Dictionary<int, GridActualAndWasted>>();

            //Parse data into a more convenient format.
            foreach (KPIObjectEnergy data in updateData)
            {
                Dictionary<int, GridActualAndWasted> monthData;
                if (parsedUpdateData.TryGetValue(data.month, out monthData))
                {
                    GridActualAndWasted currentGridData;
                    if (monthData.TryGetValue(data.grid, out currentGridData))
                    {
                        if (currentGridData.m_socketActual.ContainsKey(data.country))
                            currentGridData.m_socketActual[data.country] += data.actual;
                        else
                            currentGridData.m_socketActual.Add(data.country, data.actual);
                        currentGridData.m_totalReceived += data.actual;
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
                foreach(KeyValuePair<int, KPIValueCollectionEnergy> kvp in KPIsPerCountry)
                    kvp.Value.UpdateKPIValuesForMonth(lowestKey, parsedUpdateData[lowestKey]);
                parsedUpdateData.Remove(lowestKey);

                highestMonthProcessed = lowestKey;
            }

            //Notify all KPIs we have finished updating
            if (highestMonthProcessed != -1)
            {
                foreach (KeyValuePair<int, KPIValueCollectionEnergy> kvp in KPIsPerCountry)
                    kvp.Value.FinishedUpdatingKPI(highestMonthProcessed);
				EnergyGridReceivedEvent.Invoke();
			}
        }

        void AssignActualAndWastedToGrids(Dictionary<int, GridActualAndWasted> gridDataForMonth)
        {
            foreach (KeyValuePair<int, GridActualAndWasted> gridData in gridDataForMonth)
            {
                EnergyGrid associatedGrid = PolicyLogicEnergy.Instance.GetEnergyGrid(gridData.Key);
                gridData.Value.m_wasted = associatedGrid.AvailablePower - gridData.Value.m_totalReceived;

                //Make socket power negative if it has been sent
                foreach (KeyValuePair<int, CountryEnergyAmount> kvp in associatedGrid.m_energyDistribution.m_distribution)
                {
                    if (kvp.Value.m_expected < 0)
                        gridData.Value.m_socketActual[kvp.Key] = -gridData.Value.m_socketActual[kvp.Key];
                }

                //Determine the actual sourcepower that was used
                foreach (EnergyPointSubEntity source in associatedGrid.m_sources)
                {
                    if (gridData.Value.m_sourceActual.ContainsKey(source.m_entity.Country))
                        gridData.Value.m_sourceActual[source.m_entity.Country] += source.UsedCapacity;
                    else
                        gridData.Value.m_sourceActual.Add(source.m_entity.Country, source.UsedCapacity);
                }

                //Assign the actual and wasted values to its grid. This will be overwritten in later updates.
                associatedGrid.m_actualAndWasted = gridData.Value;
            }
        }
    }
}
