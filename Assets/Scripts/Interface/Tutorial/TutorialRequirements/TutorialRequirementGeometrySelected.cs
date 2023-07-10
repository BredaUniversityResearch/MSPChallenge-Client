using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementGeometrySelected : ATutorialRequirement
	{
		[SerializeField] private bool m_invert;

		public override bool EvaluateRequirement()
		{
			if(m_invert)
				return !FSM.CurrentState.HasGeometrySelected;
			return FSM.CurrentState.HasGeometrySelected;
		}
	}
}
