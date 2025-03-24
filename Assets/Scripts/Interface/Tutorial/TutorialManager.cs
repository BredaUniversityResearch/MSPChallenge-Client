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
		int m_currentChapterStep = -1, m_nextChapterStep = -1, m_previousChapterStep = -1;

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
			if (m_data == a_tutorialData)
				return;
			m_data = a_tutorialData;

			if (Main.InEditMode)
			{
				DialogBoxManager.instance.ConfirmationWindow("Stop editing", "Starting the tutorial will leave edit mode. All changes made to the plan will be lost. Are you sure you want to open the tutorial?", null, () => StartTutorialConfirmed());
			}
			else
				StartTutorialConfirmed();
		}

		void StartTutorialConfirmed()
		{
			InterfaceCanvas.Instance.activePlanWindow.ForceCancel(true);
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
			InterfaceCanvas.Instance.activePlanWindow.SetTutorialButtonCoverActive(false); //Enforce this is turned off, if tutorial closed halfway
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
			if (m_currentStep == m_data.m_steps.Length)
				CloseTutorial();
			else
			{
				if(m_data.m_steps[m_currentStep].IsChapterStart())
				{
					DetermineChapterSteps();
				}
				m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length - 1, m_nextChapterStep >= 0, m_previousChapterStep >= 0);
			}
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
				if(m_currentStep < m_currentChapterStep)
				{
					DetermineChapterSteps();
				}

				m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length - 1, m_nextChapterStep >= 0, m_previousChapterStep >= 0);
			}
			else
			{
				CloseTutorial();
			}
		}

		public void MoveToNextChapter()
		{
			if (m_currentStep >= 0)
			{
				m_data.m_steps[m_currentStep].ExitStep(this);
			}
			m_currentStep = m_nextChapterStep;
			DetermineChapterSteps();
			m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length - 1, m_nextChapterStep >= 0, m_previousChapterStep >= 0);
		}

		public void MoveToPreviousChapter()
		{
			m_data.m_steps[m_currentStep].ExitStep(this);
			m_currentStep = m_previousChapterStep;
			DetermineChapterSteps();
			m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length - 1, m_nextChapterStep >= 0, m_previousChapterStep >= 0);
		}

		void DetermineChapterSteps()
		{
			m_nextChapterStep = DetermineNextChapterStep(m_currentStep + 1);
			m_currentChapterStep = DeterminePreviousChapterStep(m_currentStep);
			m_previousChapterStep = DeterminePreviousChapterStep(m_currentChapterStep - 1);
		}

		int DetermineNextChapterStep(int a_from)
		{
			for (int i = a_from; i < m_data.m_steps.Length; i++)
			{
				if (m_data.m_steps[i].IsChapterStart())
					return i;
			}
			return -1;
		}
		int DeterminePreviousChapterStep(int a_from)
		{
			for (int i = a_from; i >= 0; i--)
			{
				if (m_data.m_steps[i].IsChapterStart())
					return i;
			}
			return -1;
		}

		public void HandleResolutionOrScaleChange()
		{
			if(m_UI != null)
			{
				m_data.m_steps[m_currentStep].ExitStep(this);
				m_data.m_steps[m_currentStep].EnterStep(this, m_currentStep == 0, m_currentStep == m_data.m_steps.Length - 1, m_nextChapterStep >= 0, m_previousChapterStep >= 0);
			}
		}
	}
}