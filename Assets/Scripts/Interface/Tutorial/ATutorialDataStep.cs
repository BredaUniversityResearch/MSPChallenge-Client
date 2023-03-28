using System.Collections;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class ATutorialDataStep
	{
		public abstract void EnterStep(TutorialManager a_manager, bool a_firstStep, bool a_lastStep);
		public abstract void ExitStep(TutorialManager a_manager);
		public abstract void Update(TutorialManager a_manager);

		public virtual bool CheckPrerequisites()
		{
			return true;
		}
	}
}