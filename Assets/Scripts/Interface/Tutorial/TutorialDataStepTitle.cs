using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialDataStepTitle : ATutorialDataStep
	{
		[SerializeField] string m_partText;

		public override void EnterStep(TutorialManager a_manager, bool a_firstStep, bool a_lastStep)
		{
			//TODO: choice titles
			a_manager.UI.SetUIToTitle(m_headerText, m_contentText, m_partText, !a_firstStep, !a_lastStep);
		}

		public override void ExitStep(TutorialManager a_manager)
		{}

		public override void Update(TutorialManager a_manager)
		{}
	}
}