using System.Linq;
using UnityEngine;

namespace CradleImpactTool
{
	public class LineData
	{
		public int impactId { get; set; }
		public string thickness { get; set; }
		public string title { get; set; }
		public string description { get; set; }

		public bool Validate(int a_index, ImpactObjectData a_impactData, GraphSettings a_graphSettings)
		{
			bool isValid = true;

			ImpactTypeData impactType = a_impactData.impactTypes.FirstOrDefault((ImpactTypeData impactItem) => impactItem.id == impactId);
			if (impactType == null)
			{
				Debug.LogError($"LineData indexed at {a_index} references ImpactTypeData ID {impactId}, which does not exist in the list of impact types provided.");
				isValid = false;
			}

			if (description == null || description.Length == 0)
			{
				Debug.LogWarning($"LineData indexed at {a_index} is missing a description.");
			}

			if (string.IsNullOrEmpty(thickness))
			{
				Debug.LogError($"LineData indexed at {a_index} thickness cannot be null or empty.");
				isValid = false;
			}
			else if (a_graphSettings.lineThicknesses == null || a_graphSettings.lineThicknesses?.ContainsKey(thickness) == false)
			{
				Debug.LogError($"LineData indexed at {a_index} references line thickness '{thickness}', which does not exist in the list of line thicknesses provided.");
				isValid = false;
			}

			return isValid;
		}
	}
}
