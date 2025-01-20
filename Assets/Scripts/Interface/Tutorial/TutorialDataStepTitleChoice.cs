using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepTitleChoice : TutorialDataStepTitle
	{
		[SerializeField] string m_continueButtonText;
		[SerializeField] string m_quitButtonText;

		public override void EnterStep(TutorialManager a_manager, bool a_firstStep, bool a_lastStep, bool a_showNextChapterButton, bool a_showPrevChapterButton)
		{
			a_manager.UI.SetUIToTitle(m_headerText, m_contentText, m_partText, m_continueButtonText, m_quitButtonText, !a_firstStep);
			a_manager.UI.SetChapterButtonActivity(a_showNextChapterButton, a_showPrevChapterButton);
			if (m_enterStepActions != null)
			{
				foreach (ATutorialAction action in m_enterStepActions)
					action.Invoke();
			}
		}
	}
}