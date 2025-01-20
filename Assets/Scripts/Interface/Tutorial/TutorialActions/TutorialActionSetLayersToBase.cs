using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class TutorialActionSetLayersToBase : ATutorialAction
	{
		public override void Invoke()
		{
			LayerManager.Instance.ResetVisibleLayersToBase();
		}
	}
}