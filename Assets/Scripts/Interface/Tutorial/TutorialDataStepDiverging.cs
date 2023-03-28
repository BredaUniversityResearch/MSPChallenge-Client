using System;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepDiverging : ATutorialDataStep
	{
		[SerializeField] TutorialDataStepRegular[] m_divergingSteps; //Should always be one more than requirements, to have a default
		[SerializeField] ATutorialRequirement[] m_divergingRequirements;

		private int m_actualStep;

		public override void EnterStep(TutorialManager a_manager, bool a_firstStep, bool a_lastStep)
		{
			m_actualStep = DetermineActualStep();
			m_divergingSteps[m_actualStep].EnterStep(a_manager, a_firstStep, a_lastStep);
		}

		public override void ExitStep(TutorialManager a_manager)
		{
			m_divergingSteps[m_actualStep].ExitStep(a_manager);
		}

		public override void Update(TutorialManager a_manager)
		{
			//Check if actual step changes, then update
			int newStep = DetermineActualStep();
			if (newStep != m_actualStep)
			{
				m_divergingSteps[m_actualStep].ExitStep(a_manager);
				m_actualStep = newStep;
				m_divergingSteps[m_actualStep].EnterStep(a_manager, false, false);
			}
			m_divergingSteps[m_actualStep].Update(a_manager);
		}

		int DetermineActualStep()
		{
			for (int i = 0; i < m_divergingRequirements.Length; i++)
			{
				if (m_divergingRequirements[i].EvaluateRequirement())
					return i;
			}
			return m_divergingRequirements.Length;
		}

		public override bool CheckPrerequisites()
		{
			return m_divergingSteps[DetermineActualStep()].CheckPrerequisites();
		}
	}
}
