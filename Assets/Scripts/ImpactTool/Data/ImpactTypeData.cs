using UnityEngine;

namespace CradleImpactTool
{
	public class ImpactTypeData
	{
		public int id { get; set; }
		public string type { get; set; }
		public string iconIndicator { get; set; }
		public int iconAmount { get; set; }

		public bool Validate(GraphSettings a_graphSettings)
		{
			bool isValid = true;

			if (a_graphSettings.lineColors == null || a_graphSettings.lineColors.ContainsKey(type) == false)
			{
				Debug.LogError($"ImpactTypeData ID {id} references line color \"{type}\", which does not exist in the GraphSettings provided.");
				isValid = false;
			}

			if (a_graphSettings.lineIndicators == null || a_graphSettings.lineIndicators?.ContainsKey(iconIndicator) == false)
			{
				Debug.LogError($"ImpactTypeData ID {id} references line indicator \"{iconIndicator}\", which does not exist in the GraphSettings provided.");
				isValid = false;
			}

			return isValid;
		}
	}
}
