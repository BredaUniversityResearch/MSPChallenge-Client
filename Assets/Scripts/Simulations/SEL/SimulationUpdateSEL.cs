using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class SimulationUpdateSEL : ASimulationData
	{
		public KPIObject[] kpi;
		public List<ShippingIssueObject> shipping_issues;
	}
}