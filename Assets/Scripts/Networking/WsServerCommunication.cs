using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Utility.Serialization;
using Websocket.Client;

namespace Networking
{
	public class WsServerCommunication
	{
		private const string ApiTokenHeader = ServerCommunication.ApiTokenHeader;
		private const string GameSessionIdHeader = "GameSessionId";
		public bool? IsConnected = null;

		private readonly int m_TeamId;
		private readonly int m_UserId;
		private double m_LastUpdateTimestamp = 0;
		private readonly IWebsocketClient m_Client;

		private Dictionary<int, Action<BatchExecutionResult>> m_BatchRequestSuccessCallbacks = new Dictionary<int, Action<BatchExecutionResult>>();

		public Queue<KeyValuePair<Action<BatchExecutionResult>, BatchExecutionResult>> BatchRequestSuccessCallbackQueue = new Queue<KeyValuePair<Action<BatchExecutionResult>, BatchExecutionResult>>();

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
		
		public WsServerCommunication(int gameSessionId, int teamId, int userId, Action<UpdateObject> updateSuccessCallback)
		{
			this.m_TeamId = teamId;
			this.m_UserId = userId;
			
			var factory = new System.Func<ClientWebSocket>(() =>
			{
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
			m_Client.DisconnectionHappened.Subscribe(x =>
			{
				IsConnected = false;
			});
			m_Client.ReconnectionHappened.Subscribe(reconnectionInfo =>
			{
				if (!m_Client.IsStarted)
				{
					return;
				}
				SendStartingData();
				IsConnected = true;
			});
			m_Client.MessageReceived.Subscribe( responseMessage =>
			{
				MemoryTraceWriter traceWriter = new MemoryTraceWriter();
				traceWriter.LevelFilter = System.Diagnostics.TraceLevel.Warning;
				bool processPayload = false;
				ServerCommunication.RequestResult result = null;
				try
				{
					result = JsonConvert.DeserializeObject<ServerCommunication.RequestResult>(responseMessage.ToString(), new JsonSerializerSettings
					{
						TraceWriter = traceWriter,
						Error = (sender, errorArgs) =>
						{
							Debug.LogError("Unable to deserialize: '" + responseMessage.ToString() + "'");
							Util.HandleDeserializationError(sender, errorArgs);
							Debug.LogError("Deserialization error: " + errorArgs.ErrorContext.Error);

						},
						Converters = new List<JsonConverter> { new JsonConverterBinaryBool() }
					});
					if (result != null)
					{
						processPayload = result.success;	
					}
				}
				catch (System.Exception e)
				{
					Debug.LogError($"Error deserializing message from request to url: {Server.WsServerUri.AbsoluteUri}\nError message: {e.Message}");
				}
				if (processPayload)
				{
					ProcessPayload(result, updateSuccessCallback);
				}
			});
		}

		public void RegisterBatchRequestCallbacks(int batchId, Action<BatchExecutionResult> successCallback)
		{
			UnregisterBatchRequestCallbacks(batchId);
			m_BatchRequestSuccessCallbacks.Add(batchId, successCallback);
		}

		public void UnregisterBatchRequestCallbacks(int batchId)
		{
			if (m_BatchRequestSuccessCallbacks.ContainsKey(batchId))
			{
				m_BatchRequestSuccessCallbacks.Remove(batchId);
			}
		}

		private void ProcessPayload(ServerCommunication.RequestResult result, Action<UpdateObject> updateSuccessCallback)
		{
			switch (result.type)
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
			foreach (JToken token in result.payload.Children())
			{
				int batchId = Int32.Parse(token.Path);
				if (!m_BatchRequestSuccessCallbacks.ContainsKey(batchId))
				{
					continue;
				}

				JsonSerializer serializer = new JsonSerializer();
				serializer.Converters.Add(new JsonConverterBinaryBool());
				BatchExecutionResult batchExecutionResult = token.First.ToObject<BatchExecutionResult>(serializer);
				//m_BatchRequestSuccessCallbacks[batchId].Invoke(batchExecutionResult);

				KeyValuePair<Action<BatchExecutionResult>, BatchExecutionResult> pair = new KeyValuePair<Action<BatchExecutionResult>, BatchExecutionResult>(m_BatchRequestSuccessCallbacks[batchId], batchExecutionResult);
				BatchRequestSuccessCallbackQueue.Enqueue(pair);

				UnregisterBatchRequestCallbacks(batchId);
			}
		}

		private void ProcessGameLatestPayload(ServerCommunication.RequestResult result, Action<UpdateObject> updateSuccessCallback)
		{
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
