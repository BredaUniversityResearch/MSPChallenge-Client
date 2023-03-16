using System.Collections;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepRegular : ATutorialDataStep
	{
		[SerializeField, TextArea] protected string m_headerText;
		[SerializeField, TextArea] protected string m_contentText;
		[SerializeField] bool m_alignTop;
		[SerializeField] ATutorialRequirement[] m_completionRequirements;
		[SerializeField] ATutorialRequirement[] m_prerequisites;
		[SerializeField] string[] m_highlightedObjects;
		[SerializeField] bool m_highlightBasedOnTags;
		[SerializeField] GameObject m_visualsPrefab;
		[SerializeField] ATutorialAction[] m_enterStepActions;

		private bool m_complete;
		public bool IsComplete() { return m_complete; }

		public override void EnterStep(TutorialManager a_manager, bool a_firstStep, bool a_lastStep)
		{
			//Initialise requirements
			if (m_completionRequirements != null && m_completionRequirements.Length > 0)
			{
				foreach (ATutorialRequirement requirement in m_completionRequirements)
					requirement.ActivateRequirement();
			}
			if (m_prerequisites != null && m_prerequisites.Length > 0)
			{
				foreach (ATutorialRequirement prerequisite in m_prerequisites)
					prerequisite.ActivateRequirement();
			}

			if (m_enterStepActions != null)
			{
				foreach(ATutorialAction action in m_enterStepActions)
					action.Invoke();
			}

			if(m_completionRequirements == null || m_completionRequirements.Length == 0)
				m_complete = true;
			else if (CheckCompletion())
			{
				//Don't automatically move on if we are complete on start
				m_complete = true;
				a_manager.UI.SetRequirementChecked(true);
			}
			else
			{
				m_complete = false;
			}

			a_manager.UI.SetUIToRegular(m_headerText, m_contentText, m_completionRequirements != null && m_completionRequirements.Length > 0, m_alignTop, m_visualsPrefab, IsComplete);
			if (m_highlightedObjects != null && m_highlightedObjects.Length > 0)
			{
				HighlightManager.instance.SetUIHighlights(m_highlightedObjects, m_highlightBasedOnTags);
			}

		}

		public override void ExitStep(TutorialManager a_manager)
		{
			if (m_completionRequirements != null && m_completionRequirements.Length > 0)
			{
				foreach (ATutorialRequirement requirement in m_completionRequirements)
					requirement.DeactivateRequirement();
			}
			if (m_prerequisites != null && m_prerequisites.Length > 0)
			{
				foreach (ATutorialRequirement prerequisite in m_prerequisites)
					prerequisite.DeactivateRequirement();
			}
			HighlightManager.instance.ClearUIHighlights();
		}

		public override void Update(TutorialManager a_manager)
		{
			//Check completion
			if (!m_complete)
			{
				CheckCompletion();
				if(m_complete)
				{
					a_manager.UI.SetRequirementChecked(true);
					a_manager.MoveToNextStep();
				}
			}

			//Check prerequisites
			if ((m_completionRequirements == null || !m_complete) && !CheckPrerequisites())
			{
				a_manager.MoveToPreviousStep();
			}
		}

		bool CheckCompletion()
		{
			m_complete = true;
			if (m_completionRequirements != null && m_completionRequirements.Length > 0)
			{
				foreach (ATutorialRequirement requirement in m_completionRequirements)
				{
					if (!requirement.EvaluateRequirement())
					{
						m_complete = false;
						break;
					}
				}
			}
			return m_complete;
		}

		public override bool CheckPrerequisites()
		{
			if (m_prerequisites == null)
				return true;
			foreach(ATutorialRequirement prerequisite in m_prerequisites)
				if (!prerequisite.EvaluateRequirement())
					return false;
			return true;
		}
	}
}