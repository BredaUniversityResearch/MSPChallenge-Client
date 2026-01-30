using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
    class CountryKPICollection<T> where T: KPIValueCollection, new()
    {
        protected Dictionary<int, T> KPIsPerCountry;

        public CountryKPICollection()
        {
            KPIsPerCountry = new Dictionary<int, T>();
        }

        public void AddKPIForCountry(int country)
        {
            T collection = new T();
            collection.Initialise(country);
            KPIsPerCountry.Add(country, collection);
        }

        public virtual void SetupKPIValues(KPICategoryDefinition[] kpiDefinitions, int numberOfKpiMonths)
        {
            foreach (KeyValuePair<int, T> kvp in KPIsPerCountry)
            {
                kvp.Value.SetupKPIValues(kpiDefinitions, numberOfKpiMonths);
            }
        }

        public T GetKPIForCountry(int country)
        {
            if(KPIsPerCountry != null && KPIsPerCountry.TryGetValue(country, out var result))
                return result;
            return null;
        }

        public List<KPIValueCollection> GetKPIForAllCountries()
		{
            if(KPIsPerCountry == null)
            {
                return new List<KPIValueCollection>();
			}
            List<KPIValueCollection> result = new List<KPIValueCollection>(KPIsPerCountry.Count);
            foreach(var kvp in KPIsPerCountry)
			{
                result.Add(kvp.Value);
			}
            return result;
        }
    }
}
