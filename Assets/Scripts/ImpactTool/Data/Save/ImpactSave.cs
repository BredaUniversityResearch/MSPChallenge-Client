using System.Collections.Generic;
using UnityEngine;

namespace CradleImpactTool
{
	public class ImpactSave
	{
		public Dictionary<string, CategorySave> categories = new Dictionary<string, CategorySave>();

		public bool Validate()
		{
			bool isValid = true;

			if (categories == null)
			{
				Debug.LogError($"ImpactSave cannot have a categories dictionary that is null.");
				isValid = false;
			}

			return isValid;
		}

		public bool TryGetItem(string a_categoryName, string a_itemName, out ItemSave a_item)
		{
			CategorySave category;
			if (categories.TryGetValue(a_categoryName, out category))
				return category.items.TryGetValue(a_itemName, out a_item);

			a_item = null;
			return false;
		}

		public ItemSave GetItem(string a_categoryName, string a_itemName)
		{
			CategorySave category;
			if (categories.TryGetValue(a_categoryName, out category) == false)
			{
				categories.Add(a_categoryName, new CategorySave());
				categories.TryGetValue(a_categoryName, out category);
			}

			return category.Get(a_itemName);
		}
	}
}
