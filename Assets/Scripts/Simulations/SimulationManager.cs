using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationManager : MonoBehaviour
	{
		public const string CEL_SIM_NAME = "CEL";
		public const string MEL_SIM_NAME = "MEL";
		public const string SEL_SIM_NAME = "SEL";

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
		private Dictionary<string, ASimulationData> m_simulationSettings = new Dictionary<string, ASimulationData>();

		private CountryKPICollectionGeometry geometryKPIs = new CountryKPICollectionGeometry();

		public delegate void SimulationsInitialisedCallback();
		public event SimulationsInitialisedCallback m_onSimulationsInitialised;

		private bool m_initialised;
		public bool Initialised => m_initialised;

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;

			if (Main.Instance.GameLoaded)
				CreateGeometryKPI();
			else
				Main.Instance.OnFinishedLoadingLayers += CreateGeometryKPI;
		}

		void OnDestroy()
		{
			singleton = null;
			foreach (var kvp in m_simulationLogic)
			{
				kvp.Value.Destroy();
			}
		}

		//All possible policies should be registered before policies are initilised
		public void RegisterPolicy(SimulationDefinition a_simulation)
		{
			m_simulationDefinitions.Add(a_simulation.m_name, a_simulation);
		}

		public void RegisterBuiltInSimulations()
		{
			m_simulationDefinitions.Add(MEL_SIM_NAME, new SimulationDefinition { m_name = MEL_SIM_NAME, m_updateType = typeof(SimulationUpdateMEL), m_logicType = typeof(SimulationLogicMEL), m_settingsType = typeof(SimulationSettingsMEL)});
			m_simulationDefinitions.Add(CEL_SIM_NAME, new SimulationDefinition { m_name = CEL_SIM_NAME, m_updateType = typeof(SimulationUpdateCEL), m_logicType = typeof(SimulationLogicCEL), m_settingsType = typeof(SimulationSettingsCEL) });
			m_simulationDefinitions.Add(SEL_SIM_NAME, new SimulationDefinition { m_name = SEL_SIM_NAME, m_updateType = typeof(SimulationUpdateSEL), m_logicType = typeof(SimulationLogicSEL), m_settingsType = typeof(SimulationSettingsSEL) });
		}

		public void InitialiseSimulations(List<ASimulationData> a_simulationSettings)
		{
			//Create logic instances
			foreach (ASimulationData data in a_simulationSettings)
			{
				if(m_simulationDefinitions.TryGetValue(data.simulation_type, out SimulationDefinition definition))
				{
					ASimulationLogic logic = (ASimulationLogic)gameObject.AddComponent(definition.m_logicType);
					logic.Initialise(data);
					m_simulationLogic.Add(data.simulation_type, logic);
					m_simulationSettings.Add(data.simulation_type, data);
				}
				else
				{
					Debug.LogError("Simulation settings received from the server for a simulation without definition: " + data.simulation_type);
				}
			}
			if (m_onSimulationsInitialised != null)
			{
				m_onSimulationsInitialised.Invoke();
				m_onSimulationsInitialised = null;
			}
			m_initialised = true;
		}

		public bool TryGetDefinition(string a_name, out SimulationDefinition a_definition)
		{
			return m_simulationDefinitions.TryGetValue(a_name, out a_definition);
		}

		public bool TryGetLogic(string a_name, out ASimulationLogic a_logic)
		{
			return m_simulationLogic.TryGetValue(a_name, out a_logic);
		}

		public bool TryGetSettings(string a_name, out ASimulationData a_settings)
		{
			return m_simulationSettings.TryGetValue(a_name, out a_settings);
		}

		public void RunGeneralUpdate(List<ASimulationData> a_data)
		{
			foreach (ASimulationData data in a_data)
			{
				if (m_simulationLogic.TryGetValue(data.simulation_type, out ASimulationLogic simulation))
				{
					simulation.HandleGeneralUpdate(data);
				}
			}
		}

		public KPIValueCollection GetKPIValuesForSimulation(string a_targetSimulation, int a_countryId = -1)
		{
			if(string.IsNullOrEmpty(a_targetSimulation))
			{
				return geometryKPIs.GetKPIForCountry(a_countryId);
			}
			if (m_simulationLogic.TryGetValue(a_targetSimulation, out var logic))
			{
				return logic.GetKPIValuesForCountry(a_countryId);
			}
			return null;
		}

		private void CreateGeometryKPI()
		{
			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (!team.IsManager)
				{
					geometryKPIs.AddKPIForCountry(team.ID);
				}
			}
			//Collection for all countries together
			geometryKPIs.AddKPIForCountry(0);
			geometryKPIs.SetupKPIValues(null, SessionManager.Instance.MspGlobalData.session_end_month);
			TimeManager.Instance.OnCurrentMonthChanged += UpdateGeometryKPI;
		}

		private void UpdateGeometryKPI(int oldMonth, int newMonth)
		{
			geometryKPIs.CalculateKPIValues(newMonth);
		}
	}
}