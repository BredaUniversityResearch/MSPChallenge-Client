using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Websocket.Client;
using BatchRequestCallbacks = System.Collections.Generic.Dictionary<System.Guid, (System.Action<MSP2050.Scripts.BatchExecutionResult>, System.Action<string>)>;

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

		private BatchRequestCallbacks m_batchRequestCallbacks = new();
		private Queue<Action> m_callbackQueue = new();

		[SuppressMessage("ReSharper", "InconsistentNaming")] // need to match json
		private class BatchRequestResultHeaderData
		{
			public Guid batch_guid;
		}
		
		public event Action<UpdateObject> OnGameLatestUpdate;
		public event Action<List<ImmersiveSession>> OnImmersiveSessionUpdate;
		public event Action<string> OnImmersiveSessionUpdateFailed;		

		public WsServerCommunication(int a_gameSessionId, int a_teamId, int a_userId)
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
				ProcessPayload(result);
			});
		}

		public bool? IsConnected()
		{
			return m_isConnected;
		}

		public void Update()
		{
			while (m_callbackQueue.Count > 0)
			{
				m_callbackQueue.Dequeue().Invoke();
			}
		}

		public void RegisterBatchRequestCallbacks(Guid a_batchGuid, Action<BatchExecutionResult> a_successCallback,
			Action<string> a_failureCallback)
		{
			UnregisterBatchRequestCallbacks(a_batchGuid);
			m_batchRequestCallbacks.Add(a_batchGuid, (a_successCallback, a_failureCallback));
		}

		public void UnregisterBatchRequestCallbacks(Guid a_batchGuid)
		{
			if (m_batchRequestCallbacks.ContainsKey(a_batchGuid))
			{
				m_batchRequestCallbacks.Remove(a_batchGuid);
			}
		}

		private void ProcessPayload(RequestResult a_result)
		{
			switch (a_result.header_type)
			{
                case "ImmersiveSessions/Update":
                	ProcessImmersiveSessionsUpdatePayload(a_result);
					break;
				case "Game/Latest":
					ProcessGameLatestPayload(a_result);
					break;
				case "Batch/ExecuteBatch":
					ProcessBatchExecuteBatchPayload(a_result);
					break;
				default:
					throw new NotImplementedException();
			}
		}
		
		private void ProcessImmersiveSessionsUpdatePayload(RequestResult a_result)
		{
			if (!a_result.success)
			{
				return;
			}
			List<ImmersiveSession> sessions;
			try
			{
				//Parse payload to expected type
				JsonSerializer serializer = new JsonSerializer();
				serializer.Converters.Add(new JsonConverterBinaryBool());
				sessions = a_result.payload.ToObject<List<ImmersiveSession>>(serializer);
			}
			catch (System.Exception e)
			{
				Debug.LogError("Exception in ProcessImmersiveSessionsUpdatePayload: " + e.Message + "\n" + e.StackTrace);
				Debug.LogError("ImmersiveSessions/Update payload: " + a_result.payload);
				m_callbackQueue.Enqueue(() => OnImmersiveSessionUpdateFailed?.Invoke(e.Message + "\n" + e.StackTrace));
				return;
			}
			Debug.Log(a_result.payload.ToString().Substring(0, 80));
			m_callbackQueue.Enqueue(() => OnImmersiveSessionUpdate?.Invoke(sessions));
		}		

		private void ProcessBatchExecuteBatchPayload(RequestResult a_result)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JsonConverterBinaryBool());
			BatchRequestResultHeaderData headerData =
				a_result.header_data.ToObject<BatchRequestResultHeaderData>(serializer);
			if (!a_result.success && 
			    // If a batch is created server side, for this client, but not by the client itself,
			    //   the client will not have registered the batch call back, so check if it is there
			    // E.g. when a programmer injects some batches to test with for websocket server testing
			    m_batchRequestCallbacks.ContainsKey(headerData.batch_guid))
			{
				m_callbackQueue.Enqueue(() =>
					m_batchRequestCallbacks[headerData.batch_guid].Item2.Invoke(a_result.message));
				return;
			}

			// new scope
			if (m_batchRequestCallbacks.ContainsKey(headerData.batch_guid))
			{
				m_callbackQueue.Enqueue(() =>
					m_batchRequestCallbacks[headerData.batch_guid].Item1.Invoke(
						a_result.payload.ToObject<BatchExecutionResult>(serializer))
					);				
			}

			UnregisterBatchRequestCallbacks(headerData.batch_guid);
		}

		private void ProcessGameLatestPayload(RequestResult a_result)
		{
			if (!a_result.success)
			{
				return;
			}

			UpdateObject updateObject;
			try
			{
				//Parse payload to expected type
				JsonSerializer serializer = new JsonSerializer();
				serializer.Converters.Add(new JsonConverterBinaryBool());
				updateObject = a_result.payload.ToObject<UpdateObject>(serializer);	
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
			m_callbackQueue.Enqueue(() => OnGameLatestUpdate?.Invoke(updateObject));
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
