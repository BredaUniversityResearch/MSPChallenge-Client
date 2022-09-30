using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyLogicFishing : APolicyLogic
	{
		public override void Initialise(APolicyData a_settings)
		{ }

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan) { }
		public override void HandlePreKPIUpdate(APolicyData a_data) { }
		public override APolicyData FormatPlanData(Plan a_plan) 
		{
			return null;
		}

		public override void UpdateAfterEditing(Plan a_plan) { }

		public override void HandlePostKPIUpdate(APolicyData a_data)
		{
		}
	}
}