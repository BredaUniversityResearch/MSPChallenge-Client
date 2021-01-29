using UnityEngine;

class DisableIfSimulationUnavailable: MonoBehaviour
{
	[SerializeField]
	private ESimulationType simulationType = ESimulationType.None;

	private void Awake()
	{
		Main.OnGlobalDataLoaded += OnGlobalDataLoaded;
	}

	private void OnDestroy()
	{
		Main.OnGlobalDataLoaded -= OnGlobalDataLoaded;
	}

	private void OnGlobalDataLoaded()
	{
		gameObject.SetActive(Main.IsSimulationConfigured(simulationType));
	}
}