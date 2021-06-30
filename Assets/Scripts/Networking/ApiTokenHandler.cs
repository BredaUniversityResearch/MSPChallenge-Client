using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Assets.Networking
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

		private static readonly TimeSpan TokenCheckInterval = new TimeSpan(0, 0, 5);

		private string currentAccessToken = "";
		private string recoveryToken = "";
		private DateTime lastTokenCheckTime = DateTime.MinValue;
		private UnityWebRequest currentTokenRequest = null;
		private UnityWebRequest renewTokenRequest = null;

		public void Update()
		{
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
				ServerCommunication.RequestResult response = JsonConvert.DeserializeObject<ServerCommunication.RequestResult>(jsonResponse);
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

		private void HandleRenewTokenRequest()
		{
			if (renewTokenRequest.isDone)
			{
				string responseText = renewTokenRequest.downloadHandler.text;
				ServerCommunication.RequestResult response = JsonConvert.DeserializeObject<ServerCommunication.RequestResult>(responseText);
				if (response.success)
				{
					currentAccessToken = response.payload.ToObject<ApiTokenResponse>().token;
				}
				else
				{
					UnityEngine.Debug.LogError("Failed to renew api access token. Message: " + response.message + " Request error: " + currentTokenRequest.error);
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
	}
}
