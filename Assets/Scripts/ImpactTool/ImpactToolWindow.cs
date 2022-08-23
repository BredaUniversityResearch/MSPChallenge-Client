using UnityEngine;

namespace MSP2050.Scripts
{
	public class ImpactToolWindow : MonoBehaviour
	{
		private void OnDisable()
		{
			if (InterfaceCanvas.Instance.menuBarImpactTool.toggle.isOn)
			{
				InterfaceCanvas.Instance.menuBarImpactTool.toggle.isOn = false;
			}
		}
	}
}
