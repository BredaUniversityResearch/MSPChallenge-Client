using System.Collections.Generic;

namespace KPI
{
	class KPIValueCollectionShipping: KPIValueCollection
	{
		public KPIValueCollectionShipping(int countryId)
			: base(countryId)
		{
		}

		protected override bool ValueSubsetFilter(KPIValueDefinition value, KPICategoryDefinition category)
		{
			return value.valueDependentCountry == countryId || value.valueDependentCountry == KPIValue.CountryGlobal || countryId == KPIValue.CountryGlobal;
		}

		protected override bool IsValueSubset()
		{
			return countryId != KPIValue.CountryGlobal;
		}
	}
}
