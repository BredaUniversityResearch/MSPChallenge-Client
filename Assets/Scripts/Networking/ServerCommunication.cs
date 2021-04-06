using System;
using UnityEngine;
using System.Collections.Generic;
using Assets.Networking;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine.Networking;
using Utility.Serialization;
using System.Text;

public static class ServerCommunication
{
	public const string ApiTokenHeader = "MSPAPIToken";
	public static readonly int[] REQUEST_TIMEOUT = { 1, 10, 30 };
	public enum EWebRequestFailureResponse { Log, Error, Crash }

	public abstract class ARequest
	{
		public UnityWebRequest Www;
		public string Url;
		public Action<ARequest, string> failureCallback;
		public int retriesRemaining;
		public bool expectMSPResultFormat = true;
		public int timeoutLevel = 1;

		public ARequest(string url, Action<ARequest, string> failureCallback, int retriesRemaining)
		{
			Url = url;
			this.failureCallback = failureCallback;
			this.retriesRemaining = retriesRemaining;
		}
		public abstract void ProcessPayload(JToken payload);
		public abstract void CreateRequest(Dictionary<string, string> defaultHeaders);
	}

	public abstract class Request<T> : ARequest
	{
		public Action<T> successCallback;

		public Request(string url, Action<T> successCallback, Action<ARequest, string> failureCallback, int retriesRemaining)
			: base(url, failureCallback, retriesRemaining)
		{
			this.successCallback = successCallback;
		}

		public override void ProcessPayload(JToken payload)
		{
			////If we expect a string, return the payload directly
			//if(payload is T)
			//{
			//	if (successCallback != null)
			//		successCallback.Invoke((T)Convert.ChangeType(payload, typeof(T)));
			//	return;
			//}
			bool success = false;
			T payloadContent = default(T);
			try
			{
				//Parse payload to expected type
				//T payloadContent = JsonConvert.DeserializeObject<T>(payload);

				//T payloadContent = payload.ToObject<T>();

				JsonSerializer serializer = new JsonSerializer();
				serializer.Converters.Add(new JsonConverterBinaryBool());
				payloadContent = payload.ToObject<T>(serializer);
				success = true;
			}
			catch (System.Exception e)
			{
				//Or invoke the failure callback if that fails
				failureCallback.Invoke(this, $"Failed to deserialize results from {Url}: {payload.ToString()}\nMessage: {e.Message}");
			}
			if (success && successCallback != null)
				successCallback.Invoke(payloadContent);
		}
	}

	public class RequestResult
	{
		public bool success;
		public string message;
		public JToken payload; 
	}

    public class FormRequest<T> : Request<T>
    {
        public List<IMultipartFormSection> formData;
		private bool addDefaultHeaders;
		private bool priority = false;

		public FormRequest(string url, List<IMultipartFormSection> formData, Action<T> successCallback, Action<ARequest, string> failureCallback, int retriesRemaining, bool addDefaultHeaders = true)
			: base(url, successCallback, failureCallback, retriesRemaining)
        {
            this.formData = formData;
			this.addDefaultHeaders = addDefaultHeaders;
		}

		public override void CreateRequest(Dictionary<string, string> defaultHeaders)
		{
			if (formData == null)
			{
				Www = UnityWebRequest.Get(Url);
			}
			else
			{
				Www = UnityWebRequest.Post(Url, formData);
			}
			if(defaultHeaders != null && addDefaultHeaders)
				AddHeaders(Www, defaultHeaders);
			Www.timeout = REQUEST_TIMEOUT[timeoutLevel];
		}
	}

    public class RawDataRequest<T> : Request<T>
    {
        public string data;
		private bool addDefaultHeaders;


		public RawDataRequest(string url, string data, Action<T> successCallback, Action<ARequest, string> failureCallback, int retriesRemaining, bool addDefaultHeaders = true)
			: base(url, successCallback, failureCallback, retriesRemaining)
		{
            Debug.Log("Created request with raw content: " + data);
            this.data = data;
			this.addDefaultHeaders = addDefaultHeaders;
		}

		public override void CreateRequest(Dictionary<string, string> defaultHeaders)
		{
			//Www = UnityWebRequest.Post(Url, data);
			//if (defaultHeaders != null && addDefaultHeaders)
			//	AddHeaders(Www, defaultHeaders);
			//Www.SetRequestHeader("Content-Type", "application/json");
			////Www.uploadHandler.contentType = "application/json";
			//Www.timeout = REQUEST_TIMEOUT;

			//Kevin: updated the webrequest creation as the version above is appearantly broken for custom content types
			Www = new UnityWebRequest(Url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
			Www.uploadHandler = new UploadHandlerRaw(bodyRaw);
			Www.downloadHandler = new DownloadHandlerBuffer();
			Www.timeout = REQUEST_TIMEOUT[timeoutLevel];
			Www.SetRequestHeader("Content-Type", "application/json");
			if (defaultHeaders != null && addDefaultHeaders)
				AddHeaders(Www, defaultHeaders);
		}
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

	private static GameObject doingSomethingWindow;
	private static float lastTimeInactive;

	private static List<ARequest> requests = new List<ARequest>();

	private static Queue<ARequest> requestsQueue = new Queue<ARequest>();

	private static List<WaitForConditionData> conditions = new List<WaitForConditionData>();
    private static Dictionary<int, AbstractOperation> operations = new Dictionary<int, AbstractOperation>();
    private static int nextOperationIndex = 0;

	public delegate bool TestDelegate();

	public delegate void TestSuccessfulDelegate();
	public delegate void TestSuccessfulConnectionDelegate(List<EnergyLineStringSubEntity> cables);
	public delegate void TestSuccessfulGridDelegate(HashSet<int> deleted, List<GridObject> newGrids);

	public delegate void ProfilerDelegate(ARequest targetRequest);
	public static event ProfilerDelegate OnRequestQueued; //Queued by the DoRequest
	public static event ProfilerDelegate OnRequestStarted;	//Removed from the queue and an actual WWW Request is done.
	public static event ProfilerDelegate OnRequestResponseReceived; //WWW request is received but not processed
	public static event ProfilerDelegate OnRequestResponseProcessed; //Request is processed by the game and is done.

	private static ApiTokenHandler tokenHandler = new ApiTokenHandler();

	static ServerCommunication()
	{
		lastTimeInactive = Time.time;
	}

	public static void CreateActivityWindow()
	{
		doingSomethingWindow = GameObject.Instantiate(Resources.Load<GameObject>("TransmittingDataWindow"));
		doingSomethingWindow.transform.SetParent(UIManager.GetInterfaceCanvas().transform);
		doingSomethingWindow.GetComponent<RectTransform>().localPosition = Vector3.zero;
		doingSomethingWindow.SetActive(false);
	}

	public static void DoPriorityRequest(string url, NetworkForm form, Action<string> successCallback, Action<ARequest, string> failureCallback)
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
	public static void DoRequest<T>(string url, NetworkForm form, Action<T> successCallback, Action<ARequest, string> failureCallback, int retriesOnFail = 3)
	{
		ARequest request = new FormRequest<T>(Server.Url + url, (form != null) ? form.Form : null, successCallback, failureCallback, retriesOnFail);
		requestsQueue.Enqueue(request);

		if (OnRequestQueued != null)
		{
			OnRequestQueued(request);
		}
	}

	public static void DoRequest<T>(string url, NetworkForm form, Action<T> successCallback, EWebRequestFailureResponse responseType = EWebRequestFailureResponse.Error, int retriesOnFail = 3)
	{
		switch(responseType)
		{
			case EWebRequestFailureResponse.Error:
				DoRequest<T>(url, form, successCallback, HandleRequestFailureError, retriesOnFail);
				break;
			case EWebRequestFailureResponse.Crash:
				DoRequest<T>(url, form, successCallback, HandleRequestFailureCrash, retriesOnFail);
				break;
			default:
				DoRequest<T>(url, form, successCallback, HandleRequestFailureLog, retriesOnFail);
				break;
		}
	}

	public static void DoRequest(string url, NetworkForm form, int retriesOnFail = 3)
	{
		DoRequest<string>(url, form, null, HandleRequestFailureError, retriesOnFail);
	}

	//Note: specifying a custom failure callback avoids all default ones, including automatic retries.
	public static void DoRequest<T>(string url, string rawData, Action<T> successCallback, Action<ARequest, string> failureCallback, int retriesOnFail = 0)
    {
        ARequest request = new RawDataRequest<T>(Server.Url + url, rawData, successCallback, failureCallback, retriesOnFail);
        requestsQueue.Enqueue(request);

        if (OnRequestQueued != null)
        {
            OnRequestQueued(request);
        }
    }

	public static void DoRequest<T>(string url, string rawData, Action<T> successCallback, EWebRequestFailureResponse responseType, int retriesOnFail = 0)
	{
		switch (responseType)
		{
			case EWebRequestFailureResponse.Error:
				DoRequest<T>(url, rawData, successCallback, HandleRequestFailureError, retriesOnFail);
				break;
			case EWebRequestFailureResponse.Crash:
				DoRequest<T>(url, rawData, successCallback, HandleRequestFailureCrash, retriesOnFail);
				break;
			default:
				DoRequest<T>(url, rawData, successCallback, HandleRequestFailureLog, retriesOnFail);
				break;
		}
	}

	public static void DoRequest(string url, string rawData, EWebRequestFailureResponse responseType = EWebRequestFailureResponse.Error, int retriesOnFail = 3)
	{
		DoRequest<string>(url, rawData, null, HandleRequestFailureError, retriesOnFail);
	}

	public static void DoExternalAPICall<T>(string url, Dictionary<int, SubEntity> subEntitiesToPass, Action<T> successCallback, Action<ARequest, string> failureCallback, int retriesOnFail = 0)
	{
		List<Feature> features = new List<Feature>(subEntitiesToPass.Count);
		foreach (var kvp in subEntitiesToPass)
		{
			features.Add(kvp.Value.GetGeoJSONFeature(kvp.Key));
		}
		FeatureCollection featureCollection = new FeatureCollection(features);
		string content = JsonConvert.SerializeObject(featureCollection);

		ARequest request = new RawDataRequest<T>(url, content, successCallback, failureCallback, retriesOnFail, false); //Needs to be seperate so the server URL is not added
		request.timeoutLevel = 2;
		request.expectMSPResultFormat = false;
		requestsQueue.Enqueue(request);

		if (OnRequestQueued != null)
		{
			OnRequestQueued(request);
		}
	}

	public static void WaitForCondition(TestDelegate condition, TestSuccessfulDelegate conditionTrueDelegate)
	{
		conditions.Add(new WaitForConditionData(condition, conditionTrueDelegate));
	}

    public static void AddOperation(AbstractOperation operation)
    {
        int currentIndex = nextOperationIndex;
        operations.Add(currentIndex, operation);
        operation.StartOperation(currentIndex, CompleteOperation);
        nextOperationIndex++;
    }

    private static void CompleteOperation(int index)
    {
        operations.Remove(index);
    }

	public static void Update(bool updateToken = true)
	{
		if (updateToken)
		{
			tokenHandler.Update();
		}

		while (requestsQueue.Count > 0 && requests.Count < 50)
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

		if (Main.IsDeveloper == false)
			return;

		if (doingSomethingWindow != null)
		{
			if (requests.Count == 0)
			{
				lastTimeInactive = Time.time;

				if (doingSomethingWindow.activeSelf)
				{
					doingSomethingWindow.SetActive(false);
				}
			}
			else
			{
				if (!doingSomethingWindow.activeSelf && Time.time - lastTimeInactive > 0.33f)
				{
					doingSomethingWindow.SetActive(true);
				}
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

	private static void HandleRequestFailureLog(ARequest request, string message)
	{
		if(request.retriesRemaining > 0)
		{
			Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
			RetryRequest(request);
		}
		else
			Debug.Log(message);
	}

	private static void HandleRequestFailureError(ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
			RetryRequest(request);
		}
		else
			Debug.LogError(message);
	}

	private static void HandleRequestFailureCrash(ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
			RetryRequest(request);
		}
		else
			throw new Exception("Webrequest failed with message: " + message);
	}

	public static void RetryRequest(ARequest request)
	{
		request.retriesRemaining--;
		request.CreateRequest(GetAuthenticationHeaders());
		requestsQueue.Enqueue(request);

		if (OnRequestQueued != null)
		{
			OnRequestQueued(request);
		}
	}
	
	private static Dictionary<string, string> GetAuthenticationHeaders()
	{
		return new Dictionary<string, string> {{ApiTokenHeader, tokenHandler.GetAccessToken() }};
	}

	public static void AddDefaultHeaders(UnityWebRequest request)
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

	public static void SetApiAccessToken(string responseApiToken, string recoveryApiToken)
	{
		tokenHandler.SetAccessToken(responseApiToken, recoveryApiToken);
	}
}
