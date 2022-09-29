using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class PolicyDataEnergyPlan : APolicyData
	{
		public bool alters_energy_distribution;
		public List<GridObject> grids;
		public HashSet<int> deleted_grids;
		public string energy_error;
	}
}