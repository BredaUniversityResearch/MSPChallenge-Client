using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialRequirementButtonPress : ATutorialRequirement
	{
		[SerializeField] private string[] m_buttonNames;
		private bool m_complete;

		public override bool EvaluateRequirement()
		{
			return m_complete;
		}

		public override void ActivateRequirement()
		{
			m_complete = false;
			InterfaceCanvas.Instance.interactionEvent += OnInteract;
		}

		public override void DeactivateRequirement()
		{
			m_complete = false;
			InterfaceCanvas.Instance.interactionEvent -= OnInteract;
		}

		void OnInteract(string a_name, string[] a_tags)
		{
			if (m_complete)
				return;
			foreach (string name in m_buttonNames)
			{
				if (string.Equals(name, a_name))
				{
					m_complete = true;
					break;
				}
			}
		}
	}
}
