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
	    private int gameSessionId;
        private int teamId;
        private string user;
        private double lastUpdateTimestamp = 0;
        private readonly IWebsocketClient m_client;

        public class UpdateRequest : ServerCommunication.Request<UpdateObject>
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
	        this.gameSessionId = gameSessionId;
            this.teamId = teamId;
            this.user = user;
            
            var serverUri = new Uri(Server.Url);
            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket {
                    Options = {
                        KeepAliveInterval = TimeSpan.FromSeconds(5),
                        // Proxy = new WebProxy(serverUri.Host, 8888)
                        // ClientCertificates = ...
                    }
                };
                //client.Options.SetRequestHeader("Origin", "xxx");
                return client;
            });

            var scheme = serverUri.Scheme == "https" ? "wss" : "ws";
            var url = new Uri(scheme + "://" + serverUri.Host + "/ws/");
            m_client = new WebsocketClient(url, factory);
            m_client.ErrorReconnectTimeout = TimeSpan.FromSeconds(30);
            m_client.ReconnectionHappened.Subscribe(reconnectionInfo =>
            {
                if (!m_client.IsStarted)
                {
                    return;
                }
                SendStartingData();
            });
            m_client.MessageReceived.Subscribe( responseMessage =>
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
					processPayload = result.success;
				}
				catch (System.Exception e)
				{
					Debug.LogError($"Error deserializing message from request to url: {url}\nError message: {e.Message}");
				}
				if (processPayload)
				{
					UpdateRequest request = new UpdateRequest(Server.Url, updateSuccessCallback);
					try
					{
						//Parse payload to expected type
						UpdateObject updateObject = request.ToObject(result.payload);
						// there is mismatch between the expected update time and given by the server
						if (Math.Abs(updateObject.prev_update_time - lastUpdateTimestamp) > Double.Epsilon)
						{
							SendStartingData(); // re-sync with server
							return;
						}
						// last update time matches, update it to the new one given by the server, continue processing
						lastUpdateTimestamp = updateObject.update_time;
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
	        m_client.Stop(WebSocketCloseStatus.NormalClosure, "Websocket connection closed");
	        m_client.IsReconnectionEnabled = false;
        }

        public void Start()
        {
	        m_client.Start().Wait(); // this is blocking, todo
            SendStartingData();
        }

        private void SendStartingData()
        {
			dynamic obj = new JObject();
			obj.game_session_id = gameSessionId;
			obj.team_id = teamId;
			obj.user = user;
			obj.last_update_time = lastUpdateTimestamp;
			m_client.Send(obj.ToString());
        }
    }
}
