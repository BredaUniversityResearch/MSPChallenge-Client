using UnityEngine;

namespace MSP2050.Scripts
{
	class DisableIfSimulationUnavailable: MonoBehaviour
	{
		[SerializeField]
		private ESimulationType simulationType = ESimulationType.None;

		private void Start()
		{
			gameObject.SetActive(Main.Instance.IsSimulationConfigured(simulationType));
		}
	}
}