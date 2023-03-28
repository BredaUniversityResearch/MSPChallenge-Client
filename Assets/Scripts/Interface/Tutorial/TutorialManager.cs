using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialManager : MonoBehaviour
	{
		private static TutorialManager m_instance;
		public static TutorialManager Instance => m_instance;

		[SerializeField] private GameObject m_tutorialUIPrefab;

		TutorialUI m_UI;
		TutorialData m_data;
		int m_currentStep;
		
		public TutorialUI UI => m_UI;

		private void Awake()
		{
			m_instance = this;
		}

		private void OnDestroy()
		{
			m_instance = null;
		}

		private void Update()
		{
			if (m_data != null && m_currentStep >= 0)
			{
				m_data.m_steps[m_currentStep].Update(this);
			}
		}

		public void StartTutorial(TutorialData a_tutorialData)
		{
			m_data = a_tutorialData;
			if (m_UI == null)
			{
				m_UI = Instantiate(m_tutorialUIPrefab, transform).GetComponent<TutorialUI>();
				m_UI.Initialise(MoveToNextStep, MoveToPreviousStep, CloseTutorial);
			}

			m_currentStep = -1;
			MoveToNextStep();
		}

		public void CloseTutorial()
		{
			if (m_currentStep >= 0 && m_currentStep < m_data.m_steps.Length)
			{
				m_data.m_steps[m_currentStep].ExitStep(this);
			}
			Destroy(m_UI.gameObject);
			m_data = null;
			m_UI = null;
			m_currentStep = -1;
		}

		public void MoveToNextStep()
		{
			if (m_currentStep >= 0)
			{
				m_data.m_steps[m_currentStep].ExitStep(this);
			}


			m_currentStep++;
			if(m_currentStep == m_data.m_steps.Length)
				CloseTutorial();
			else
				m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length-1);
		}

		public void MoveToPreviousStep()
		{
			m_data.m_steps[m_currentStep].ExitStep(this);
			m_currentStep--;
			while (m_currentStep >= 0)
			{
				if (m_data.m_steps[m_currentStep].CheckPrerequisites())
					break;
				m_currentStep--;
			}

			if (m_currentStep >= 0)
			{
				m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length - 1);
			}
			else
			{
				CloseTutorial();
			}
		}
	}
}