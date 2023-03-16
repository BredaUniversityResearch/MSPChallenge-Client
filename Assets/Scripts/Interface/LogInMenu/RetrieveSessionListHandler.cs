using System;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
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
			UriBuilder baseUrl;
			try
			{
				baseUrl = new UriBuilder(new Uri(hostname)); // not that Uri() only accepts "hostname" with a scheme
			}
			catch (UriFormatException e) // exception! meaning "hostname" does not include a scheme
			{
				// so create one. We use a secure base URL by default, unlike UriBuilder
				baseUrl = new UriBuilder($"{Uri.UriSchemeHttps}://{hostname}");
			}
			string scheme = baseUrl.Scheme;
			string host = baseUrl.Host;

			string address = $"{scheme}://{host}/ServerManager/api/getsessionslist.php";
			yield return TryRetrieveSessionList(address);

			// yes, we succeeded
			if (Success ||
				// or, we already tried an insecure URL
				baseUrl.Scheme == Uri.UriSchemeHttp)
			{
				yield break;
			}

			// ok, then let's try the insecure URL
			address = $"{Uri.UriSchemeHttp}://{host}/ServerManager/api/getsessionslist.php";
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