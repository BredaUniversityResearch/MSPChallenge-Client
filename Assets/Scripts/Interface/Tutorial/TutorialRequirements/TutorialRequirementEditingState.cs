using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementEditingState : ATutorialRequirement
	{
		[SerializeField] private FSMState.EEditingStateType m_targetState;

		public override bool EvaluateRequirement()
		{
			return FSM.CurrentState.StateType == m_targetState;
		}
	}
}
