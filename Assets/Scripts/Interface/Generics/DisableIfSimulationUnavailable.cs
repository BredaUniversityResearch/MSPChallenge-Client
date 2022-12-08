using UnityEngine;

namespace MSP2050.Scripts
{
	class DisableIfSimulationUnavailable: MonoBehaviour
	{
		[SerializeField]
		private ESimulationType simulationType = ESimulationType.None;

		private void Start()
		{
			if(SimulationManager.Instance.Initialised)
			{ 
				gameObject.SetActive(SimulationManager.Instance.TryGetLogic(simulationType.ToString(), out var logic));
			}
			else
			{
				SimulationManager.Instance.m_onSimulationsInitialised += OnSimulationsInitialised;
			}
		}

		void OnSimulationsInitialised()
		{
			gameObject.SetActive(SimulationManager.Instance.TryGetLogic(simulationType.ToString(), out var logic));
		}
	}
}