using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepTitle : ATutorialDataStep
	{
		[SerializeField, TextArea] protected string m_headerText;
		[SerializeField, TextArea] protected string m_contentText;
		[SerializeField] protected string m_partText;
		[SerializeField] ATutorialAction[] m_enterStepActions;

		public override void EnterStep(TutorialManager a_manager, bool a_firstStep, bool a_lastStep)
		{
			a_manager.UI.SetUIToTitle(m_headerText, m_contentText, m_partText, !a_firstStep, !a_lastStep);

			if (m_enterStepActions != null)
			{
				foreach (ATutorialAction action in m_enterStepActions)
					action.Invoke();
			}
		}

		public override void ExitStep(TutorialManager a_manager)
		{}

		public override void Update(TutorialManager a_manager)
		{}
	}
}