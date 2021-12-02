using UnityEngine;

namespace CradleImpactTool
{
	public class LinkData
	{
		public int fromId { get; set; }
		public int toId { get; set; }
		public LineData[] lines { get; set; }

		public bool Validate(ImpactObjectData a_impactData, GraphSettings a_graphSettings)
		{
			bool isValid = true;
			if (fromId <= 0 || toId <= 0)
			{
				Debug.LogError($"Invalid link: fromId ({fromId}) and toId ({toId}) require to be above zero.");
				isValid = false;
			}

			if (lines == null || lines.Length == 0)
			{
				Debug.LogError($"Link id \"{fromId} - {toId}\" has no lines listed.");
				return false;
			}

			for (int i = 0; i < lines.Length; i++)
			{
				isValid |= lines[i].Validate(i, a_impactData, a_graphSettings);
			}

			return isValid;
		}
	}
}
