﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MSP2050.Scripts
{
	public class RetrieveSessionListHandler
	{
		public bool Success
		{
			get;
			private set;
		}

		public GameSessionList SessionList
		{
			get;
			private set;
		}

		private readonly string hostname;
		private readonly WWWForm formData;

		public RetrieveSessionListHandler(string hostname, WWWForm formData)
		{
			this.hostname = hostname;
			this.formData = formData;
		}

		public IEnumerator RetrieveListAsync()
		{
			var hostnameToUse = hostname;
			// note that Uri() only accepts "hostname" with a scheme, or without the scheme if :port is added,
			//   but then it messes up parsing, so to prevent errors, always force a scheme https if absent
			if (!(hostname.StartsWith(Uri.UriSchemeHttp) || hostname.StartsWith(Uri.UriSchemeHttps)))
			{
				// we use https as default
				hostnameToUse = "https://" + hostname;
			}
			
			// This should not go wrong, and if it does we get an UriFormatException
			UriBuilder baseUrl = new UriBuilder(new Uri(hostnameToUse));

			var scheme = baseUrl.Scheme;
			var host = baseUrl.Host;
			// if a port was specified, use it, otherwise it will be the default
			var port = baseUrl.Port;

			var address = $"{scheme}://{host}:{port}/ServerManager/api/getsessionslist.php";
			yield return TryRetrieveSessionList(address);

			// yes, we succeeded
			if (Success ||
				// or, we already tried an insecure URL
				baseUrl.Scheme == Uri.UriSchemeHttp)
			{
				yield break;
			}

			// ok, then let's try the insecure URLs
			// meaning, if we tried the default https port, then we need to switch to port 80 for http,
			//   also meaning that if a non-default value was given, just try that again, but on a http scheme
			if (port == 443) port = 80;
			address = $"{Uri.UriSchemeHttp}://{host}:{port}/ServerManager/api/getsessionslist.php";
			yield return TryRetrieveSessionList(address);
		}

		private IEnumerator TryRetrieveSessionList(string fullAddress)
		{
			UnityWebRequest www = UnityWebRequest.Post(fullAddress, formData);
			www.certificateHandler = new AcceptAllCertificates();

			yield return www.SendWebRequest();
			if (www.error == null)
			{
				try
				{
					SessionList = Util.DeserializeObject<GameSessionList>(www, true);
					if (SessionList != null)
					{
						Success = true;
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"Unexpected response from server when fetching session list: {e.Message}. Response: { www.downloadHandler.text}");
				}
			}
		}
	}

	class AcceptAllCertificates : CertificateHandler
	{
		protected override bool ValidateCertificate(byte[] certificateData)
		{
			return true;
		}
	}
}