using System;
using UnityEngine;

namespace UnityEngine.UI
{
	public interface IUIScaleChangeReceiver
	{
		void OnUIScaleChange(int a_newScale);
	}
}
