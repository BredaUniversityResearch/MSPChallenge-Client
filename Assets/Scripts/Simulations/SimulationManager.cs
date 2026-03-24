using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Codice.Client.Common.EventTracking.TrackFeatureUseEvent.Features.DesktopGUI.Filters;

namespace MSP2050.Scripts
{
	public class SimulationManager : MonoBehaviour
	{
		public const string CEL_SIM_NAME = "CEL";
		public const string MEL_SIM_NAME = "MEL";
		public const string SEL_SIM_NAME = "SEL";
		public const string SE_SIM_NAME = "SANDEXTRACTION";
		public const string OTHER_SIM_NAME = "EXTERNAL";
		public const string Geometry_KPI_NAME = "GEOMETRY";
		public const string MultiUse_KPI_NAME = "MULTIUSE";

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

		private CountryKPICollectionGeometry m_geometryKPIs = new CountryKPICollectionGeometry();
		private CountryKPICollectionMUP m_MUPKPIs = new CountryKPICollectionMUP();

		public delegate void SimulationsInitialisedCallback();
		public event SimulationsInitialisedCallback m_onSimulationsInitialised;

		private bool m_initialised;
		public bool Initialised => m_initialised;
		public Dictionary<string, ASimulationData> Settings => m_simulationSettings;

		void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;

			if (Main.Instance.GameLoaded)
				CreateClientKPIs();
			else
				Main.Instance.OnFinishedLoadingLayers += CreateClientKPIs;
		}

		void OnDestroy()
		{
			singleton = null;
			foreach (var kvp in m_simulationLogic)
			{
				kvp.Value.Destroy();
			}
		}

		//All possible simulations should be registered before policies are initialised
		public void RegisterSimulation(SimulationDefinition a_simulation)
		{
			m_simulationDefinitions.Add(a_simulation.m_name.ToUpper(), a_simulation);
		}

		public void RegisterBuiltInSimulations()
		{
			m_simulationDefinitions.Add(MEL_SIM_NAME, new SimulationDefinition { m_name = MEL_SIM_NAME, m_updateType = typeof(SimulationUpdateMEL), m_logicType = typeof(SimulationLogicMEL), m_settingsType = typeof(SimulationSettingsMEL)});
			m_simulationDefinitions.Add(CEL_SIM_NAME, new SimulationDefinition { m_name = CEL_SIM_NAME, m_updateType = typeof(SimulationUpdateCEL), m_logicType = typeof(SimulationLogicCEL), m_settingsType = typeof(SimulationSettingsCEL) });
			m_simulationDefinitions.Add(SEL_SIM_NAME, new SimulationDefinition { m_name = SEL_SIM_NAME, m_updateType = typeof(SimulationUpdateSEL), m_logicType = typeof(SimulationLogicSEL), m_settingsType = typeof(SimulationSettingsSEL) });
			m_simulationDefinitions.Add(SE_SIM_NAME, new SimulationDefinition { m_name = SE_SIM_NAME, m_updateType = typeof(SimulationUpdateOther), m_logicType = typeof(SimulationLogicOther), m_settingsType = typeof(SimulationSettingsOther) });
			m_simulationDefinitions.Add(OTHER_SIM_NAME, new SimulationDefinition { m_name = OTHER_SIM_NAME, m_updateType = typeof(SimulationUpdateOther), m_logicType = typeof(SimulationLogicOther), m_settingsType = typeof(SimulationSettingsOther) });
		}

		public void InitialiseSimulations(List<ASimulationData> a_simulationSettings)
		{
			//Create logic instances
			foreach (ASimulationData data in a_simulationSettings)
			{
				if(data != null && !string.IsNullOrEmpty(data.simulation_type) && m_simulationDefinitions.TryGetValue(data.simulation_type.ToUpper(), out SimulationDefinition definition))
				{
					ASimulationLogic logic = (ASimulationLogic)gameObject.AddComponent(definition.m_logicType);
					logic.Initialise(data);
					m_simulationLogic.Add(data.simulation_type.ToUpper(), logic);
					m_simulationSettings.Add(data.simulation_type.ToUpper(), data);
				}
				else
				{
					Debug.LogError("Simulation settings received from the server for a simulation without definition: " + (data == null ? "null" : data.simulation_type));
				}
			}
			if (m_onSimulationsInitialised != null)
			{
				m_onSimulationsInitialised.Invoke();
				m_onSimulationsInitialised = null;
			}
			m_initialised = true;
		}

		public void PostLayerMetaInitialise()
		{ 
			foreach(var kvp in m_simulationLogic)
				kvp.Value.PostLayerMetaInitialise();
		}

		public bool TryGetDefinition(string a_name, out SimulationDefinition a_definition)
		{
			return m_simulationDefinitions.TryGetValue(a_name.ToUpper(), out a_definition);
		}

		public bool TryGetLogic(string a_name, out ASimulationLogic a_logic)
		{
			return m_simulationLogic.TryGetValue(a_name.ToUpper(), out a_logic);
		}

		public bool TryGetSettings(string a_name, out ASimulationData a_settings)
		{
			return m_simulationSettings.TryGetValue(a_name.ToUpper(), out a_settings);
		}

		public void RunGeneralUpdate(List<ASimulationData> a_data)
		{
			foreach (ASimulationData data in a_data)
			{
				if (m_simulationLogic.TryGetValue(data.simulation_type.ToUpper(), out ASimulationLogic simulation))
				{
					simulation.HandleGeneralUpdate(data);
				}
			}
		}

		public KPIValueCollection GetKPIValuesForSimulation(string a_targetSimulation, int a_countryId = -1)
		{
			if (string.IsNullOrEmpty(a_targetSimulation))
			{
				return m_geometryKPIs.GetKPIForCountry(a_countryId);
			}

			string upper = a_targetSimulation.ToUpper();
			if (upper == MultiUse_KPI_NAME)
			{
				return m_MUPKPIs.GetKPIForCountry(a_countryId);
			}
			if (upper == Geometry_KPI_NAME)
			{
				return m_geometryKPIs.GetKPIForCountry(a_countryId);
			}
			if (m_simulationLogic.TryGetValue(upper, out var logic))
			{
				return logic.GetKPIValuesForCountry(a_countryId);
			}
			return null;
		}

		public List<KPIValueCollection> GetKPIValuesForAllCountriesSimulation(string a_targetSimulation)
		{
			if (string.IsNullOrEmpty(a_targetSimulation))
			{
				return m_geometryKPIs.GetKPIForAllCountries();
			}

			string upper = a_targetSimulation.ToUpper();
			if (upper == MultiUse_KPI_NAME)
			{
				return m_MUPKPIs.GetKPIForAllCountries();
			}
			if (upper == Geometry_KPI_NAME)
			{
				return m_geometryKPIs.GetKPIForAllCountries();
			}
			if (m_simulationLogic.TryGetValue(upper, out var logic))
			{
				return logic.GetKPIValuesForAllCountries();
			}
			return null;
		}

		private void CreateClientKPIs()
		{
			m_geometryKPIs.SetupKPIValues(null, SessionManager.Instance.MspGlobalData.session_end_month);
			m_MUPKPIs.SetupKPIValues(null, SessionManager.Instance.MspGlobalData.session_end_month);
			TimeManager.Instance.OnCurrentMonthChanged += UpdateClientKPIs;
		}

		private void UpdateClientKPIs(int oldMonth, int newMonth)
		{
			m_geometryKPIs.CalculateKPIValues(newMonth);
			m_MUPKPIs.CalculateKPIValues(newMonth);
		}
	}
}