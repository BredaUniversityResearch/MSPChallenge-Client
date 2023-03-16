using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementAPHasLayers : ATutorialRequirement
	{
		public override bool EvaluateRequirement()
		{
			return InterfaceCanvas.Instance.activePlanWindow.CurrentPlan.PlanLayers.Count > 0;
		}
	}
}
