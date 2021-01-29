using System;
using System.Diagnostics;
using UnityEngine;

namespace Networking.Profiling
{
	class RequestProfileEntry
	{
		public readonly ServerCommunication.ARequest targetRequest;
		private readonly Stopwatch timer = null;

		public readonly float RequestStartTime; //Request start time in seconds since start.

		public double QueuedTime //Time between start and remove from queue in ms
		{
			get;
			private set;
		}

		public double ResponseTime //Time between start and request finished.
		{
			get;
			private set;
		}

		public double ProcessTime //Time between start and spent processing the response of this request.
		{
			get;
			private set;
		}

		public RequestProfileEntry(ServerCommunication.ARequest request)
		{
			targetRequest = request;
			RequestStartTime = Time.realtimeSinceStartup;
			timer = Stopwatch.StartNew();
		}

		public void OnRemovedFromQueue()
		{
			QueuedTime = ((double)timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000.0;
		}

		public void OnResponseReceived()
		{
			ResponseTime = ((double)timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000.0;
		}

		public void OnResponseProcessed()
		{
			ProcessTime = ((double)timer.ElapsedTicks / (double)Stopwatch.Frequency) * 1000.0;
			timer.Stop();
		}
	}
}
