using UnityEngine;

namespace CradleImpactTool
{
	public class ItemData
	{
		public int id { get; set; }
		public int category { get; set; }
		public string name { get; set; }
		public string icon { get; set; }

		public bool Validate()
		{
			bool isValid = true;
			if (name == null)
			{
				Debug.LogError($"Item {id} has no name.");
				name = $"<Unnamed Item ID {id}>"; // Temp name for future errors
				isValid = false;
			}

			if (id == 0)
			{
				Debug.LogError($"Item {name} does not have a valid ID. Item IDs can not be zero.");
				isValid = false;
			}

			// TODO: Validate icons
			return isValid;
		}
	}
}
