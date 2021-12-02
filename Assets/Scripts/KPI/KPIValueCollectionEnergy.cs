using System;
using System.Collections.Generic;
using UnityEngine;

namespace KPI
{
	public class KPIValueCollectionEnergy : KPIValueCollection
	{	
		private float[] investment = new float[EnergyKPI.allEnergyTypes.Length]; //Investment points of energy network of country (Cumulative cost of added/removed energy geometry) 

		public KPIValueCollectionEnergy(int countryId) : base(countryId)
		{ }

		public void UpdateKPIValuesForMonth(int month, Dictionary<int, GridActualAndWasted> gridDataForMonth)
		{
			//Keep track of layer states so we can use the area of energy layers in KPIs
			List<LayerState> layerStates = new List<LayerState>();
			foreach (AbstractLayer layer in LayerManager.energyLayers)
            {
                //Start layer state in previous month so we get the right new and removed geometry
                LayerState newLayerState = layer.GetLayerStateAtTime(month-1);
                newLayerState.AdvanceStateToMonth(month);
                layerStates.Add(newLayerState);
			}

			List<EnergyGrid> grids = PlanManager.GetEnergyGridsAtTime(month, EnergyGrid.GridColor.Either);
			CalculateKPIValues(month, gridDataForMonth, grids, layerStates);
		}

        public void FinishedUpdatingKPI(int finalMonth)
        {
            OnNewKpiDataReceived(finalMonth);
        }

		private void CalculateKPIValues(int month, Dictionary<int, GridActualAndWasted> gridActual, List<EnergyGrid> grids, List<LayerState> layerStates)
		{
			//Reset values
			float[] area = new float[EnergyKPI.allEnergyTypes.Length];
			long productionGreen = 0;
			long productionGrey = 0;
			long usedGreen = 0;
			long usedGrey = 0;
			long sharedGreen = 0;
			long sharedGrey = 0;
			long wastedGreen = 0;
			long wastedGrey = 0;
			float areaTotal = 0;
			float investmentTotal = 0;

			Dictionary<int, List<DirectionalConnection>> greenNetwork = null;
			Dictionary<int, List<DirectionalConnection>> greyNetwork = null;

			//Get state of all energy layers at current time
			foreach (LayerState state in layerStates)
			{
				//Go through all geometry
				foreach (Entity t in state.baseGeometry)
				{
					//If geometry belongs to this country
					if (countryId == 0 || t.Country == countryId)
					{
						//Calculate area 
						float restrictionArea = t.GetRestrictionAreaSurface();
						area[(int)GetEntityEnergyType(t)] += restrictionArea;
						areaTotal += restrictionArea;
					}
				}

				if (state.newGeometry != null)
					foreach (Entity t in state.newGeometry)
						if (countryId == 0 || t.Country == countryId)
						{
							float cost = t.GetInvestmentCost();
							investment[(int)GetEntityEnergyType(t)] += cost;
							investmentTotal += cost;
						}

				if (state.removedGeometry != null)
					foreach (Entity t in state.removedGeometry)
						if (countryId == 0 || t.Country == countryId)
						{
							float cost = t.GetInvestmentCost();
							investment[(int)GetEntityEnergyType(t)] += cost;
							investmentTotal += cost;
						}

				if (state.layer.IsEnergyLineLayer())
				{
					if (state.layer.greenEnergy)
						greenNetwork = state.GetCableNetworkForState();
					else
						greyNetwork = state.GetCableNetworkForState();
				}
			}

			//For each source belonging to us, add capacity to grey or green production
			//For each active grid, get what we produce, receive and what was wasted   
			foreach (EnergyGrid grid in grids)
			{
				int id = grid.GetDatabaseID();
                //All countries
                if (countryId == 0)
                {
                    if (gridActual.ContainsKey(id))
                    {
                        if (grid.IsGreen)
                        {
                            foreach (var kvp in gridActual[id].socketActual)
                            {
                                usedGreen += kvp.Value; //could be a negative number
                                long actualSourcePower = 0;
                                gridActual[id].sourceActual.TryGetValue(kvp.Key, out actualSourcePower);
                                sharedGreen += Math.Max(0, actualSourcePower - kvp.Value);
                            }
                            foreach (var kvp in grid.energyDistribution.distribution)
                            {
                                productionGreen += kvp.Value.sourceInput;
                            }
                            wastedGreen += gridActual[id].wasted;
                        }
                        else
                        {
                            foreach (var kvp in gridActual[id].socketActual)
                            {
                                usedGrey += kvp.Value; //could be a negative number
                                long actualSourcePower = 0;
                                gridActual[id].sourceActual.TryGetValue(kvp.Key, out actualSourcePower);
                                sharedGrey += Math.Max(0, actualSourcePower - kvp.Value);
                            }
                            foreach (var kvp in grid.energyDistribution.distribution)
                                productionGrey += kvp.Value.sourceInput;
                            wastedGrey += gridActual[id].wasted;
                        }
                    }
                }
                //Specific country
                else
                {
                    if (!gridActual.ContainsKey(id) || !grid.energyDistribution.distribution.ContainsKey(countryId)) //!gridActual[id].socketActual.ContainsKey(countryId))
                        continue;
                    if (grid.IsGreen)
                    {
                        long actualReceivedPower = 0; //could be a negative number
                        long actualSourcePower = 0;
                        gridActual[id].socketActual.TryGetValue(countryId, out actualReceivedPower);
                        gridActual[id].sourceActual.TryGetValue(countryId, out actualSourcePower);

                        usedGreen += actualReceivedPower; 
                        sharedGreen += Math.Max(0, actualSourcePower - actualReceivedPower);
                        productionGreen += grid.energyDistribution.distribution[countryId].sourceInput;
                        wastedGreen += gridActual[id].wasted;

                        //Set all lastRunGrid for geometry in grid
                        grid.SetAsLastRunGridForContent(greenNetwork);
                    }
                    else
                    {
                        long actualReceivedPower = 0; //could be a negative number
                        long actualSourcePower = 0;
                        gridActual[id].socketActual.TryGetValue(countryId, out actualReceivedPower);
                        gridActual[id].sourceActual.TryGetValue(countryId, out actualSourcePower);

                        usedGrey += actualReceivedPower; 
                        sharedGrey += Math.Max(0, actualSourcePower - actualReceivedPower);
                        productionGrey += grid.energyDistribution.distribution[countryId].sourceInput;
                        wastedGrey += gridActual[id].wasted;

                        //Set all lastRunGrid for geometry in grid
                        grid.SetAsLastRunGridForContent(greyNetwork);
                    }
                }
			}

			//Production
			TryUpdateKPIValue("Green production", month, productionGreen);
			TryUpdateKPIValue("Grey production", month, productionGrey);
			TryUpdateKPIValue("Production", month, productionGrey + productionGreen);

			//Used
			TryUpdateKPIValue("Green used", month, usedGreen);
			TryUpdateKPIValue("Grey used", month, usedGrey);
			TryUpdateKPIValue("Used", month, usedGreen + usedGrey);

			//Shared
			TryUpdateKPIValue("Green shared", month, sharedGreen);
			TryUpdateKPIValue("Grey shared", month, sharedGrey);
			TryUpdateKPIValue("Shared", month, sharedGreen + sharedGrey);


			//Wasted
			TryUpdateKPIValue("Green wasted", month, wastedGreen);
			TryUpdateKPIValue("Grey wasted", month, wastedGrey);
			TryUpdateKPIValue("Wasted", month, wastedGreen + wastedGrey);

			//Area
			foreach (EnergyKPI.EnergyType type in EnergyKPI.allEnergyTypes)
			{
				string name = type + " area";
				TryUpdateKPIValue(name, month, area[(int)type]);

			}
			TryUpdateKPIValue("Area", month, areaTotal);

			//Investment
			foreach (EnergyKPI.EnergyType type in EnergyKPI.allEnergyTypes)
			{
				string name = type + " investment";
				TryUpdateKPIValue(name, month, investment[(int)type]);
			}

			TryUpdateKPIValue("Investment", month, investmentTotal);

			OnNewKpiDataReceived(month);
		}

		private static EnergyKPI.EnergyType GetEntityEnergyType(Entity t)
		{
			if (!t.Layer.IsEnergyLayer())
				return EnergyKPI.EnergyType.NoEnergy;

			switch (t.Layer.editingType)
			{
				case AbstractLayer.EditingType.Cable:
					if (t.GreenEnergy)
						return EnergyKPI.EnergyType.Cables;
					else
						return EnergyKPI.EnergyType.Pipelines;
				case AbstractLayer.EditingType.SourcePoint:
					if (t.GreenEnergy)
						return EnergyKPI.EnergyType.GreenProductionZones;
					else
						return EnergyKPI.EnergyType.GreyProductionZones;
				case AbstractLayer.EditingType.SourcePolygon:
					if (t.GreenEnergy)
						return EnergyKPI.EnergyType.GreenProductionZones;
					else
						return EnergyKPI.EnergyType.GreyProductionZones;
				case AbstractLayer.EditingType.Transformer:
					return EnergyKPI.EnergyType.TransformerStations;
				case AbstractLayer.EditingType.Socket:
					return EnergyKPI.EnergyType.LandSockets;
				default:
					return EnergyKPI.EnergyType.NoEnergy;
			}
		}
	}
}
