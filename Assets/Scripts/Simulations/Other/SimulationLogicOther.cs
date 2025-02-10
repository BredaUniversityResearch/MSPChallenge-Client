using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MSP2050.Scripts
{
	public class SimulationLogicOther : ASimulationLogic
	{
		private KPIValueCollection m_kpis;

		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateOther data = (SimulationUpdateOther)a_data;
			m_kpis.ProcessReceivedKPIData(data.kpi);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			SimulationSettingsOther settings = (SimulationSettingsOther)a_settings;
			m_kpis = new KPIValueCollection();
			m_kpis.SetupKPIValues(settings.kpis, SessionManager.Instance.MspGlobalData.session_end_month);
		}
		public override void Destroy()
		{	}

		public override KPIValueCollection GetKPIValuesForCountry(int a_countryId = -1)
		{
			return m_kpis;
		}

		public override List<KPIValueCollection> GetKPIValuesForAllCountries()
		{
			return new List<KPIValueCollection>() { m_kpis };
		}
	}
}