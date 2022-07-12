using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Websocket.Client;
using BatchRequestSuccessCallbacks = System.Collections.Generic.Dictionary<int, System.Action<MSP2050.Scripts.BatchExecutionResult>>;
using BatchRequestFailureCallbacks = System.Collections.Generic.Dictionary<int, System.Action<string>>;
using BatchRequestResultAndSuccessCallback =
	System.Collections.Generic.KeyValuePair<MSP2050.Scripts.BatchExecutionResult, System.Action<MSP2050.Scripts.BatchExecutionResult>>;
using BatchRequestResultAndFailureCallback = System.Collections.Generic.KeyValuePair<string, System.Action<string>>;

namespace MSP2050.Scripts
{
	public interface IWsServerCommunicationInteractor
	{
		public void Start();
		public void Stop();
		public void RegisterBatchRequestCallbacks(int batchId, Action<BatchExecutionResult> successCallback,
			System.Action<string> failureCallback);
		public void UnregisterBatchRequestCallbacks(int batchId);
		public bool? IsConnected();
	}

	public class WsServerCommunication : IWsServerCommunicationInteractor
	{
		private const string ApiTokenHeader = ServerCommunication.ApiTokenHeader;
		private const string GameSessionIdHeader = "GameSessionId";
		private bool? m_IsConnected = null;

		private readonly int m_TeamId;
		private readonly int m_UserId;
		private double m_LastUpdateTimestamp = 0;
		private readonly IWebsocketClient m_Client;

		private BatchRequestSuccessCallbacks m_BatchRequestSuccessCallbacks = new BatchRequestSuccessCallbacks();
		private BatchRequestFailureCallbacks m_BatchRequestFailureCallbacks = new BatchRequestFailureCallbacks();

		private Queue<BatchRequestResultAndSuccessCallback> m_BatchRequestResultAndSuccessCallbackQueue =
			new Queue<BatchRequestResultAndSuccessCallback>();
		private Queue<BatchRequestResultAndFailureCallback> m_BatchRequestResultAndFailureCallbackQueue =
			new Queue<BatchRequestResultAndFailureCallback>();

		private class UpdateRequest : ServerCommunication.Request<UpdateObject>
		{
			public UpdateRequest(string url, Action<UpdateObject> successCallback) :
				base(url, successCallback, HandleUpdateFailCallback, 1)
			{
			}

			public override void CreateRequest(Dictionary<string, string> defaultHeaders)
			{
			}

			private static void HandleUpdateFailCallback(ServerCommunication.ARequest request, string message)
			{
			}
		}

		private class BatchRequestResultHeaderData
		{
			public int batch_id;
		}

		public WsServerCommunication(int gameSessionId, int teamId, int userId,
			Action<UpdateObject> updateSuccessCallback)
		{
			this.m_TeamId = teamId;
			this.m_UserId = userId;

			var factory = new System.Func<ClientWebSocket>(() => {
				var client = new ClientWebSocket {
					Options = {
						KeepAliveInterval = TimeSpan.FromSeconds(5)
					}
				};
				client.Options.SetRequestHeader(ApiTokenHeader, ServerCommunication.GetApiAccessToken());
				client.Options.SetRequestHeader(GameSessionIdHeader, gameSessionId.ToString());
				return client;
			});

			Debug.Log(Server.WsServerUri);
			m_Client = new WebsocketClient(Server.WsServerUri, factory);
			m_Client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
			m_Client.DisconnectionHappened.Subscribe(x => {
				m_IsConnected = false;
			});
			m_Client.ReconnectionHappened.Subscribe(reconnectionInfo => {
				if (!m_Client.IsStarted)
				{
					return;
				}
				SendStartingData();
				m_IsConnected = true;
			});
			m_Client.MessageReceived.Subscribe(responseMessage => {
				MemoryTraceWriter traceWriter = new MemoryTraceWriter();
				traceWriter.LevelFilter = System.Diagnostics.TraceLevel.Warning;
				bool processPayload = false;
				ServerCommunication.RequestResult result = null;
				try
				{
					result = JsonConvert.DeserializeObject<ServerCommunication.RequestResult>(
						responseMessage.ToString(), new JsonSerializerSettings {
							TraceWriter = traceWriter,
							Error = (sender, errorArgs) => {
								Debug.LogError("Unable to deserialize: '" + responseMessage.ToString() + "'");
								Util.HandleDeserializationError(sender, errorArgs);
								Debug.LogError("Deserialization error: " + errorArgs.ErrorContext.Error);

							},
							Converters = new List<JsonConverter> {new JsonConverterBinaryBool()}
						});
				}
				catch (System.Exception e)
				{
					Debug.LogError(
						$"Error deserializing message from request to url: {Server.WsServerUri.AbsoluteUri}\nError message: {e.Message}");
				}
				ProcessPayload(result, updateSuccessCallback);
			});
		}

		public bool? IsConnected()
		{
			return m_IsConnected;
		}

		public void Update()
		{
			ProcessBatchRequests();
		}

		private void ProcessBatchRequests()
		{
			while (m_BatchRequestResultAndSuccessCallbackQueue.Count > 0)
			{
				BatchRequestResultAndSuccessCallback pair = m_BatchRequestResultAndSuccessCallbackQueue.Dequeue();
				pair.Value.Invoke(pair.Key);
			}
			while (m_BatchRequestResultAndFailureCallbackQueue.Count > 0)
			{
				BatchRequestResultAndFailureCallback pair = m_BatchRequestResultAndFailureCallbackQueue.Dequeue();
				pair.Value.Invoke(pair.Key);
			}
		}

		public void RegisterBatchRequestCallbacks(int batchId, Action<BatchExecutionResult> successCallback,
			Action<string> failureCallback)
		{
			UnregisterBatchRequestCallbacks(batchId);
			m_BatchRequestSuccessCallbacks.Add(batchId, successCallback);
			m_BatchRequestFailureCallbacks.Add(batchId, failureCallback);
		}

		public void UnregisterBatchRequestCallbacks(int batchId)
		{
			if (m_BatchRequestSuccessCallbacks.ContainsKey(batchId))
			{
				m_BatchRequestSuccessCallbacks.Remove(batchId);
			}
			if (m_BatchRequestFailureCallbacks.ContainsKey(batchId))
			{
				m_BatchRequestFailureCallbacks.Remove(batchId);
			}
		}

		private void ProcessPayload(ServerCommunication.RequestResult result,
			Action<UpdateObject> updateSuccessCallback)
		{
			switch (result.header_type)
			{
				case "Game/Latest":
					ProcessGameLatestPayload(result, updateSuccessCallback);
					break;
				case "Batch/ExecuteBatch":
					ProcessBatchExecuteBatchPayload(result);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		private void ProcessBatchExecuteBatchPayload(ServerCommunication.RequestResult result)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JsonConverterBinaryBool());
			BatchRequestResultHeaderData headerData =
				result.header_data.ToObject<BatchRequestResultHeaderData>(serializer);
			if (!result.success)
			{
				BatchRequestResultAndFailureCallback pair =
					new BatchRequestResultAndFailureCallback(result.message,
						m_BatchRequestFailureCallbacks[headerData.batch_id]);
				m_BatchRequestResultAndFailureCallbackQueue.Enqueue(pair);
				return;
			}

			// new scope
			{
				BatchExecutionResult batchExecutionResult = result.payload.ToObject<BatchExecutionResult>(serializer);
				BatchRequestResultAndSuccessCallback pair =
					new BatchRequestResultAndSuccessCallback(batchExecutionResult,
						m_BatchRequestSuccessCallbacks[headerData.batch_id]);
				m_BatchRequestResultAndSuccessCallbackQueue.Enqueue(pair);
			}

			UnregisterBatchRequestCallbacks(headerData.batch_id);
		}

		private void ProcessGameLatestPayload(ServerCommunication.RequestResult result,
			Action<UpdateObject> updateSuccessCallback)
		{
			if (!result.success)
			{
				return;
			}

			UpdateRequest request = new UpdateRequest(Server.Url, updateSuccessCallback);
			try
			{
				//Parse payload to expected type
				UpdateObject updateObject = request.ToObject(result.payload);
				// there is mismatch between the expected update time and given by the server
				if (Math.Abs(updateObject.prev_update_time - m_LastUpdateTimestamp) > Double.Epsilon)
				{
					SendStartingData(); // re-sync with server
					return;
				}
				// last update time matches, update it to the new one given by the server, continue processing
				Debug.Log("got update, prev: " + m_LastUpdateTimestamp + ", new: " + updateObject.update_time);
				m_LastUpdateTimestamp = updateObject.update_time;
			}
			catch (System.Exception e)
			{
				Debug.LogError("Exception in ProcessGameLatestPayload: " + e.Message + "\n" + e.StackTrace);
				Debug.LogError("update payload: " + result.payload);
				// do not update lastUpdateTimestamp and do not process payload
				return;
			}
			Debug.Log(result.payload.ToString().Substring(0, 80));
			request.ProcessPayload(result.payload);
		}

		public void Stop()
		{
			m_Client.Stop(WebSocketCloseStatus.NormalClosure, "Websocket connection closed");
			m_Client.IsReconnectionEnabled = false;
		}

		public void Start()
		{
			m_Client.Start();
		}

		private void SendStartingData()
		{
			JObject obj = new JObject();
			obj.Add("team_id", m_TeamId);
			obj.Add("user", m_UserId);
			obj.Add("last_update_time", m_LastUpdateTimestamp);
			m_Client.Send(obj.ToString());
		}
	}
}
