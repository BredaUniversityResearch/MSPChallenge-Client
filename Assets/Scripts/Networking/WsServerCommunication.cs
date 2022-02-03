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

		private readonly int _teamId;
		private readonly string _user;
		private double _lastUpdateTimestamp = 0;
		private readonly IWebsocketClient _client;

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
			this._teamId = teamId;
			this._user = user;
			
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
			
			_client = new WebsocketClient(Server.WsServerUri, factory);
			_client.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);
			_client.DisconnectionHappened.Subscribe(x =>
			{
				IsConnected = false;
			});
			_client.ReconnectionHappened.Subscribe(reconnectionInfo =>
			{
				if (!_client.IsStarted)
				{
					return;
				}
				SendStartingData();
				IsConnected = true;
			});
			_client.MessageReceived.Subscribe( responseMessage =>
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
						if (Math.Abs(updateObject.prev_update_time - _lastUpdateTimestamp) > Double.Epsilon)
						{
							SendStartingData(); // re-sync with server
							return;
						}
						// last update time matches, update it to the new one given by the server, continue processing
						Debug.Log("got update, prev: " + _lastUpdateTimestamp + ", new: " + updateObject.update_time);
						_lastUpdateTimestamp = updateObject.update_time;
					}
					catch (System.Exception e)
					{
						// do not update lastUpdateTimestamp and do not process payload
						return;
					}
					request.ProcessPayload(result.payload);
				}
			});     
		}

		public void Stop()
		{
			_client.Stop(WebSocketCloseStatus.NormalClosure, "Websocket connection closed");
			_client.IsReconnectionEnabled = false;
		}

		public void Start()
		{
			_client.Start();
		}

		private void SendStartingData()
		{
			dynamic obj = new JObject();
			obj.team_id = _teamId;
			obj.user = _user;
			obj.last_update_time = _lastUpdateTimestamp;
			_client.Send(obj.ToString());
		}
	}
}
