using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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

		private static readonly TimeSpan TokenRenewInterval = new TimeSpan(0, 55, 0);

		private string currentAccessToken = "";
		private string refreshToken = "";
		private DateTime lastTokenReceiveTime = DateTime.MinValue;
		private UnityWebRequest refreshTokenRequest = null;
		private int requestSessionAttempts = 0;

		private const int MAX_REQUEST_SESSION_ATTEMPTS = 10;

		public void Update()
		{
			if (refreshTokenRequest != null)
			{
				HandleRenewTokenRequest();
			}
			else if (requestSessionAttempts == 0 && DateTime.Now - lastTokenReceiveTime > TokenRenewInterval)
			{
				RefreshToken();
			}
		}
		private void RefreshToken()
		{
			requestSessionAttempts++;
			//This is circumventing the regular way of doing API requests via the ServerCommunication class as this is a more time-critical process.
			List<IMultipartFormSection> postData = new List<IMultipartFormSection>(1) { new MultipartFormDataSection("api_refresh_token", refreshToken) };
			refreshTokenRequest = UnityWebRequest.Post(Server.Url + Server.RefreshApiToken(), postData);
			refreshTokenRequest.SendWebRequest();
		}

		private void HandleRenewTokenRequest()
		{
			if (refreshTokenRequest.isDone)
			{
				string responseText = refreshTokenRequest.downloadHandler.text;
				RequestResult response = null;
				try
				{
					response = JsonConvert.DeserializeObject<RequestResult>(responseText);
				}
				catch (Exception e)
				{
					Debug.LogError("Failed to deserialize " + Server.RefreshApiToken() + " request, error: " + e.Message);
				}
				if (response != null && response.success)
				{
					ServerCommunication.RequestSessionResponse result = response.payload.ToObject<ServerCommunication.RequestSessionResponse>();
					currentAccessToken = result.api_access_token;
					refreshToken = result.api_refresh_token;
					lastTokenReceiveTime = DateTime.Now;
					Debug.Log("API token successfully renewed");
					requestSessionAttempts = 0;
				}
				else
				{
					if (requestSessionAttempts > MAX_REQUEST_SESSION_ATTEMPTS)
					{
						// fatal error, user has to quit the game
						string msg = "Failed to request new API token, response message: " + response?.message;
						Debug.LogError(msg);
						throw new Exception(msg);
					}

					string message = "Failed to renew api access token. response message: " + response?.message;
					if (refreshTokenRequest != null)
					{
						message += ", Request error: " + refreshTokenRequest.error;
					}
					//Only log warning, since this is not a fatal error just yet.
					Debug.LogWarning(message);
					RefreshToken();
				}

				refreshTokenRequest = null;
			}
		}

		public void SetAccessToken(string responseApiToken, string refreshApiToken)
		{
			currentAccessToken = responseApiToken;
			refreshToken = refreshApiToken;
			lastTokenReceiveTime = DateTime.Now;
		}

		public string FormatAccessToken()
		{
			return "Bearer " + currentAccessToken;
		}
	}
}
