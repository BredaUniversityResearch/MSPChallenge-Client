using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationUpdateCEL : ASimulationData
	{
		public KPIObjectEnergy[] kpi;
	}

	public class KPIObjectEnergy
	{
		public int grid;
		public int month;
		public int country;
		public long actual;
	}
}