using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialManager : MonoBehaviour
	{
		TutorialData m_data;
		int m_currentStep;


		void Start()
		{

		}

		private void Update()
		{
			//Check current step completion
			//If not complete: check current step prerequisites
			//If not met: go back until met
		}


		public void StartTutorial(TutorialData a_tutorialData)
		{
			m_data = a_tutorialData;
			m_currentStep = -1;
			MoveToNextStep();
		}

		public void MoveToNextStep()
		{
			//TODO
		}

		public void MoveToPreviousStep()
		{
			//TODO: check prerequisites, keep going back until they are met
		}
	}
}