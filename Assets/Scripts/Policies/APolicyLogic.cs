using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class APolicyLogic : MonoBehaviour
	{
		public abstract void Initialise(APolicyData a_settings);
		public abstract void HandlePlanUpdate(APolicyData a_data, Plan a_plan);
		public abstract void HandlePreKPIUpdate(APolicyData a_data);
		public abstract void HandlePostKPIUpdate(APolicyData a_data);
		public abstract APolicyData FormatPlanData(Plan a_plan);
		
		public virtual void UpdateAfterEditing(Plan a_plan) { }
	}
}