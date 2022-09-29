using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class PolicyDataEnergy : APolicyData
	{
		public List<EnergyConnectionObject> connections;
		public List<EnergyOutputObject> output;
	}
}