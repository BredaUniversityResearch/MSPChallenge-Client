using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementAPState : ATutorialRequirement
	{
		[SerializeField] private ActivePlanWindow.EInteractionMode m_targetState;
		[SerializeField] private bool m_invert;

		public override bool EvaluateRequirement()
		{
			if (m_invert)
				return InterfaceCanvas.Instance.activePlanWindow.InteractionMode != m_targetState;
			return InterfaceCanvas.Instance.activePlanWindow.InteractionMode == m_targetState;
		}
	}
}
