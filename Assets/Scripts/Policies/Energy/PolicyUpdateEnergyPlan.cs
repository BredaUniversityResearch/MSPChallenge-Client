using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class PolicyUpdateEnergyPlan : APolicyData
	{
		public bool alters_energy_distribution;
		public List<GridObject> grids;
		public HashSet<int> deleted_grids;
		public string energy_error;
	}

	public class GridObject
	{
		public int id;
		public int persistent;
		public string name;
		public int active;
		public bool distribution_only;
		public List<GeomIDObject> sources;
		public List<GeomIDObject> sockets;
		public List<CountryExpectedObject> energy;
	}
	public class CountryExpectedObject
	{
		public int country_id;
		public long expected; //Expected WHAT? Cows? Apples? 
	}
	public class GeomIDObject
	{
		public int geometry_id;
	}
}