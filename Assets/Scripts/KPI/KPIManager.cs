using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPIManager
	{
		Dictionary<string, KPICat> m_categories = new Dictionary<string, KPICat>();
		Dictionary<string, KPISubCat> m_subCategories = new Dictionary<string, KPISubCat>();
		Dictionary<string, KPIVal> m_values = new Dictionary<string, KPIVal>();

		public KPICat AddOrGetCategory(KPICatDefinition a_definition)
		{
			KPICat result; 
			if(m_categories.TryGetValue(a_definition.id, out result))
			{
				return result;
			}
			result = new KPICat()
			{
				m_dispayName = a_definition.dispayName,
				m_id = a_definition.id,
				m_icon = null, //TODO
				m_subCategories = new List<KPISubCat>()
			}; 
			m_categories.Add(a_definition.id, result);
			return result;
		}

		public KPISubCat AddOrGetSubCategory(KPISubCatDefinition a_definition, bool a_addAndSetCategory = true)
		{
			KPISubCat result;
			if (m_subCategories.TryGetValue(a_definition.id, out result))
			{
				return result;
			}
			result = new KPISubCat()
			{
				m_dispayName = a_definition.dispayName,
				m_id = a_definition.id,
				m_values = new List<KPIVal>(),
				m_countrySpecific = a_definition.countrySpecific
			};
			if(a_addAndSetCategory)
			{
				if (m_categories.TryGetValue(a_definition.categoryId, out var category))
				{
					result.m_category = category;
					category.m_subCategories.Add(result);
				}
				else
					Debug.LogWarning($"Trying to add KPI subcategory {a_definition.dispayName} to a nonexistent category {a_definition.categoryId}");
			}
			ValueConversionCollection vcc = VisualizationUtil.Instance.VisualizationSettings.ValueConversions;
			vcc.TryGetConverter(a_definition.unit, out result.m_unit);
			m_subCategories.Add(a_definition.id, result);
			return result;
		}

		public KPIVal AddValue(KPIValDefinition a_definition, bool a_addAndSetCategory = true)
		{
			KPIVal result;
			if (m_values.TryGetValue(a_definition.id, out result))
			{
				return result;
			}
			result = new KPIVal()
			{
				m_dispayName = a_definition.dispayName,
				m_id = a_definition.id
			};
			if (a_addAndSetCategory)
			{
				if (m_subCategories.TryGetValue(a_definition.subCategoryId, out var subCategory))
				{
					result.m_subCategory = subCategory;
					subCategory.m_values.Add(result);
				}
				else
					Debug.LogWarning($"Trying to add KPI value {a_definition.dispayName} to a nonexistent subcategory {a_definition.subCategoryId}");
			}
			m_values.Add(a_definition.id, result);
			return result;
		}

		public KPICat GetCategory(string m_categoryId)
		{
			return m_categories[m_categoryId];
		}

		public KPISubCat GetSubCategory(string m_subcategoryId)
		{
			return m_subCategories[m_subcategoryId];
		}

		public KPIVal GetValue(string m_valueId)
		{
			return m_values[m_valueId];
		}
	}

	public class KPICat
	{
		public string m_id; //Needs to be completely unique
		public string m_dispayName;
		public Sprite m_icon;
		public List<KPISubCat> m_subCategories;
	}

	public class KPICatDefinition
	{
		public string id;
		public string dispayName;
		public string iconName;
	}

	public class KPISubCat
	{
		public KPICat m_category;
		public string m_id; //Needs to be completely unique
		public string m_dispayName;
		public ValueConversionUnit m_unit;
		public bool m_countrySpecific;
		public List<KPIVal> m_values;
	}

	public class KPISubCatDefinition
	{
		public string id;
		public string categoryId;
		public string dispayName;
		public string unit;
		public bool countrySpecific;
	}

	public class KPIVal
	{
		public KPISubCat m_subCategory;
		public string m_id; //Needs to be completely unique
		public string m_dispayName;
		public List<KPIValueData> m_data;

		public event Action<KPIVal> OnValueUpdated;

		public void AppendValueNames(List<string> a_result)
		{
			//TODO: 
		}

		public List<float?> GetValues(int a_month)
		{ }
	}

	public class KPIValDefinition
	{
		public string id;
		public string subCategoryId;
		public string dispayName;
	}

	public class KPIValueData
	{
		public int m_month;
		public float? m_value;
		public int m_country;
		public int m_otherIndex;
	}

	public class KPIValueDataObject
	{
		public string valueId;
		public int month;
		public float value;
		public int country;
		public int otherIndex;
	}
}
