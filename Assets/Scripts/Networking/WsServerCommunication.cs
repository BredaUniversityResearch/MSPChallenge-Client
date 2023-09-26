using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Websocket.Client;
using BatchRequestSuccessCallbacks = System.Collections.Generic.Dictionary<System.Guid, System.Action<MSP2050.Scripts.BatchExecutionResult>>;
using BatchRequestFailureCallbacks = System.Collections.Generic.Dictionary<System.Guid, System.Action<string>>;
using BatchRequestResultAndSuccessCallback =
	System.Collections.Generic.KeyValuePair<MSP2050.Scripts.BatchExecutionResult, System.Action<MSP2050.Scripts.BatchExecutionResult>>;
using BatchRequestResultAndFailureCallback = System.Collections.Generic.KeyValuePair<string, System.Action<string>>;

namespace MSP2050.Scripts
{
	public interface IWsServerCommunicationInteractor
	{
		public void Start();
		public void Stop();
		public void RegisterBatchRequestCallbacks(Guid a_batchGuid, Action<BatchExecutionResult> a_successCallback,
			System.Action<string> a_failureCallback);
		public void UnregisterBatchRequestCallbacks(Guid a_batchGuid);
		public bool? IsConnected();
	}

	public class WsServerCommunication : IWsServerCommunicationInteractor
	{
		private const string API_TOKEN_HEADER = ServerCommunication.ApiTokenHeader;
		private const string GAME_SESSION_ID_HEADER = "Game-Session-Id";
		private bool? m_isConnected = null;

		private readonly int m_teamId;
		private readonly int m_userId;
		private double m_lastUpdateTimestamp = 0;
		private readonly IWebsocketClient m_client;

		private BatchRequestSuccessCallbacks m_batchRequestSuccessCallbacks = new BatchRequestSuccessCallbacks();
		private BatchRequestFailureCallbacks m_batchRequestFailureCallbacks = new BatchRequestFailureCallbacks();

		private Queue<BatchRequestResultAndSuccessCallback> m_batchRequestResultAndSuccessCallbackQueue =
			new Queue<BatchRequestResultAndSuccessCallback>();
		private Queue<BatchRequestResultAndFailureCallback> m_batchRequestResultAndFailureCallbackQueue =
			new Queue<BatchRequestResultAndFailureCallback>();

		private class UpdateRequest : Request<UpdateObject>
		{
			public UpdateRequest(string a_url, Action<UpdateObject> a_successCallback) :
				base(a_url, a_successCallback, HandleUpdateFailCallback, 1)
			{
			}

			public override void CreateRequest(Dictionary<string, string> a_defaultHeaders)
			{
			}

			private static void HandleUpdateFailCallback(ARequest a_request, string a_message)
			{
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")] // need to match json
		private class BatchRequestResultHeaderData
		{
			public Guid batch_guid;
		}

		public WsServerCommunication(int a_gameSessionId, int a_teamId, int a_userId,
			Action<UpdateObject> a_updateSuccessCallback)
		{
			m_teamId = a_teamId;
			m_userId = a_userId;

			var factory = new System.Func<ClientWebSocket>(() => {
				var client = new ClientWebSocket {
					Options = {
						KeepAliveInterval = TimeSpan.FromSeconds(5)
					}
				};
				client.Options.SetRequestHeader(API_TOKEN_HEADER, ServerCommunication.Instance.GetApiAccessToken());
				client.Options.SetRequestHeader(GAME_SESSION_ID_HEADER, a_gameSessionId.ToString());
				client.Options.SetRequestHeader("GameSessionId", a_gameSessionId.ToString()); // backwards compatible
				return client;
			});

			Debug.Log(Server.WsServerUri);
			m_client = new WebsocketClient(Server.WsServerUri, factory);
			m_client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
			m_client.DisconnectionHappened.Subscribe(a_x => {
				m_isConnected = false;
			});
			m_client.ReconnectionHappened.Subscribe(a_reconnectionInfo => {
				if (!m_client.IsStarted)
				{
					return;
				}
				SendStartingData();
				m_isConnected = true;
			});
			m_client.MessageReceived.Subscribe(a_responseMessage => {
				MemoryTraceWriter traceWriter = new MemoryTraceWriter();
				traceWriter.LevelFilter = System.Diagnostics.TraceLevel.Warning;
				bool processPayload = false;
				RequestResult result = null;
				try
				{
					result = JsonConvert.DeserializeObject<RequestResult>(
						a_responseMessage.ToString(), new JsonSerializerSettings {
							TraceWriter = traceWriter,
							Error = (a_sender, a_errorArgs) => {
								Debug.LogError("Unable to deserialize: '" + a_responseMessage.ToString() + "'");
								Util.HandleDeserializationError(a_sender, a_errorArgs);
								Debug.LogError("Deserialization error: " + a_errorArgs.ErrorContext.Error);

							},
							Converters = new List<JsonConverter> {new JsonConverterBinaryBool()}
						});
				}
				catch (System.Exception e)
				{
					Debug.LogError(
						$"Error deserializing message from request to url: {Server.WsServerUri.AbsoluteUri}\nError message: {e.Message}");
				}
				ProcessPayload(result, a_updateSuccessCallback);
			});
		}

		public bool? IsConnected()
		{
			return m_isConnected;
		}

		public void Update()
		{
			ProcessBatchRequests();
		}

		private void ProcessBatchRequests()
		{
			while (m_batchRequestResultAndSuccessCallbackQueue.Count > 0)
			{
				BatchRequestResultAndSuccessCallback pair = m_batchRequestResultAndSuccessCallbackQueue.Dequeue();
				pair.Value.Invoke(pair.Key);
			}
			while (m_batchRequestResultAndFailureCallbackQueue.Count > 0)
			{
				BatchRequestResultAndFailureCallback pair = m_batchRequestResultAndFailureCallbackQueue.Dequeue();
				pair.Value.Invoke(pair.Key);
			}
		}

		public void RegisterBatchRequestCallbacks(Guid a_batchGuid, Action<BatchExecutionResult> a_successCallback,
			Action<string> a_failureCallback)
		{
			UnregisterBatchRequestCallbacks(a_batchGuid);
			m_batchRequestSuccessCallbacks.Add(a_batchGuid, a_successCallback);
			m_batchRequestFailureCallbacks.Add(a_batchGuid, a_failureCallback);
		}

		public void UnregisterBatchRequestCallbacks(Guid a_batchGuid)
		{
			if (m_batchRequestSuccessCallbacks.ContainsKey(a_batchGuid))
			{
				m_batchRequestSuccessCallbacks.Remove(a_batchGuid);
			}
			if (m_batchRequestFailureCallbacks.ContainsKey(a_batchGuid))
			{
				m_batchRequestFailureCallbacks.Remove(a_batchGuid);
			}
		}

		private void ProcessPayload(RequestResult a_result,
			Action<UpdateObject> a_updateSuccessCallback)
		{
			switch (a_result.header_type)
			{
				case "Game/Latest":
					ProcessGameLatestPayload(a_result, a_updateSuccessCallback);
					break;
				case "Batch/ExecuteBatch":
					ProcessBatchExecuteBatchPayload(a_result);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		private void ProcessBatchExecuteBatchPayload(RequestResult a_result)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JsonConverterBinaryBool());
			BatchRequestResultHeaderData headerData =
				a_result.header_data.ToObject<BatchRequestResultHeaderData>(serializer);
			if (!a_result.success)
			{
				BatchRequestResultAndFailureCallback pair =
					new BatchRequestResultAndFailureCallback(a_result.message,
						m_batchRequestFailureCallbacks[headerData.batch_guid]);
				m_batchRequestResultAndFailureCallbackQueue.Enqueue(pair);
				return;
			}

			// new scope
			{
				BatchExecutionResult batchExecutionResult = a_result.payload.ToObject<BatchExecutionResult>(serializer);
				BatchRequestResultAndSuccessCallback pair =
					new BatchRequestResultAndSuccessCallback(batchExecutionResult,
						m_batchRequestSuccessCallbacks[headerData.batch_guid]);
				m_batchRequestResultAndSuccessCallbackQueue.Enqueue(pair);
			}

			UnregisterBatchRequestCallbacks(headerData.batch_guid);
		}

		private void ProcessGameLatestPayload(RequestResult a_result,
			Action<UpdateObject> a_updateSuccessCallback)
		{
			if (!a_result.success)
			{
				return;
			}

			UpdateRequest request = new UpdateRequest(Server.Url, a_updateSuccessCallback);
			try
			{
				//Parse payload to expected type
				UpdateObject updateObject = request.ToObject(a_result.payload);
				// there is mismatch between the expected update time and given by the server
				if (Math.Abs(updateObject.prev_update_time - m_lastUpdateTimestamp) > Double.Epsilon)
				{
					SendStartingData(); // re-sync with server
					return;
				}
				// last update time matches, update it to the new one given by the server, continue processing
				Debug.Log("got update, prev: " + m_lastUpdateTimestamp + ", new: " + updateObject.update_time);
				m_lastUpdateTimestamp = updateObject.update_time;
			}
			catch (System.Exception e)
			{
				Debug.LogError("Exception in ProcessGameLatestPayload: " + e.Message + "\n" + e.StackTrace);
				Debug.LogError("update payload: " + a_result.payload);
				// do not update lastUpdateTimestamp and do not process payload
				return;
			}
			Debug.Log(a_result.payload.ToString().Substring(0, 80));
			request.ProcessPayload(a_result.payload);
		}

		public void Stop()
		{
			m_client.Stop(WebSocketCloseStatus.NormalClosure, "Websocket connection closed");
			m_client.IsReconnectionEnabled = false;
		}

		public void Start()
		{
			m_client.Start();
		}

		private void SendStartingData()
		{
			JObject obj = new JObject();
			obj.Add("team_id", m_teamId);
			obj.Add("user", m_userId);
			obj.Add("last_update_time", m_lastUpdateTimestamp);
			m_client.Send(obj.ToString());
		}
	}
}
