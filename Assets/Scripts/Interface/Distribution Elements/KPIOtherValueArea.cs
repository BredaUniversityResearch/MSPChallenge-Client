using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class KPIOtherValueArea : MonoBehaviour
	{
		[Header("Prefabs")]
		public GameObject m_entryPrefab;
		public Transform m_entryParent;

		private Dictionary<string, KPIGroupBar> m_entries = new Dictionary<string, KPIGroupBar>();
    
		private void CreateBarForGrid(EnergyGrid a_grid, int a_country)
		{
			KPIGroupBar bar = Instantiate(m_entryPrefab, m_entryParent).GetComponent<KPIGroupBar>();
			m_entries.Add(a_grid.GetDatabaseID().ToString(), bar);
			bar.SetToEnergyValues(a_grid, a_country, a_grid.name);
		}

		private void UpdateBarForFishing(string a_name, Dictionary<int, float> a_values)
		{
			if (m_entries.TryGetValue(a_name, out var bar))
			{
				bar.UpdateFishingValues(a_values, a_name);
			}
			else
			{
				CreateBarForFishing(a_name, a_values);
			}
		}

		private void CreateBarForFishing(string a_name, Dictionary<int, float> a_values)
		{
			KPIGroupBar bar = Instantiate(m_entryPrefab, m_entryParent).GetComponent<KPIGroupBar>();
			m_entries.Add(a_name, bar);
			bar.UpdateFishingValues(a_values, a_name);
		}

		private void ClearBars()
		{
			foreach (KPIGroupBar bar in m_entries.Values)
			{
				Destroy(bar.gameObject);
			}

			m_entries.Clear();
		}

		public void SetBarsToGrids(List<EnergyGrid> a_grids, int a_country)
		{
			ClearBars();
			//Checks if grid is more than just a socket, is relevant for our country
			if (a_grids != null)
			{
				foreach (EnergyGrid grid in a_grids)
				{
					if (grid.ShouldBeShown && grid.CountryHasSocketInGrid(a_country))
					{
						CreateBarForGrid(grid, a_country);
					}
				}
			}
		}

		public void SetBarsToFishing(FishingDistributionSet a_distributionDelta)
		{
			foreach (KeyValuePair<string, Dictionary<int, float>> kvp in a_distributionDelta.GetValues())
			{
				UpdateBarForFishing(kvp.Key, kvp.Value);
			}
		}
	}
}
