using System.Collections.Generic;
using UnityEngine;

namespace CradleImpactTool
{
	public class ImpactObjectData
	{
		public CategoryData[] categories { get; set; }
		public LinkData[] links { get; set; }
		public ImpactTypeData[] impactTypes { get; set; }
		public ItemData[] items { get; set; }

		public bool Validate(GraphSettings a_graphSettings)
		{
			bool isValid = true;
			if (categories == null || categories.Length == 0)
			{
				Debug.LogError("ImpactObjectData requires to have at least one category.");
				isValid = false;
			}

			if (links == null || links.Length == 0)
			{
				Debug.LogError("ImpactObjectData requires to have at least one link.");
				isValid = false;
			}

			if (impactTypes == null || impactTypes.Length == 0)
			{
				Debug.LogError("ImpactObjectData requires to have at least one impact type.");
				isValid = false;
			}

			if (categories != null)
			{
				HashSet<int> categoryIds = new HashSet<int>();
				foreach (CategoryData category in categories)
				{
					categoryIds.Add(category.id);
					if (categoryIds.Count != categoryIds.Count)
					{
						Debug.LogError($"Category ID {category.id} is not unique.");
						isValid = false;
					}

					isValid |= category.Validate();
				}
			}

			if (links != null)
			{
				foreach (LinkData link in links)
				{
					isValid |= link.Validate(this, a_graphSettings);
				}
			}

			if (impactTypes != null)
			{
				HashSet<int> impactTypeIds = new HashSet<int>();
				foreach (ImpactTypeData impactType in impactTypes)
				{
					impactTypeIds.Add(impactType.id);
					if (impactTypeIds.Count != impactTypeIds.Count)
					{
						Debug.LogError($"ImpactType ID {impactType.id} is not unique.");
						isValid = false;
					}

					isValid |= impactType.Validate(a_graphSettings);
				}
			}

			if (items == null || items.Length == 0)
			{
				Debug.LogError("ImpactObjectData requires to have at least one item.");
				isValid = false;
			}

			HashSet<int> itemIds = new HashSet<int>();
			foreach (ItemData item in items)
			{
				itemIds.Add(item.id);
				if (itemIds.Count != itemIds.Count)
				{
					Debug.LogError($"Category ID \"{item.id}\" is not unique.");
					isValid = false;
				}

				isValid |= item.Validate();
			}

			return isValid;
		}
	}
}
