namespace MSP2050.Scripts
{
	class KPIValueCollectionShipping: KPIValueCollection
	{
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
