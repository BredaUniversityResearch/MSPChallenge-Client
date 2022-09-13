using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementCreateGeometry : ATutorialRequirement
	{
		private bool m_complete;

		public override void ActivateRequirement()
		{
			Main.Instance.fsm.onGeometryCompleted += OnGeomComplete;
			m_complete = false;
		}

		public override void DeactivateRequirement()
		{
			Main.Instance.fsm.onGeometryCompleted -= OnGeomComplete;
			m_complete = false;
		}

		public override bool EvaluateRequirement()
		{
			return m_complete;
		}

		void OnGeomComplete()
		{
			m_complete = true;
		}
	}
}
