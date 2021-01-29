using System.Collections.Generic;

namespace KPI
{
	public class KPIValueCollection
	{
        public readonly int countryId;
        private readonly Dictionary<string, KPIValue> values = new Dictionary<string, KPIValue>(); //Includes categories.
		private readonly List<KPICategory> valueCategories = new List<KPICategory>();
		protected int MostRecentMonthReceived
		{
			get;
			private set;
		}

		public delegate void KPIValuesUpdated(KPIValueCollection sourceCollection, int previousMostRecentMonth, int mostRecentMonthReceived);
		public event KPIValuesUpdated OnKPIValuesReceivedAndProcessed;
		public event KPIValuesUpdated OnKPIValuesUpdated;

		public delegate void KPIValueDefinitionsChanged(KPIValueCollection sourceCollection);
		public event KPIValueDefinitionsChanged OnKPIValueDefinitionsChanged;	//Called when new values have been added or removed.

		public KPIValueCollection(int countryId = -1)
		{
			MostRecentMonthReceived = -1;
            this.countryId = countryId;
        }

		public virtual void SetupKPIValues(KPICategoryDefinition[] kpiDefinitions, int numberOfKpiMonths)
		{
			if (kpiDefinitions != null)
			{
				InitializeKPIValueCategories(kpiDefinitions, numberOfKpiMonths);
			}
		}

		private void InitializeKPIValueCategories(KPICategoryDefinition[] categories, int numberOfKpiMonths)
		{
			foreach (KPICategoryDefinition category in categories)
			{
				KPICategory kpiCategory = new KPICategory(numberOfKpiMonths, category.categoryName, category.unit, category.categoryColor, category.categoryValueType, category.categoryDisplayName, category.valueColorScheme, countryId);
				valueCategories.Add(kpiCategory);
				values.Add(category.categoryName, kpiCategory);

				foreach (KPIValueDefinition val in category.valueDefinitions)
				{
					if (!IsValueSubset() || ValueSubsetFilter(val, category))
					{
						int kpiCountry = /*(val.valueDependentCountry != KPIValue.CountrySpecific)? val.valueDependentCountry :*/ countryId;

						KPIValue value = new KPIValue(kpiCategory.name, numberOfKpiMonths, val.valueName, val.unit, val.valueColor, val.valueDisplayName, kpiCountry);
						values.Add(val.valueName, value);
						kpiCategory.AddChildValue(value);
					}
				}
			}

			if (OnKPIValueDefinitionsChanged != null)
			{
				OnKPIValueDefinitionsChanged.Invoke(this);
			}
		}

		protected virtual bool ValueSubsetFilter(KPIValueDefinition value, KPICategoryDefinition category)
		{
			return true;
		}

		protected virtual bool IsValueSubset()
		{
			return false;
		}

		public void ProcessReceivedKPIData(IEnumerable<EcologyKPIObject> receivedKpiData)
		{
			int mostRecentMonth = -1;
			foreach (EcologyKPIObject kpiData in receivedKpiData)
			{
				TryUpdateKPIValue(kpiData);
				if (kpiData.month > mostRecentMonth)
				{
					mostRecentMonth = kpiData.month;
				}
			}

			//Always update the KPI values so we ensure that the category values are always correct. It's a bit wasteful but if we don't do this we run the risk of having categories populated with half received values.
			OnNewKpiDataReceived(mostRecentMonth);
		}

		protected void OnNewKpiDataReceived(int newMostRecentMonth)
		{
			if (OnKPIValuesReceivedAndProcessed != null)
			{
				OnKPIValuesReceivedAndProcessed(this, MostRecentMonthReceived, newMostRecentMonth);
			}

			UpdateKPICategories(newMostRecentMonth);

			if (OnKPIValuesUpdated != null)
			{
				OnKPIValuesUpdated(this, MostRecentMonthReceived, newMostRecentMonth);
			}

			if (newMostRecentMonth > MostRecentMonthReceived)
			{
				MostRecentMonthReceived = newMostRecentMonth;
			}
		}

		private void TryUpdateKPIValue(EcologyKPIObject kpi)
		{
			TryUpdateKPIValue(kpi.name, kpi.month, kpi.value);
		}

		public void TryUpdateKPIValue(string kpiName, int kpiMonth, float kpiValue)
		{
			KPIValue value;
			if (values.TryGetValue(kpiName, out value))
			{
				value.UpdateValue(kpiMonth, kpiValue);
			}
			else if(!IsValueSubset())
			{
				UnityEngine.Debug.LogError("Received KPI values for a KPI that we don't know about. KPI: \"" + kpiName + "\"");
			}
		}

		private void UpdateKPICategories(int currentMonth)
		{
			foreach (KPICategory category in valueCategories)
			{
				if (category.categoryValueType == EKPICategoryValueType.Manual)
				{
					continue;
				}

				for (int i = category.MostRecentMonth; i <= currentMonth; ++i)
				{
					float valueSum = 0.0f;
					foreach (KPIValue childValue in category.GetChildValues())
					{
						valueSum += childValue.GetKpiValueForMonth(i);
					}

					if (category.categoryValueType == EKPICategoryValueType.Sum)
					{
						//Do nothing.
					}
					else if (category.categoryValueType == EKPICategoryValueType.Average)
					{
						valueSum /= (float)category.GetChildValueCount();
					}
					else
					{
						UnityEngine.Debug.LogError("Unimplemented category value type " + category.categoryValueType);
					}

					category.UpdateValue(i, valueSum);
				}
			}
		}

		public IEnumerable<KPICategory> GetCategories()
		{
			return valueCategories;
		}

		public IEnumerable<KPIValue> GetValues()
		{
			return values.Values;
		}

		public KPIValue FindValueByName(string valueName)
		{
			KPIValue result;
			values.TryGetValue(valueName, out result);
			return result;
		}

		public KPICategory FindCategoryByName(string targetValueOwningCategoryName)
		{
			return valueCategories.Find(obj => obj.name == targetValueOwningCategoryName);
		}
	}
}
