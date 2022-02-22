using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using JetBrains.Annotations;
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
		private readonly string m_User;
		private double m_LastUpdateTimestamp = 0;
		private readonly IWebsocketClient m_Client;

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
		
		public WsServerCommunication(int gameSessionId, int teamId, string user, Action<UpdateObject> updateSuccessCallback)
		{
			this.m_TeamId = teamId;
			this.m_User = user;
			
			var factory = new Func<ClientWebSocket>(() =>
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
			});     
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
			dynamic obj = new JObject();
			obj.team_id = m_TeamId;
			obj.user = m_User;
			obj.last_update_time = m_LastUpdateTimestamp;
			m_Client.Send(obj.ToString());
		}
	}
}
