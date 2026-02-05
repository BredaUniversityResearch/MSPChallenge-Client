using System.Collections.Generic;

namespace MSP2050.Scripts
{
	class CountryKPICollectionShipping : CountryKPICollection<KPIValueCollectionShipping>
	{
		
		public CountryKPICollectionShipping()
		{
			KPIsPerCountry = new Dictionary<int, KPIValueCollectionShipping>();
			AddKPIForCountry(0);
			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (!team.IsManager)
				{
					AddKPIForCountry(team.ID);
				}
			}
		}

		public void ProcessReceivedKPIData(KPIObject[] shippingData)
		{
			foreach (KeyValuePair<int, KPIValueCollectionShipping> kvp in KPIsPerCountry)
			{
				kvp.Value.ProcessReceivedKPIData(shippingData);
			}
		}
	}
}
