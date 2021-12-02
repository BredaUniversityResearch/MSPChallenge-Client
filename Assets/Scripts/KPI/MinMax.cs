namespace KPI
{
	public class MinMax
	{
		public float min { get; set; }
		public float max { get; set; }

		public MinMax(float min, float max)
		{
			this.min = min;
			this.max = max;
		}

		/// <summary>
		/// Returns a [0-1] value that indicates where the input value lies in the range
		/// </summary>
		public float GetRelative(float value)
		{
			if (value < min)
				return 0.0f;
			if (value > max)
				return 1.0f;
			return (value - min) / (max - min);
		}
	}
}