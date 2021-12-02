using System.Collections.Generic;
using UnityEngine;

namespace CradleImpactTool
{
	public class CategorySave
	{
		public Dictionary<string, ItemSave> items = new Dictionary<string, ItemSave>();

		public bool Validate()
		{
			bool isValid = true;

			if (items == null)
			{
				Debug.LogError($"CategorySave cannot have an items dictionary that is null.");
				isValid = false;
			}

			return isValid;
		}

		public ItemSave Get(string a_itemName)
		{
			ItemSave item;
			if (items.TryGetValue(a_itemName, out item) == false)
			{
				items.Add(a_itemName, new ItemSave());
				items.TryGetValue(a_itemName, out item);
			}

			return item;
		}
	}
}
