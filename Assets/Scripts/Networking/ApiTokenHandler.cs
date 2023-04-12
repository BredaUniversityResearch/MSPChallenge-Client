using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace MSP2050.Scripts
{
	public class ApiTokenHandler
	{
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private class ApiTokenCheckAccessResponse
		{
			public enum EResult
			{
				Expired,
				UpForRenewal,
				Valid
			};

			public EResult status = EResult.Expired;
			public float time_remaining = 0.0f; //Time remaining for the provided token in seconds.
		};

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		private class ApiTokenResponse
		{
			public string token = "";
			public DateTime valid_until = DateTime.MinValue;
		};

		private static readonly TimeSpan TokenCheckInterval = new TimeSpan(0, 0, 15);

		private string currentAccessToken = "";
		private string recoveryToken = "";
		private DateTime lastTokenCheckTime = DateTime.MinValue;
		private UnityWebRequest currentTokenRequest = null;
		private UnityWebRequest renewTokenRequest = null;
		private int requestSessionAttempts = 0;

		private const int MAX_REQUEST_SESSION_ATTEMPTS = 10;

		public void Update()
		{
			if (requestSessionAttempts > 0)
			{
				return;
			}

			if (renewTokenRequest != null)
			{
				HandleRenewTokenRequest();
			}
			else if (currentTokenRequest != null)
			{
				HandleCheckAccessResponse();
			}
			else
			{
				if (DateTime.Now - lastTokenCheckTime > TokenCheckInterval && currentTokenRequest == null)
				{
					//This is circumventing the regular way of doing API requests via the ServerCommunication class as this is a more time-critical process.
					currentTokenRequest = UnityWebRequest.Get(Server.Url + Server.CheckApiAccess());
					currentTokenRequest.SetRequestHeader(ServerCommunication.ApiTokenHeader, currentAccessToken);
					currentTokenRequest.SendWebRequest();
				}
			}
		}

		private void HandleCheckAccessResponse()
		{
			if (currentTokenRequest.isDone)
			{
				string jsonResponse = currentTokenRequest.downloadHandler.text;
				RequestResult response = null;
				try
				{
					response = JsonConvert.DeserializeObject<RequestResult>(jsonResponse);	
				}
				catch (Exception e)
				{
					Debug.LogError("Failed to deserialize " + Server.CheckApiAccess() + " request, error: " + e.Message);
				}
				if (response != null && response.success == true)
				{
					ApiTokenCheckAccessResponse payload = response.payload.ToObject<ApiTokenCheckAccessResponse>();
					switch (payload.status)
					{
					case ApiTokenCheckAccessResponse.EResult.Expired:
						UnityEngine.Debug.LogWarning("Access token came back as expired... Recovering token...");
						RecoverToken();
						break;
					case ApiTokenCheckAccessResponse.EResult.UpForRenewal:
						RenewCurrentToken();
						break;
					case ApiTokenCheckAccessResponse.EResult.Valid:
						break;
					}
				}
				else
				{
					UnityEngine.Debug.LogWarning("Access token check request failed. Full response: " + jsonResponse + " Request error: " + currentTokenRequest.error);
				}

				lastTokenCheckTime = DateTime.Now;
				currentTokenRequest = null;
			}
		}

		private void RecoverToken()
		{
			List<IMultipartFormSection> postData = new List<IMultipartFormSection>(1) {new MultipartFormDataSection("expired_token", currentAccessToken)};
			renewTokenRequest = UnityWebRequest.Post(Server.Url + Server.RenewApiToken(), postData);
			renewTokenRequest.SetRequestHeader(ServerCommunication.ApiTokenHeader, recoveryToken);
			renewTokenRequest.SendWebRequest();
		}

		private void RenewCurrentToken()
		{
			renewTokenRequest = UnityWebRequest.Get(Server.Url + Server.RenewApiToken());
			renewTokenRequest.SetRequestHeader(ServerCommunication.ApiTokenHeader, currentAccessToken);
			renewTokenRequest.SendWebRequest();
		}

		private void RequestSession()
		{
			requestSessionAttempts++;
			int countryIndex = SessionManager.Instance.CurrentUserTeamID;
			ServerCommunication.Instance.RequestSession(
				countryIndex, SessionManager.Instance.CurrentUserName, RequestSessionSuccess, RequestSessionFailure,
				SessionManager.Instance.Password
			);
		}

		private void HandleRenewTokenRequest()
		{
			if (renewTokenRequest.isDone)
			{
				string responseText = renewTokenRequest.downloadHandler.text;
				RequestResult response = null;
				try
				{
					response = JsonConvert.DeserializeObject<RequestResult>(responseText);
				}
				catch (Exception e)
				{
					Debug.LogError("Failed to deserialize " + Server.RenewApiToken() + " request, error: " + e.Message);
				}
				if (response != null && response.success)
				{
					currentAccessToken = response.payload.ToObject<ApiTokenResponse>().token;
				}
				else
				{
					string message = "Failed to renew api access token. Message: " + response.message;
					if (currentTokenRequest != null)
					{
						message += ", Request error: " + currentTokenRequest.error;
					}
					// only log warning, since this is not a fatal error just yet.
					UnityEngine.Debug.LogWarning(message);

					// renewal of access token might fail if token is older than 35 min
					//   (=see Server's Security::TOKEN_DELETE_AFTER_TIME)
					// This means the token details has been deleted, and the server cannot retrieve them anymore,
					//   incl. its scope
					// So in this case, create a completely new one session and token using "RequestSession" call.
					RequestSession();
				}

				renewTokenRequest = null;
			}
		}

		public void SetAccessToken(string responseApiToken, string recoveryApiToken)
		{
			currentAccessToken = responseApiToken;
			recoveryToken = recoveryApiToken;
		}

		public string GetAccessToken()
		{
			return currentAccessToken;
		}

		void RequestSessionSuccess(ServerCommunication.RequestSessionResponse response)
		{
			requestSessionAttempts = 0; // yes, user can continue playing
			ServerCommunication.Instance.SetApiAccessToken(response.api_access_token, response.api_access_recovery_token);
			SessionManager.Instance.CurrentSessionID = response.session_id;
		}

		void RequestSessionFailure(ARequest request, string message)
		{
			string msg = "Failed to request new session, request error: " + message;
			if (requestSessionAttempts > MAX_REQUEST_SESSION_ATTEMPTS)
			{
				// fatal error, user has to quit the game
				UnityEngine.Debug.LogError(msg);
				throw new Exception(msg);
			}

			// only log warning, since this is not a fatal error just yet.
			UnityEngine.Debug.LogWarning(msg);
			RequestSession(); // try again
		}
	}
}
