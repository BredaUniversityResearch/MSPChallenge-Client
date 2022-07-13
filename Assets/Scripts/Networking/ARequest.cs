using System;
using System.Collections.Generic;
using System.Text;
using GeoJSON.Net.Feature;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace MSP2050.Scripts
{
	public abstract class ARequest
	{
		public UnityWebRequest Www;
		public string Url;
		public System.Action<ARequest, string> failureCallback;
		public int retriesRemaining;
		public bool expectMSPResultFormat = true;
		public int timeoutLevel = 1;

		public ARequest(string url, System.Action<ARequest, string> failureCallback, int retriesRemaining)
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

		public Request(string url, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesRemaining)
			: base(url, failureCallback, retriesRemaining)
		{
			this.successCallback = successCallback;
		}

		public T ToObject(JToken a_Payload)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JsonConverterBinaryBool());
			return a_Payload.ToObject<T>(serializer);
		}

		public override void ProcessPayload(JToken a_Payload)
		{
			T payloadContent = default(T);
			try
			{
				//Parse payload to expected type
				payloadContent = ToObject(a_Payload);
			}
			catch (System.Exception e)
			{
				//Or invoke the failure callback if that fails
				failureCallback.Invoke(this, $"Failed to deserialize results from {Url}: {a_Payload.ToString()}\nMessage: {e.Message}");
				return;
			}
			ProcessPayload(payloadContent);
		}

		public void ProcessPayload(T a_PayloadContent)
		{
			if (successCallback == null)
			{
				return;
			}
			successCallback.Invoke(a_PayloadContent);
		}
	}

	public class RequestResult
	{
		public string header_type;
		public JToken header_data;
		public bool success;
		public string message;
		public JToken payload;
	}

	public class FormRequest<T> : Request<T>
	{
		public List<IMultipartFormSection> formData;
		private bool addDefaultHeaders;
		private bool priority = false;

		public FormRequest(string url, List<IMultipartFormSection> formData, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesRemaining, bool addDefaultHeaders = true)
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
			if (defaultHeaders != null && addDefaultHeaders)
				ServerCommunication.AddHeaders(Www, defaultHeaders);
			Www.timeout = ServerCommunication.REQUEST_TIMEOUT[timeoutLevel];
		}
	}

	public class RawDataRequest<T> : Request<T>
	{
		public string data;
		private bool addDefaultHeaders;


		public RawDataRequest(string url, string data, Action<T> successCallback, System.Action<ARequest, string> failureCallback, int retriesRemaining, bool addDefaultHeaders = true)
			: base(url, successCallback, failureCallback, retriesRemaining)
		{
			Debug.Log("Created request with raw content: " + data);
			this.data = data;
			this.addDefaultHeaders = addDefaultHeaders;
		}

		public override void CreateRequest(Dictionary<string, string> defaultHeaders)
		{
			Www = new UnityWebRequest(Url, "POST");
			byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
			Www.uploadHandler = new UploadHandlerRaw(bodyRaw);
			Www.downloadHandler = new DownloadHandlerBuffer();
			Www.timeout = ServerCommunication.REQUEST_TIMEOUT[timeoutLevel];
			Www.SetRequestHeader("Content-Type", "application/json");
			if (defaultHeaders != null && addDefaultHeaders)
				ServerCommunication.AddHeaders(Www, defaultHeaders);
		}
	}
}
