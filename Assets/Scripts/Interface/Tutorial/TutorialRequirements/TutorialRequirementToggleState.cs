using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TutorialRequirementToggleState : ATutorialRequirement
	{
		[SerializeField] private string[] m_toggleNames;
		[SerializeField] private bool m_requireAll;

		private bool m_complete;
		private Toggle[] m_toggles;

		public override bool EvaluateRequirement()
		{
			if (m_complete)
				return true;
			if (m_requireAll)
			{
				bool allChecked = true;
				foreach (Toggle toggle in m_toggles)
				{
					if (!toggle.isOn)
					{
						allChecked = false;
						break;
					}
				}
				if (allChecked)
					m_complete = true;
			}
			else
			{
				foreach (Toggle toggle in m_toggles)
				{
					if (toggle.isOn)
					{
						m_complete = false;
						break;
					}
				}
			}

			return m_complete;
		}

		public override void ActivateRequirement()
		{
			m_complete = false;
			m_toggles = new Toggle[m_toggleNames.Length];
			for (int i = 0; i < m_toggleNames.Length; i++)
			{
				m_toggles[i] = InterfaceCanvas.Instance.GetUIToggle(m_toggleNames[i]);
			}
		}

		public override void DeactivateRequirement()
		{
			m_complete = false;
		}
	}
}
