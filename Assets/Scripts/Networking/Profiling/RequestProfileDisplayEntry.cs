using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Profiling
{
	//Entry representing a single request. This is what is shown in the ui.
	class RequestProfileDisplayEntry: MonoBehaviour
	{
		[SerializeField]
		private Text apiRequestUrl = null;

		[SerializeField]
		private Text requestStartTime = null;

		[SerializeField]
		private Text requestQueueTime = null;

		[SerializeField]
		private Text requestResponseTime = null;

		[SerializeField]
		private Text requestProcessTime = null;

		public void UpdateTimings(RequestProfileEntry entry)
		{
			requestQueueTime.text = entry.QueuedTime.ToString("N5", CultureInfo.InvariantCulture);
			requestResponseTime.text = (entry.ResponseTime - entry.QueuedTime).ToString("N5", CultureInfo.InvariantCulture);
			requestProcessTime.text = (entry.ProcessTime - entry.ResponseTime).ToString("N5", CultureInfo.InvariantCulture);
		}

		public void UpdateRequestInfo(RequestProfileEntry entry)
		{
			apiRequestUrl.text = entry.targetRequest.Url.Replace(Server.Url, "");
			requestStartTime.text = entry.RequestStartTime.ToString(CultureInfo.InvariantCulture);
		}
	}
}
