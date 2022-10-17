using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyPlanDataEnergy: APolicyPlanData
	{
		public List<EnergyGrid> energyGrids;
		public HashSet<int> removedGrids;
		public bool altersEnergyDistribution;
		public bool energyError;
	}
}