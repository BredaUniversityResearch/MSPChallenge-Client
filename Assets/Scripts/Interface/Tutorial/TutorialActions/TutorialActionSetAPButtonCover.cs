using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TutorialActionSetAPButtonCover : ATutorialAction
	{
		[SerializeField] private bool m_enabled;

		public override void Invoke()
		{
			InterfaceCanvas.Instance.activePlanWindow.SetTutorialButtonCoverActive(m_enabled);
		}
	}
}
