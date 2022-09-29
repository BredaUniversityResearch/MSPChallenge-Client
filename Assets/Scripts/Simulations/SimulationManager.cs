using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationManager : MonoBehaviour
	{
		private static SimulationManager singleton;
		public static SimulationManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<SimulationManager>();
				return singleton;
			}
		}

		private Dictionary<string, SimulationDefinition> m_simulationDefinitions = new Dictionary<string, SimulationDefinition>();
		private Dictionary<string, ASimulationLogic> m_simulationLogic = new Dictionary<string, ASimulationLogic>();

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
		}

		void OnDestroy()
		{
			singleton = null;
		}

		//All possible policies should be registered before policies are initilised
		public void RegisterPolicy(SimulationDefinition a_simulation)
		{
			m_simulationDefinitions.Add(a_simulation.m_name, a_simulation);
		}

		public void InitialiseSimulations(ASimulationData[] a_simulationSettings)
		{
			//Create logic instances
			foreach(ASimulationData data in a_simulationSettings)
			{
				if(m_simulationDefinitions.TryGetValue(data.simulation_type, out SimulationDefinition definition))
				{
					ASimulationLogic logic = (ASimulationLogic)gameObject.AddComponent(definition.m_logicType);
					logic.Initialise(data);
					m_simulationLogic.Add(data.simulation_type, logic);
				}
				else
				{
					Debug.LogError("Simulation settings received from the server for a simulation without definition: " + data.simulation_type);
				}
			}
		}

		public bool TryGetDefinition(string a_name, out SimulationDefinition a_definition)
		{
			return m_simulationDefinitions.TryGetValue(a_name, out a_definition);
		}

		public bool TryGetLogic(string a_name, out ASimulationLogic a_logic)
		{
			return m_simulationLogic.TryGetValue(a_name, out a_logic);
		}
	}
}