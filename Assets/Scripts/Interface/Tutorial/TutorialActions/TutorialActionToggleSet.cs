using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TutorialActionToggleSet : ATutorialAction
	{
		[SerializeField] private string m_toggleName;
		[SerializeField] private bool m_value;

		public override void Invoke()
		{
			Toggle toggle = InterfaceCanvas.Instance.GetUIToggle(m_toggleName);
			if (toggle != null)
				toggle.isOn = m_value;

		}
	}
}
