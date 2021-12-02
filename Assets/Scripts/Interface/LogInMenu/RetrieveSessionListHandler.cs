using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography.X509Certificates;

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
		//string address = $"http://{hostname}/api/getsessionslist.php";
		//yield return TryRetrieveSessionList(address);
		//if (!Success)
		//{
		//    address = $"http://{hostname}/stable/servermanager/api/getsessionslist.php";
		//    yield return TryRetrieveSessionList(address);
		//    if (!Success)
		//    {
		//        address = $"http://{hostname}/ServerManager/api/getsessionslist.php";
		//  yield return TryRetrieveSessionList(address);
		//    }
		//}

		string address = $"https://{hostname}/ServerManager/api/getsessionslist.php";
		yield return TryRetrieveSessionList(address);
		if (!Success)
		{
			address = $"http://{hostname}/ServerManager/api/getsessionslist.php";
			yield return TryRetrieveSessionList(address);
		}
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