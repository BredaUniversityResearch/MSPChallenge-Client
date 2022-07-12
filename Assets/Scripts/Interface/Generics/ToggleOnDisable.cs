using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ToggleOnDisable : MonoBehaviour {

		public Toggle toggle;
		public bool dir;

		void OnDisable()
		{
			toggle.isOn = dir;
		}
	}
}