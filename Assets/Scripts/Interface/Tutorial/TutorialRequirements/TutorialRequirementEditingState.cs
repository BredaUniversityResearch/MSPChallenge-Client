using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementEditingState : ATutorialRequirement
	{
		[SerializeField] private FSMState.EEditingStateType m_targetState;
		[SerializeField] private bool m_invert;

		public override bool EvaluateRequirement()
		{
			if(m_invert)
				return FSM.CurrentState.StateType != m_targetState;
			return FSM.CurrentState.StateType == m_targetState;
		}
	}
}
