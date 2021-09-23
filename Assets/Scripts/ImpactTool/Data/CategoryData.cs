using System.Collections.Generic;
using UnityEngine;

namespace CradleImpactTool
{
	public class CategoryData
	{
		public int id { get; set; }
		public string name { get; set; }

		public bool Validate()
		{
			bool isValid = true;
			if (name == null)
			{
				Debug.LogError($"Category {id} has no name.");
				name = $"<Unnamed Category ID {id}>"; // Temp name for future errors
				isValid = false;
			}

			if (id == 0)
			{
				Debug.LogError($"Category \"{name}\" does not have a valid ID. Category IDs can not be zero.");
				isValid = false;
			}

			return isValid;
		}
	}
}
