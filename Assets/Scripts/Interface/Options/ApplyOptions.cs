using UnityEngine;

namespace MSP2050.Scripts
{
	public class ApplyOptions: MonoBehaviour
	{
		public void Start()
		{
			GameSettings.Instance.ApplyCurrentSettings(false);
		}
	}
}
