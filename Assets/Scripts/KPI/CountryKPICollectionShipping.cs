using System.Collections.Generic;

namespace KPI
{
	class CountryKPICollectionShipping
	{
		private Dictionary<int, KPIValueCollectionShipping> shippingKPIs; //Geometry KPIs per country id.
		
		public CountryKPICollectionShipping()
		{
			shippingKPIs = new Dictionary<int, KPIValueCollectionShipping>();
			AddKPIForCountry(0);
			foreach (Team team in TeamManager.GetTeams())
			{
				if (!team.IsManager)
				{
					AddKPIForCountry(team.ID);
				}
			}
		}

		public void AddKPIForCountry(int country)
		{
			shippingKPIs.Add(country, new KPIValueCollectionShipping(country));
		}

		public void SetupKPIValues(KPICategoryDefinition[] kpiDefinitions, int numberOfKpiMonths)
		{
			foreach (KeyValuePair<int, KPIValueCollectionShipping> kvp in shippingKPIs)
			{
				kvp.Value.SetupKPIValues(kpiDefinitions, numberOfKpiMonths);
			}
		}

		public void ProcessReceivedKPIData(EcologyKPIObject[] shippingData)
		{
			foreach (KeyValuePair<int, KPIValueCollectionShipping> kvp in shippingKPIs)
			{
				kvp.Value.ProcessReceivedKPIData(shippingData);
			}
		}

		public KPIValueCollection GetKPIForCountry(int country)
		{
			if (shippingKPIs.ContainsKey(country))
				return shippingKPIs[country];
			return null;
		}
	}
}
