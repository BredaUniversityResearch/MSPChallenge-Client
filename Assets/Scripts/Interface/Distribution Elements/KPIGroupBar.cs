using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class KPIGroupBar : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] TextMeshProUGUI m_title;
		[SerializeField] TextMeshProUGUI m_barValueText;

		[SerializeField] ValueConversionCollection m_valueConversionCollection = null;
		[SerializeField] DistributionFillBar m_fillBar = null;
		[SerializeField] Button m_viewButton;

		[Header("Prefabs")]
		[SerializeField] GameObject m_itemPrefab;
		[SerializeField] Transform m_itemParent;
		[SerializeField] GameObject m_countryBallPrefab;
		[SerializeField] Transform m_countryBallParent;

		private List<KPIGroupBarItem> m_items = new List<KPIGroupBarItem>();

		private KPIGroupBarItem CreateItem(int a_teamID, float a_val, string a_valueText)
		{
			Color col;
			if (a_teamID < 0)
				col = Color.white;
			else
				col = SessionManager.Instance.GetTeamByTeamID(a_teamID).color;

			// Generate item
			KPIGroupBarItem item = Instantiate(m_itemPrefab, m_itemParent).GetComponent<KPIGroupBarItem>();
			m_items.Add(item);

			// Set values
			item.teamGraphic.color = col;
			item.team = a_teamID;
			UpdateItem(item, a_val, a_valueText);
			return item;
		}

		private KPIGroupBarItem CreateItem(int a_teamID, float a_val, bool a_createFillWeights = true)
		{
			KPIGroupBarItem item = CreateItem(a_teamID, a_val, a_val.Abbreviated());

			// Create fill
			if (a_createFillWeights)
			{
				CalculateEcologyFillWeights();
			}

			return item;
		}

		private void UpdateItem(KPIGroupBarItem a_item, float a_value, string a_valueText)
		{
			a_item.value = a_value;
			a_item.numbers.text = a_valueText;

			if (m_fillBar != null)
			{
				m_fillBar.SetFill(a_item.team, a_value);
			}
		}

		private KPIGroupBarItem CreateEnergyItem(string a_title, string a_valueText)
		{
			// Generate item
			KPIGroupBarItem item = Instantiate(m_itemPrefab, m_itemParent.transform).GetComponent<KPIGroupBarItem>();
			m_items.Add(item);

			// Set values
			item.teamGraphic.gameObject.SetActive(false);
			item.numbers.text = a_valueText;
			item.title.text = a_title;
			return item;
		}

		private KPIGroupBarItem CreateEnergyItem(int a_teamID, string a_valueText)
		{
			// Generate item
			GameObject go = Instantiate(m_itemPrefab);
			KPIGroupBarItem item = go.GetComponent<KPIGroupBarItem>();
			m_items.Add(item);
			go.transform.SetParent(m_itemParent.transform, false);

			// Set values
			item.teamGraphic.color = SessionManager.Instance.GetTeamByTeamID(a_teamID).color;
			item.numbers.text = a_valueText;
			item.title.gameObject.SetActive(false);
			return item;
		}

		private KPIGroupBarItem SetItemAtIndexTo(int a_index, string a_title, string a_valueText)
		{
			if (m_items.Count <= a_index)
				return CreateEnergyItem(a_title, a_valueText);

			// Set item to values
			KPIGroupBarItem item = m_items[a_index];
			item.numbers.text = a_valueText;
			item.title.text = a_title;
			item.title.gameObject.SetActive(true);
			item.teamGraphic.gameObject.SetActive(false);
			return item;
		}

		private KPIGroupBarItem SetItemAtIndexTo(int a_index, int a_teamID, string a_valueText)
		{
			if (m_items.Count <= a_index)
				return CreateEnergyItem(a_teamID, a_valueText);

			// Set item to values
			KPIGroupBarItem item = m_items[a_index];
			item.teamGraphic.color = SessionManager.Instance.GetTeamByTeamID(a_teamID).color;
			item.numbers.text = a_valueText;
			item.title.gameObject.SetActive(false);
			item.teamGraphic.gameObject.SetActive(true);
			return item;
		}

		public void DestroyItem(KPIGroupBarItem a_item)
		{
			// Ecology
			if (m_fillBar != null)
			{
				m_fillBar.DestroyFill(a_item.team);
			}

			m_items.Remove(a_item);
			Destroy(a_item.gameObject);

			//if (m_items.Count <= 0)
			//{

			//	foldGraphic.enabled = false;
			//	SetExpanded(false);
			//}
		}

		public void CalculateEcologyFillWeights()
		{
			for (int i = 0; i < m_items.Count; i++)
			{
				m_fillBar.SetFill(m_items[i].team, m_items[i].value);
			}

			SortItems();
			SortFills();
		}

		public void SortItems()
		{
			for (int i = 0; i < m_items.Count; i++)
			{
				int siblingIndex = 0;
				foreach (Team team in SessionManager.Instance.GetTeams())
				{
					if (m_items[i].teamGraphic.color == team.color)
					{
						m_items[i].transform.SetSiblingIndex(siblingIndex);
					}
					++siblingIndex;
				}
			}
		}

		private void SortFills()
		{
			m_fillBar.SortFills();
		}

		public void UpdateFishingValues(Dictionary<int, float> a_values, string a_name)
		{
			m_title.text = a_name;
			foreach (KeyValuePair<int, float> kvp in a_values)
			{
				KPIGroupBarItem item = m_items.Find(obj => obj.team == kvp.Key);
				if (item == null)
				{
					CreateItem(kvp.Key, kvp.Value, a_name);
				}
				else
				{
					UpdateItem(item, kvp.Value, kvp.Value.Abbreviated());
				}
			}

			m_fillBar.CreateEmptyFill(FishingDistributionDelta.MAX_SUMMED_FISHING_VALUE, true);
		}

		public void SetToEnergyValues(EnergyGrid a_grid, int a_country, string a_name)
		{
			m_title.text = a_name;
			m_viewButton.onClick.RemoveAllListeners();
			m_viewButton.onClick.AddListener(() => a_grid.ShowGridOnMap());
			GameObject dots = m_countryBallParent.GetChild(0).gameObject;
			int numberCountryIcons = 0;
			float totalUsedPower = 0;
			int nextItemIndex = 0;

			foreach (KeyValuePair<int, CountryEnergyAmount> kvp in a_grid.energyDistribution.distribution)
			{
				if (a_grid.actualAndWasted == null)
					continue;
				float target = kvp.Value.expected;
				float received = a_grid.actualAndWasted.socketActual.ContainsKey(kvp.Key) ? a_grid.actualAndWasted.socketActual[kvp.Key] : 0;

				if (kvp.Key == a_country)
				{
					//Our team, put it in the group bar
					string formatString;
					if (kvp.Value.expected < 0)
					{
						formatString = "Sent {0} / {1} target";
					}
					else
					{
						formatString = "Got {0} / {1} target";
						totalUsedPower += received;
					}
					m_barValueText.text = string.Format(formatString, m_valueConversionCollection.ConvertUnit(received, ValueConversionCollection.UNIT_WATT).FormatAsString(), 
						m_valueConversionCollection.ConvertUnit(target, ValueConversionCollection.UNIT_WATT).FormatAsString());
				}
				else
				{
					//Other team, create an entry
					string formatString;
					if (kvp.Value.expected < 0)
					{
						//Send
						formatString = "Sent {0} / {1} target";
					}
					else
					{
						//Receive
						formatString = "Got {0} / {1} target";
						totalUsedPower += received;
					}

					SetItemAtIndexTo(nextItemIndex, kvp.Key, string.Format(formatString, m_valueConversionCollection.ConvertUnit(received, ValueConversionCollection.UNIT_WATT).FormatAsString(), 
						m_valueConversionCollection.ConvertUnit(target, ValueConversionCollection.UNIT_WATT).FormatAsString()));
					nextItemIndex++;
			
					//Create group distribution icons for countries
					if (numberCountryIcons < 3)
					{
						Image temp = Instantiate(m_countryBallPrefab, m_countryBallParent).GetComponent<Image>();
						temp.color = SessionManager.Instance.GetTeamByTeamID(kvp.Key).color;
					}
					numberCountryIcons++;
				}
			}

			//Create summary
			SetItemAtIndexTo(nextItemIndex, "Total  ", string.Format("Used {0} / {1} ({2})", m_valueConversionCollection.ConvertUnit(totalUsedPower, ValueConversionCollection.UNIT_WATT).FormatAsString(), 
				m_valueConversionCollection.ConvertUnit(a_grid.AvailablePower, ValueConversionCollection.UNIT_WATT).FormatAsString(), (totalUsedPower / (float)a_grid.AvailablePower).ToString("P1")));
			nextItemIndex++;

			//Clear unused items
			for (int i = nextItemIndex; i < m_items.Count; i++)
				Destroy(m_items[i]);
			m_items.RemoveRange(nextItemIndex, m_items.Count - nextItemIndex);

			dots.SetActive(numberCountryIcons > 3);
			dots.transform.SetAsLastSibling();
			m_countryBallParent.gameObject.SetActive(numberCountryIcons > 0);
		}
	}
}