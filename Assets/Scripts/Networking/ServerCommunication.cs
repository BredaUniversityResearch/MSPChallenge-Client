using System;
using System.Collections.Generic;
using System.Text;
using GeoJSON.Net.Feature;
using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace MSP2050.Scripts
{
	public class ServerCommunication : MonoBehaviour
	{
		//consts / enums
		public const string ApiTokenHeader = "MSPAPIToken";
		public static readonly int[] REQUEST_TIMEOUT = { 1, 10, 30 };
		public enum EWebRequestFailureResponse { Log, Error, Crash }
		public const uint DEFAULT_MAX_REQUESTS = 5;
		public static uint maxRequests = DEFAULT_MAX_REQUESTS;

		private static ServerCommunication singleton;
		public static ServerCommunication Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<ServerCommunication>();
				return singleton;
			}
		}

		private float lastTimeInactive;

		//Requires reset
		private List<ARequest> requests = new List<ARequest>();
		private Queue<ARequest> requestsQueue = new Queue<ARequest>();
		private List<WaitForConditionData> conditions = new List<WaitForConditionData>();
		private Dictionary<int, AbstractOperation> operations = new Dictionary<int, AbstractOperation>();
		private int nextOperationIndex = 0;

		//Profiler
		public delegate bool TestDelegate();
		public delegate void TestSuccessfulDelegate();
		public delegate void TestSuccessfulConnectionDelegate(List<EnergyLineStringSubEntity> cables);
		public delegate void TestSuccessfulGridDelegate(HashSet<int> deleted, List<GridObject> newGrids);

		public delegate void ProfilerDelegate(ARequest targetRequest);
		public event ProfilerDelegate OnRequestQueued; //Queued by the DoRequest
		public event ProfilerDelegate OnRequestStarted;	//Removed from the queue and an actual WWW Request is done.
		public event ProfilerDelegate OnRequestResponseReceived; //WWW request is received but not processed
		public event ProfilerDelegate OnRequestResponseProcessed; //Request is processed by the game and is done.

		private ApiTokenHandler tokenHandler = new ApiTokenHandler();

		void Awake()
		{
			lastTimeInactive = Time.time;
			if (singleton != null && singleton != this)
				Destroy(this);
			else
			{
				singleton = this;
				DontDestroyOnLoad(gameObject);
			}
		}

		public void Reset()
		{
			requests = new List<ARequest>();
			requestsQueue = new Queue<ARequest>();
			conditions = new List<WaitForConditionData>();
			operations = new Dictionary<int, AbstractOperation>();
			nextOperationIndex = 0;
		}

		public void DoPriorityRequest(string url, NetworkForm form, Action<string> successCallback, System.Action<ARequest, string> failureCallback)
		{
			ARequest request = new FormRequest<string>(Server.Url + url, (form != null) ? form.Form : null, successCallback, failureCallback, 0);
			request.timeoutLevel = 0;
			requestsQueue.Enqueue(request);

			if (OnRequestQueued != null)
			{
				OnRequestQueued(request);
			}
		}
	
		//Note: specifying a custom failure callback avoids all default ones, including automatic retries.
		public ARequest DoRequest<T>(string url, NetworkForm form, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesOnFail = 3)
		{
			ARequest request = new FormRequest<T>(Server.Url + url, (form != null) ? form.Form : null, successCallback, failureCallback, retriesOnFail);
			requestsQueue.Enqueue(request);

			if (OnRequestQueued != null)
			{
				OnRequestQueued(request);
			}

			return request;
		}

		public ARequest DoRequest<T>(string url, NetworkForm form, Action<T> successCallback, EWebRequestFailureResponse responseType = EWebRequestFailureResponse.Error, int retriesOnFail = 3)
		{
			switch(responseType)
			{
				case EWebRequestFailureResponse.Error:
					return DoRequest<T>(url, form, successCallback, HandleRequestFailureError, retriesOnFail);
				case EWebRequestFailureResponse.Crash:
					return DoRequest<T>(url, form, successCallback, HandleRequestFailureCrash, retriesOnFail);
				default:
					return DoRequest<T>(url, form, successCallback, HandleRequestFailureLog, retriesOnFail);
			}
		}

		public ARequest DoRequest(string url, NetworkForm form, int retriesOnFail = 3)
		{
			return DoRequest<string>(url, form, null, HandleRequestFailureError, retriesOnFail);
		}

		//Note: specifying a custom failure callback avoids all default ones, including automatic retries.
		public ARequest DoRequest<T>(string url, string rawData, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesOnFail = 0)
		{
			ARequest request = new RawDataRequest<T>(Server.Url + url, rawData, successCallback, failureCallback, retriesOnFail);
			requestsQueue.Enqueue(request);

			if (OnRequestQueued != null)
			{
				OnRequestQueued(request);
			}

			return request;
		}

		public ARequest DoRequest<T>(string url, string rawData, Action<T> successCallback, EWebRequestFailureResponse responseType, int retriesOnFail = 0)
		{
			switch (responseType)
			{
				case EWebRequestFailureResponse.Error:
					return DoRequest<T>(url, rawData, successCallback, HandleRequestFailureError, retriesOnFail);
				case EWebRequestFailureResponse.Crash:
					return DoRequest<T>(url, rawData, successCallback, HandleRequestFailureCrash, retriesOnFail);
				default:
					return DoRequest<T>(url, rawData, successCallback, HandleRequestFailureLog, retriesOnFail);
			}
		}

		public ARequest DoRequest(string url, string rawData, EWebRequestFailureResponse responseType = EWebRequestFailureResponse.Error, int retriesOnFail = 3)
		{
			return DoRequest<string>(url, rawData, null, HandleRequestFailureError, retriesOnFail);
		}

		public void DoExternalAPICall<T>(string url, Dictionary<int, SubEntity> subEntitiesToPass, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesOnFail = 0)
		{
			List<Feature> features = new List<Feature>(subEntitiesToPass.Count);
			foreach (var kvp in subEntitiesToPass)
			{
				features.Add(kvp.Value.GetGeoJSONFeature(kvp.Key));
			}
			FeatureCollection featureCollection = new FeatureCollection(features);
			string content = JsonConvert.SerializeObject(featureCollection);

			ARequest request = new RawDataRequest<T>(url, content, successCallback, failureCallback, retriesOnFail, false); //Needs to be separate so the server URL is not added
			request.timeoutLevel = 2;
			request.expectMSPResultFormat = false;
			requestsQueue.Enqueue(request);

			if (OnRequestQueued != null)
			{
				OnRequestQueued(request);
			}
		}

		public void DoExternalAPICall<T>(string url, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesOnFail = 0)
		{
			ARequest request = new FormRequest<T>(url, null, successCallback, failureCallback, retriesOnFail, false); //Needs to be separate so the server URL is not added
			request.timeoutLevel = 2;
			request.expectMSPResultFormat = false;
			requestsQueue.Enqueue(request);

			if (OnRequestQueued != null)
			{
				OnRequestQueued(request);
			}
		}

		public void WaitForCondition(TestDelegate condition, TestSuccessfulDelegate conditionTrueDelegate)
		{
			conditions.Add(new WaitForConditionData(condition, conditionTrueDelegate));
		}

		public void AddOperation(AbstractOperation operation)
		{
			int currentIndex = nextOperationIndex;
			operations.Add(currentIndex, operation);
			operation.StartOperation(currentIndex, CompleteOperation);
			nextOperationIndex++;
		}

		private void CompleteOperation(int index)
		{
			operations.Remove(index);
		}

		public void UpdateCommunication(bool updateToken = true)
		{
			if (updateToken)
			{
				tokenHandler.Update();
			}

			while (requestsQueue.Count > 0 && requests.Count < maxRequests)
			{
				ARequest r = requestsQueue.Dequeue();

				r.CreateRequest(GetAuthenticationHeaders());
				r.Www.SendWebRequest();

				if (OnRequestStarted != null)
				{
					OnRequestStarted(r);
				}

				requests.Add(r);
			}

			for (int i = requests.Count - 1; i >= 0; --i)
			{
				if (requests[i].Www.isDone)
				{
					ARequest r = requests[i];
					requests.RemoveAt(i);

					if (OnRequestResponseReceived != null)
					{
						OnRequestResponseReceived(r);
					}

					HandleRequestResponse(r);

					if (OnRequestResponseProcessed != null)
					{
						OnRequestResponseProcessed(r);
					}
				}
			}

			for (int i = conditions.Count - 1; i >= 0; --i)
			{
				if (conditions[i].Condition())
				{
					WaitForConditionData c = conditions[i];
					conditions.RemoveAt(i);
					c.ConditionCompleted();
				}
			}
		}

		private static void HandleRequestResponse(ARequest request)
		{
			if (!string.IsNullOrEmpty(request.Www.error))
			{
				request.failureCallback.Invoke(request, $"Network error in request to url: {request.Www.url}\nError message: {request.Www.error}");
			}
			else
			{
				MemoryTraceWriter traceWriter = new MemoryTraceWriter();
				traceWriter.LevelFilter = System.Diagnostics.TraceLevel.Warning;

				if (request.expectMSPResultFormat)
				{
					bool processPayload = false;
					RequestResult result = null;
					try
					{
						result = JsonConvert.DeserializeObject<RequestResult>(request.Www.downloadHandler.text, new JsonSerializerSettings
						{
							TraceWriter = traceWriter,
							Error = (sender, errorArgs) =>
							{
								Debug.LogError("Unable to deserialize: '" + request.Www.downloadHandler.text + "'");
								Util.HandleDeserializationError(sender, errorArgs);
								Debug.LogError("Deserialization error: " + errorArgs.ErrorContext.Error);

							},
							Converters = new List<JsonConverter> { new JsonConverterBinaryBool() }
						});
						if (!result.success)
						{
							request.failureCallback.Invoke(request, result.message);
						}
						else
							processPayload = true;
					}
					catch (System.Exception e)
					{
						request.failureCallback.Invoke(request, $"Error deserializing message from request to url: {request.Www.url}\nError message: {e.Message}");
					}
					if (processPayload)
						request.ProcessPayload(result.payload);
				}
				else
				{
					JToken result = null;
					try
					{
						result = JsonConvert.DeserializeObject<JToken>(request.Www.downloadHandler.text, new JsonSerializerSettings
						{
							TraceWriter = traceWriter,
							Error = (sender, errorArgs) =>
							{
								Debug.LogError("Unable to deserialize: '" + request.Www.downloadHandler.text + "'");
								Util.HandleDeserializationError(sender, errorArgs);
								Debug.LogError("Deserialization error: " + errorArgs.ErrorContext.Error);

							},
							Converters = new List<JsonConverter> { new JsonConverterBinaryBool() }
						});
					}
					catch (System.Exception e)
					{
						request.failureCallback.Invoke(request, $"Error deserializing message from request to url: {request.Www.url}\nError message: {e.Message}");
						return;
					}
					request.ProcessPayload(result);
				}
			}
		}

		private void HandleRequestFailureLog(ARequest request, string message)
		{
			if(request.retriesRemaining > 0)
			{
				Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
				RetryRequest(request);
			}
			else
				Debug.Log(message);
		}

		private void HandleRequestFailureError(ARequest request, string message)
		{
			if (request.retriesRemaining > 0)
			{
				Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
				RetryRequest(request);
			}
			else
				Debug.LogError(message);
		}

		private void HandleRequestFailureCrash(ARequest request, string message)
		{
			if (request.retriesRemaining > 0)
			{
				Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
				RetryRequest(request);
			}
			else
				throw new Exception("Webrequest failed with message: " + message);
		}

		public void RetryRequest(ARequest request)
		{
			request.retriesRemaining--;
			request.CreateRequest(GetAuthenticationHeaders());
			requestsQueue.Enqueue(request);

			if (OnRequestQueued != null)
			{
				OnRequestQueued(request);
			}
		}
	
		private Dictionary<string, string> GetAuthenticationHeaders()
		{
			return new Dictionary<string, string> {{ApiTokenHeader, tokenHandler.GetAccessToken() }};
		}

		public void AddDefaultHeaders(UnityWebRequest request)
		{
			AddHeaders(request, GetAuthenticationHeaders());
		}

		public static void AddHeaders(UnityWebRequest request, Dictionary<string, string> headers)
		{
			foreach (KeyValuePair<string, string> header in headers)
			{
				request.SetRequestHeader(header.Key, header.Value);
			}
		}

		public void SetApiAccessToken(string responseApiToken, string recoveryApiToken)
		{
			tokenHandler.SetAccessToken(responseApiToken, recoveryApiToken);
		}

		public string GetApiAccessToken()
		{
			return tokenHandler.GetAccessToken();
		}

		public void RequestSession(
			int countryId, string userName, Action<RequestSessionResponse> successCallback,
			System.Action<ARequest, string> failureCallback, [CanBeNull] string password = null)
		{
			NetworkForm form = new NetworkForm();
			form.AddField("country_id", countryId);
			form.AddField("user_name", userName);
			if (password != null)
			{
				form.AddField("country_password", password);
			}

			if (!ApplicationBuildIdentifier.Instance.GetHasInformation())
				ApplicationBuildIdentifier.Instance.GetManifest();

			form.AddField("build_timestamp", ApplicationBuildIdentifier.Instance.GetBuildTime());

			DoRequest(Server.RequestSession(), form, successCallback, failureCallback);
		}

		public class RequestSessionResponse
		{
			public int session_id = 0;
			public string api_access_token = "";
			public string api_access_recovery_token = "";
		}

		public class WaitForConditionData
		{
			public TestDelegate Condition;
			public TestSuccessfulDelegate ConditionTrueDelegate;

			public WaitForConditionData(TestDelegate condition, TestSuccessfulDelegate conditionTrueDelegate)
			{
				Condition = condition;
				ConditionTrueDelegate = conditionTrueDelegate;
			}

			public virtual void ConditionCompleted()
			{
				ConditionTrueDelegate();
			}
		}
	}


}
