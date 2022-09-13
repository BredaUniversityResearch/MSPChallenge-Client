using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialActionCloseAllWindows : ATutorialAction
	{
		public override void Invoke()
		{
			GameObject.FindObjectOfType<ShowHideManager>(false)?.CloseAllWindows();
		}
	}
}