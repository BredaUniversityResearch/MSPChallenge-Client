using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class SimulationLogicSEL : ASimulationLogic
	{
		private CountryKPICollectionShipping shippingKPI;

		public override void HandleGeneralUpdate(ASimulationData a_data)
		{
			SimulationUpdateSEL data = (SimulationUpdateSEL)a_data;
			if (shippingKPI != null)
			{
				shippingKPI.ProcessReceivedKPIData(data.kpi);
			}
			if (data.shipping_issues != null)
				IssueManager.Instance.UpdateShippingIssues(data.shipping_issues);
		}

		public override void Initialise(ASimulationData a_settings)
		{
			SimulationSettingsSEL config = (SimulationSettingsSEL)a_settings;
			shippingKPI = new CountryKPICollectionShipping();
			shippingKPI.SetupKPIValues(config.kpis, SessionManager.Instance.MspGlobalData.session_end_month);
		}

		public override void Destroy()
		{ }

		public override List<KPIValueCollection> GetKPIValuesForCountry(int a_countryId = -1)
		{
			return new List<KPIValueCollection> { shippingKPI.GetKPIForCountry(a_countryId) };
		}
	}
}