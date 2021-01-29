using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Networking;

public class PersistentDataLogIn : MonoBehaviour
{
	private int sessionId;
	private int countryId;
	private string userName;
	private MspGlobalData mspGlobalData;
	Dictionary<int, Team> teams;

	public void Initialize(int countryId, string userName, MspGlobalData mspGlobalData, Dictionary<int, Team> teams)
	{
		//NetworkForm form = new NetworkForm();

		//form.AddField("layer", planlayer.ID);
		//form.AddField("object", IssueManager.Serialize(planlayer));
		//form.AddField("active", 1);

		this.countryId = countryId;
		this.userName = userName;
		this.mspGlobalData = mspGlobalData;
		this.teams = teams;
		StartCoroutine(SetSessionID());
	}

	// Use this for initialization
	void Start()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene s, LoadSceneMode lsm)
	{
		TeamManager.InitializeUserValues(countryId, userName, sessionId, teams);
		Main.MspGlobalData = mspGlobalData;
		SceneManager.sceneLoaded -= OnSceneLoaded;

		Destroy(this.gameObject);
	}

	public IEnumerator SetSessionID()
	{
		UnityWebRequest www = UnityWebRequest.Get(Server.RequestSession());
		ServerCommunication.AddDefaultHeaders(www);

		yield return www.SendWebRequest();
		if (www.error != null)
		{
			Debug.LogError("Error when loading session ID: " + www.error);
		}

		sessionId = Util.ParseToInt(www.downloadHandler.text);

		SceneManager.LoadScene("MSP2050");
	}
}
