using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class DistributionGroup<T> : AbstractDistributionGroup where T : DistributionItem
	{
		//public Image changeIndicator;
		[SerializeField] protected TextMeshProUGUI title;
		[SerializeField] protected Transform distributionItemLocation;
		[SerializeField] protected GameObject itemPrefab;
		[SerializeField] protected DistributionFillBar distributionFillBar;

		protected bool changed;
		protected Color outlineCol;
		protected bool interactable = false;
		protected List<T> items = new List<T>();
		private bool initialised = false;

		public virtual void Initialise()
		{
			if (initialised)
				return;
			initialised = true;
		}

		public DistributionItem FindDistributionItemByCountryId(int countryId)
		{
			return items.Find(obj => obj.Country == countryId);
		}

		public override void ApplySliderValues(Plan plan, int index)
		{ Debug.LogError("ApplySliderValues with called on a group that doesn't implement the method."); }
		public override void SetSliderValues(Dictionary<int, float> planDeltaValues, Dictionary<int, float> initialValues)
		{ Debug.LogError("SetSliderValues with ecology parameters called on a group that doesn't implement the method."); }
		public override void SetSliderValues(EnergyGrid grid, EnergyGrid.GridPlanState state)
		{ Debug.LogError("SetSliderValues with energy parameters called on a group that doesn't implement the method."); }
    
		public override void SetName(string name)
		{
			title.text = name;
		}

		/// <summary>
		/// Create an item that can hold a slider
		/// </summary>
		public T CreateItem(int country, float maxValue)
		{
			// Generate item
			T item = CreateItem(country);
			item.SetMaximum(maxValue);
			item.SetSliderInteractability(interactable);
			return item;
		}

		/// <summary>
		/// Create an item that can hold a slider
		/// </summary>
		public T CreateItem(int country)
		{
			// Generate item
			GameObject go = Instantiate(itemPrefab);
			T item = go.GetComponent<T>();
			items.Add(item);
			go.transform.SetParent(distributionItemLocation, false);

			// Set values
			item.group = this;
			item.SetTeam(SessionManager.Instance.GetTeamByTeamID(country));
			item.SetSliderInteractability(interactable);

			return item;
		}

		/// <summary>
		/// Update the slider distribution for the given item
		/// </summary>
		public override void UpdateDistributionItem(DistributionItem updatedItem, float currentValue)
		{
			//U wot.
			updatedItem.SetValue(currentValue);
			updatedItem.ToText(updatedItem.GetDistributionValue(), PolicyLogicFishing.Instance.FishingDisplayScale, true);

			if ( distributionFillBar != null)
			{
				distributionFillBar.SetFill(updatedItem.Country, updatedItem.GetDistributionValue());
			}

			CheckIfChanged();
		}

		/// <summary>
		/// Sorts the (vertical) items based on color
		/// </summary>
		public virtual void SortItems()
		{
			// Sort by color
			items.Sort((lhs, rhs) => lhs.Country.CompareTo(rhs.Country));

			for (int i = 0; i < items.Count; ++i)
			{
				items[i].transform.SetSiblingIndex(i);
			}
		}

		protected virtual bool CheckIfChanged()
		{
			changed = false;
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].changed)
				{
					changed = true;
					//changeIndicator.DOFade(1f, 0.2f);
					break;
				}
			}

			//if (distributionFillBar != null)
			//{
			//	distributionFillBar.outline.color = changed ? Color.white : outlineCol;
			//}

			//if (!changed)
			//	changeIndicator.DOFade(0f, 0.2f);
			return changed;
		}

		public override void SetSliderInteractability(bool value)
		{
			interactable = value;
			foreach (DistributionItem item in items)
				item.SetSliderInteractability(value);
		}
	}
}
