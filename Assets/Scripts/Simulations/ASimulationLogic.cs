using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public abstract class ASimulationLogic : MonoBehaviour
	{
		public abstract void Initialise(ASimulationData a_settings);
		public abstract void HandleGeneralUpdate(ASimulationData a_data);
	}
}