using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepRegular : ATutorialDataStep
	{
		[SerializeField] bool m_alignTop;
		[SerializeField] ATutorialRequirement[] m_completionRequirements;
		[SerializeField] ATutorialRequirement[] m_prerequisites;
		[SerializeField] string[] m_highlightedObjects;
		[SerializeField] GameObject m_visualsPrefab;

		private bool m_complete;

		//===Progression requirements
		/* Scroll => register specific event
		 * Map drag => register specific event
		 * Create geometry complete => register specific event
		 * Press button => UI string reference
		 * Toggle state => UI string reference
		 * Press one of X specific buttons (ex: window more info ? buttons)  => UI string reference
		 * Press any button of type X (ex: layer category, layer) => button tags, generic button callback receiver InterfaceCanvas
		 */

		//===Graphics/animation
		//Sprite
		//Sprite sequence (cut) + fps playback

		//===Elements to highlight

		//===Automatic actions (are these needed?)
		/* Open/close windows => UI string reference toggles + state
		 */

		//Tutorial sequences are shown in series (e.g. making a plan), the system auto detects at what step in the sequence you are and will move you back if you mess up.
		//This would require tutorial step prerequisites, besides progression requirements:
		/* Window open => UI string reference
		 * In edit mode
		 * In create mode
		 */
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

			a_manager.UI.SetUIToRegular(m_headerText, m_contentText, m_completionRequirements != null && m_completionRequirements.Length > 0, m_alignTop, m_visualsPrefab);
			//TODO: Highlight objects
			m_complete = false;
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
		}

		public override void Update(TutorialManager a_manager)
		{
			//Check completion
			if (!m_complete)
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
			}

			//Check prerequisites
			if (!m_complete && !CheckPrerequisites())
			{
				a_manager.MoveToPreviousStep();
			}
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