using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyUpdateEnergy : APolicyData
	{
		public List<EnergyConnectionObject> connections;
		public List<EnergyOutputObject> output;
	}

	public class EnergyOutputObject
	{
		public int id;
		public long capacity;
		public long maxcapacity;
		public int active;
	}

	public class EnergyConnectionObject
	{
		public string start;
		public string end;
		public string cable;
		public string coords;
		public string active;
	}
}