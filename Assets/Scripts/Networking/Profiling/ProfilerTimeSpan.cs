namespace Networking.Profiling
{
	class ProfilerTimeSpan
	{
		public float StartTime
		{
			get;
			private set;
		}

		public float EndTime
		{
			get;
			private set;
		}

		public ProfilerTimeSpan(float startTime, float endTime)
		{
			Set(startTime, endTime);
		}

		public void Set(float startTime, float endTime)
		{
			StartTime = startTime;
			EndTime = endTime;
		}

		public bool Contains(float time)
		{
			return StartTime <= time && EndTime >= time;
		}
	}
}
