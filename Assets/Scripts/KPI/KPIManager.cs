using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using PlasticPipe.PlasticProtocol.Messages;

namespace MSP2050.Scripts
{
	public class KPIManager
	{
		Dictionary<string, KPICat> m_categories = new Dictionary<string, KPICat>();
		Dictionary<string, KPISubCat> m_subCategories = new Dictionary<string, KPISubCat>();
		Dictionary<string, KPIValueSet> m_values = new Dictionary<string, KPIValueSet>();

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
				m_countrySpecific = a_definition.countrySpecific,
				m_otherindexSpecific = a_definition.otherindexSpecific
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

		public KPIValueSet AddValue(KPIValDefinition a_definition)
		{
			KPIValueSet result;
			if (m_values.TryGetValue(a_definition.id, out result))
			{
				return result;
			}
			
			if (m_subCategories.TryGetValue(a_definition.subCategoryId, out var subCategory))
				result = subCategory.AddValueSet(a_definition);
			else
				Debug.LogWarning($"Trying to add KPI value {a_definition.dispayName} to a nonexistent subcategory {a_definition.subCategoryId}");
			
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

		public KPIValueSet GetValue(string m_valueId)
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
		public bool m_otherindexSpecific;
		public List<KPIVal> m_values;
		public Dictionary<string, KPIValueSet> m_valueSetsById;

		public KPIValueSet AddValueSet(KPIValDefinition a_definition, List<int> a_otherIndices = null, List<string> a_otherIndexNames = null)
		{
			KPIValueSet result = new KPIValueSet()
			{
				m_subCategory = this,
				m_id = a_definition.id,
				m_dispayName = a_definition.dispayName,
				m_data = new Dictionary<int, Dictionary<int, KPIVal>>()
			};

			if(m_countrySpecific)
			{
				if (m_otherindexSpecific)
				{
					foreach (Team team in SessionManager.Instance.GetTeams())
					{
						if (team.IsManager)
							continue;

						Dictionary<int, KPIVal> inner = new Dictionary<int, KPIVal>();
						for (int i = 0; i < a_otherIndices.Count; i++)
						{
							inner.Add(a_otherIndices[i], new KPIVal()
							{
								m_dispayName = $"{a_definition.dispayName} {team.name} {a_otherIndexNames[i]}",
								m_data = new Dictionary<int, KPIValueData>(),
								m_valueSet = result,
								m_otherIndex = a_otherIndices[i],
								m_country = team.ID
							});
						}
						result.m_data.Add(team.ID, inner);
					}
				}
				else
				{ 
					foreach(Team team in SessionManager.Instance.GetTeams())
					{
						if (team.IsManager)
							continue;

						Dictionary<int, KPIVal> inner = new Dictionary<int, KPIVal>();
						inner.Add(-1, new KPIVal()
						{
							m_dispayName = $"{a_definition.dispayName} {team.name}",
							m_data = new Dictionary<int, KPIValueData>(),
							m_valueSet = result,
							m_country = team.ID
						});
						result.m_data.Add(team.ID, inner);
					}
				}
			}
			else if (m_otherindexSpecific) 
			{
				Dictionary<int, KPIVal> inner = new Dictionary<int, KPIVal>();
				for(int i = 0; i < a_otherIndices.Count; i++)
				{
					inner.Add(a_otherIndices[i], new KPIVal() 
					{ 
						m_dispayName = $"{a_definition.dispayName} {a_otherIndexNames[i]}", 
						m_data = new Dictionary<int, KPIValueData>(), 
						m_valueSet = result,
						m_otherIndex = a_otherIndices[i]
					});
				}
				result.m_data.Add(-1, inner);
			}
			else
			{
				Dictionary<int, KPIVal> inner = new Dictionary<int, KPIVal>();
				inner.Add(-1, new KPIVal() 
				{ 
					m_dispayName = a_definition.dispayName, 
					m_data = new Dictionary<int, KPIValueData>(), 
					m_valueSet = result 
				});
				result.m_data.Add(-1, inner);
			}

			m_valueSetsById.Add(a_definition.id, result);
			return result;
		}
	}

	public class KPISubCatDefinition
	{
		public string id;
		public string categoryId;
		public string dispayName;
		public string unit;
		public bool countrySpecific;
		public bool otherindexSpecific;
	}

	public class KPIValueSet
	{
		public KPISubCat m_subCategory;
		public string m_id;
		public string m_dispayName;
		public Dictionary<int, Dictionary<int, KPIVal>> m_data; //Country, OtherIndex, Kpival

		public KPIVal GetValue()
		{
			return m_data.GetFirstValue().GetFirstValue();
		}
		public KPIVal GetValue(int a_countryId, int a_otherIndex)
		{
			if(m_data.TryGetValue(a_countryId, out var countryData))
			{
				if (countryData.TryGetValue(a_otherIndex, out var otherIndexData))
				{
					return otherIndexData;
				}
			}
			return null;
		}

		public List<KPIVal> GetAllValues()
		{
			List<KPIVal> result = new List<KPIVal>();
			foreach(var countryData in m_data)
			{
				foreach (var otherIndexData in countryData.Value)
				{
					result.Add(otherIndexData.Value);
				}
			}
			return result;
		}
	}

	public class KPIVal
	{
		public KPIValueSet m_valueSet;
		public string m_dispayName; //Displayname of id + country + otherind
		public int m_country = -1;
		public int m_otherIndex = -1;

		public Dictionary<int, KPIValueData> m_data; //Month, data

		public event Action<KPIVal> OnValueUpdated;

		public float? GetValue(int a_month)
		{
			if (m_data.TryGetValue(a_month, out var monthData))
			{
				return monthData.m_value;
			}
			return null;
		}

		public List<float?> GetValues(List<int> a_months)
		{
			List<float?> result = new List<float?>();
			foreach (int month in a_months)
			{
				if (m_data.TryGetValue(month, out var data))
				{
					result.Add(data.m_value);
				}
				else
					result.Add(null);
			}
			return result;
		}
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
