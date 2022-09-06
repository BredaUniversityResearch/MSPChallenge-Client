using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementScroll : ATutorialRequirement
	{
		private bool m_complete;

		public override void ActivateRequirement()
		{
			CameraManager.Instance.cameraZoom.onScrollZoom += OnZoom;
			m_complete = false;
		}

		public override void DeactivateRequirement()
		{
			CameraManager.Instance.cameraZoom.onScrollZoom -= OnZoom;
			m_complete = false;
		}

		public override bool EvaluateRequirement()
		{
			return m_complete;
		}

		void OnZoom()
		{
			m_complete = true;
		}
	}
}
