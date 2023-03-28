using System;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TutorialActionButtonPress : ATutorialAction
	{
		[SerializeField] private string m_buttonName;

		public override void Invoke()
		{
			Button button = InterfaceCanvas.Instance.GetUIButton(m_buttonName);
			if (button != null)
				button.onClick.Invoke();

		}
	}
}