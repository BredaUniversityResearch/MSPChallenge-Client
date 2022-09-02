using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementScroll : ATutorialRequirement
	{
		private bool m_complete;

		public override void ActivateRequirement()
		{
			CameraManager.Instance.cameraPan.onPanStart += OnPan;
			m_complete = false;
		}

		public override void DeactivateRequirement()
		{
			CameraManager.Instance.cameraPan.onPanStart -= OnPan;
			m_complete = false;
		}

		public override bool EvaluateRequirement()
		{
			return m_complete;
		}

		void OnPan()
		{
			m_complete = true;
		}
	}
}
