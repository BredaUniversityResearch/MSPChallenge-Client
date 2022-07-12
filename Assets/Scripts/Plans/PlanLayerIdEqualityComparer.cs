using System.Collections.Generic;

namespace MSP2050.Scripts
{
	/// <summary>
	/// Equality comparer class that only bases equality on plan layer ID. 
	/// </summary>
	public class PlanLayerIdEqualityComparer : IEqualityComparer<PlanLayer>
	{
		public static readonly PlanLayerIdEqualityComparer Instance = new PlanLayerIdEqualityComparer();

		public bool Equals(PlanLayer x, PlanLayer y)
		{
			return x.ID == y.ID;
		}

		public int GetHashCode(PlanLayer obj)
		{
			return obj.ID;
		}
	};
}