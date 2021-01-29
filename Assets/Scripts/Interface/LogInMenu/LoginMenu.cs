using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using Utility.Serialization;
using TMPro;
using UnityEngine.Networking;

public class LoginMenu : MonoBehaviour
{
	private const string LOGIN_COUNTRY_NAME_STR = "LoginScreenCountryName";
	private const string LOGIN_COUNTRY_INDEX_STR = "LoginScreenCountryIndex";
	public const string LOGIN_EXPERTISE_INDEX_STR = "LoginScreenExpertiseIndex";
	private const string LOGIN_USER_NAME = "LoginScreenUserName";
	private const string LOGIN_SERVER_NAME = "LoginScreenServerName";
	private const string LOGIN_SERVER_ADRESS = "LoginScreenServerAdress";
	private const string GAME_SERVER_MANAGER_HOSTNAME = "server.mspchallenge.info";
	private const float ERROR_DISPLAY_TIME = 3f;

	private static LoginMenu instance;
	public static LoginMenu Instance => instance;

	private class RequestSessionResponse
	{
		public int session_id = 0;
		public string api_access_token = "";
		public string api_access_recovery_token = "";
	}

	public GameObject loginServer;
	public GameObject loginConnecting;
	public GameObject loginTeam;
	public GameObject connectButton;
	public InputField serverInputField;
	public GameObject gameSessionInputFieldContainer;
	public InputField gameSessionInputField;
	public Dropdown serverEndPointDropdown;
	public CustomInputField nameInputField;
	public CustomInputField passwordInputField;
	public GameObject passwordContainer;
	public CustomDropdown countryDropDown;
	public CustomDropdown expertiseDropDown;
	public GameObject expertiseDropDownContainer;
	public Button[] refreshButtons;
	public RegionSettingsAsset regionSettings;

	[Header("Server list")]
	public Transform gameSessionDisplayParent;
	public GameObject gameSessionPrefab;
	public TextMeshProUGUI serverInfoName;
	public TextMeshProUGUI serverInfoState;
	public TextMeshProUGUI serverInfoCreated;
	public TextMeshProUGUI serverInfoEnd;
	public TextMeshProUGUI serverInfoConfigName;
	public TextMeshProUGUI serverInfoConfigVersion;
	public TextMeshProUGUI serverInfoSimulation;
	public TextMeshProUGUI serverInfoPlayers;
	public ToggleGroup gameSessionToggleGroup;
	public Toggle serverListTab;
	//public Toggle customAddressTab;
	public GameObject serverListInfoObject;
	public GameObject serverListInfoButton;
	public GameObject serverListTabObject;
	//public GameObject customAddresssTabObject;
	public CustomInputField serverAdressInputField;
	public Button resetServerAdressButton;

	[Header("Demo tab")]
	public Toggle demoTab;
	public GameObject demoRefreshLoading;
	public GameObject demoTabObject;
	public GameObject nsDemoNotFoundText;
	public GameObject bsDemoNotFoundText;
	public GameObject crDemoNotFoundText;
	public Button nsDemoButton;
	public Button bsDemoButton;
	public Button crDemoButton;
	private GameSession nsDemoSession;
	private GameSession bsDemoSession;
	private GameSession crDemoSession;

	[Header("Notifications")]
	public TextMeshProUGUI serverListNotification;
	public GameObject serverListNotificationLoading;
	public GameObject serverSelectErrorContainer;
	public TextMeshProUGUI serverSelectErrorMessage;
	public GameObject loginErrorContainer;
	public TextMeshProUGUI loginErrorMessage;
	private float errorHideTimer;

	private List<GameSessionDisplay> gameSessionDisplayPool;
	private GameSession selectedSession;

	private TeamImporter teamImporter;

	private Dictionary<string, int> teamsIDByCountryName;
	private int expectedServerListID = 0;

	private void Awake()
	{
		instance = this;
		if (SystemInfo.systemMemorySize < 8000)
			DialogBoxManager.instance.NotificationWindow("Device not supported", "The current device does not satisfy MSP Challenge's minimum requirements. Effects may range from none to the program feeling unresponsive and/or crashing. Switching to another device is recommended.", null);
	}

	private void Start()
	{
		teamImporter = GetComponent<TeamImporter>();
		teamsIDByCountryName = new Dictionary<string, int>();
		if (!IsApiEndpointDropdownVisible())
		{
			serverEndPointDropdown.value = 0;
			serverEndPointDropdown.gameObject.SetActive(false);
		}

		if (!IsSessionEndpointVisible())
		{
			gameSessionInputField.text = "";
			gameSessionInputFieldContainer.gameObject.SetActive(false);
		}

		countryDropDown.value = 0;
		nameInputField.text = PlayerPrefs.GetString(LOGIN_USER_NAME, "");
		string serverName = PlayerPrefs.GetString(LOGIN_SERVER_NAME, "-");
		if (serverName != "-" && serverName != "Localhost")
		{
			serverInputField.text = serverName;
		}

		gameSessionDisplayPool = new List<GameSessionDisplay>();
		serverAdressInputField.text = PlayerPrefs.GetString(LOGIN_SERVER_ADRESS, GAME_SERVER_MANAGER_HOSTNAME);
		RefreshServerList();
		//LoadImages();

		serverListTab.onValueChanged.AddListener(b =>
		{
			serverListTabObject.SetActive(b);
		});
		//customAddressTab.onValueChanged.AddListener(b => {
		//    customAddresssTabObject.SetActive(b);
		//});
		demoTab.onValueChanged.AddListener(b =>
		{
			demoTabObject.SetActive(b);
			connectButton.SetActive(!b);
		});
		nsDemoButton.onClick.AddListener(
			() =>
			{
				if (nsDemoSession == null)
					return;
				selectedSession = nsDemoSession;
				ConnectButtonPressed();
			});
		bsDemoButton.onClick.AddListener(
			() =>
			{
				if (bsDemoSession == null)
					return;
				selectedSession = bsDemoSession;
				ConnectButtonPressed();
			});
		crDemoButton.onClick.AddListener(
			() =>
			{
				if (crDemoSession == null)
					return;
				selectedSession = crDemoSession;
				ConnectButtonPressed();
			});

		serverAdressInputField.onEndEdit.AddListener(
			(s) => RefreshServerList());

		resetServerAdressButton.onClick.AddListener(() =>
		{
			serverAdressInputField.text = GAME_SERVER_MANAGER_HOSTNAME;
			RefreshServerList();
		});

		string[] args = Environment.GetCommandLineArgs();
		foreach (string s in args)
			if (s == "demo")
				demoTab.isOn = true;
	}

	private void Update()
	{
		if(errorHideTimer > 0)
		{
			errorHideTimer -= Time.deltaTime;
			if(errorHideTimer <= 0)
			{
				serverSelectErrorContainer.SetActive(false);
				loginErrorContainer.gameObject.SetActive(false);
			}
		}
		ServerCommunication.Update(false);
	}

	//bool ShowingServerList
	//{
	//    get { return serverListTabObject.activeSelf; }
	//    set
	//    {
	//        serverListTabObject.SetActive(value);
	//        customAddresssTabObject.SetActive(!value);
	//    }
	//}

	public void RefreshServerList()
	{
		SelectGameSession(null);
		serverSelectErrorContainer.SetActive(false);
		serverListNotification.gameObject.SetActive(true);
		serverListNotificationLoading.SetActive(true);
		demoRefreshLoading.SetActive(true);
		serverListNotification.text = "Fetching sessions";
		foreach (Button button in refreshButtons)
			button.interactable = false;
		foreach (GameSessionDisplay obj in gameSessionDisplayPool)
			obj.Disable();
		expectedServerListID++;
		StartCoroutine(GetServerList(expectedServerListID));
		StartCoroutine(RefreshDemoServerList());
	}

	public void SelectGameSession(GameSession newSelectedGameSession)
	{
		selectedSession = newSelectedGameSession;
		if (selectedSession == null)
		{
			gameSessionToggleGroup.SetAllTogglesOff();
			serverInfoName.text = "";
			serverInfoState.text = "";
			serverInfoCreated.text = "";
			serverInfoEnd.text = "";
			serverInfoConfigName.text = "";
			serverInfoConfigVersion.text = "";
			serverInfoSimulation.text = "";
			serverInfoPlayers.text = "";
		}
		else
		{
			//Display info
			serverInfoName.text = selectedSession.name;
			if (selectedSession.session_state == GameSession.SessionState.Healthy)
				serverInfoState.text = selectedSession.game_state.ToString();
			else
				serverInfoState.text = selectedSession.session_state.ToString();
			serverInfoCreated.text = selectedSession.GetStartTime();
			serverInfoEnd.text = selectedSession.GetEndTime();
			serverInfoConfigName.text = selectedSession.config_file_name;
			serverInfoConfigVersion.text = selectedSession.config_version_version.ToString();
			serverInfoSimulation.text = selectedSession.game_start_year.ToString() + " - " + (selectedSession.game_start_year + (selectedSession.game_end_month / 12));
			serverInfoPlayers.text = selectedSession.players_active.ToString();
		}
	}

	IEnumerator RefreshDemoServerList()
	{
		nsDemoSession = null;
		bsDemoSession = null;
		crDemoSession = null;

		WWWForm form = new WWWForm();
		form.AddField("client_version", ApplicationBuildIdentifier.FindBuildIdentifier().GetSvnRevisionNumber());
		form.AddField("visibility", "private");
		form.AddField("demo_servers", 1);

		RetrieveSessionListHandler handler = new RetrieveSessionListHandler(GAME_SERVER_MANAGER_HOSTNAME, form);
		yield return handler.RetrieveListAsync();
		if (handler.Success)
		{
			foreach (GameSession session in handler.SessionList.sessionslist)
			{
				switch (session.region)
				{
				case "northsee":
				{
					nsDemoSession = session;
					break;
				}
				case "balticline":
				{
					bsDemoSession = session;
					break;
				}
				case "simcelt":
				{
					crDemoSession = session;
					break;
				}
				}
			}

			nsDemoButton.interactable = nsDemoSession != null;
			nsDemoNotFoundText.SetActive(nsDemoSession == null);
			bsDemoButton.interactable = bsDemoSession != null;
			bsDemoNotFoundText.SetActive(bsDemoSession == null);
			crDemoButton.interactable = crDemoSession != null;
			crDemoNotFoundText.SetActive(crDemoSession == null);
		}
		demoRefreshLoading.SetActive(false);
	}

	IEnumerator GetServerList(int serverListID)
	{
		WWWForm form = new WWWForm();
		form.AddField("visibility", 0);
		form.AddField("client_version", ApplicationBuildIdentifier.FindBuildIdentifier().GetSvnRevisionNumber());

#if UNITY_EDITOR
		string host = "localhost/dev/";
#else
		string host = "localhost";
#endif
		if (!string.IsNullOrEmpty(serverAdressInputField.text))
		{
			host = serverAdressInputField.text.Trim(' ', '\r', '\n', '\t');
			PlayerPrefs.SetString(LOGIN_SERVER_ADRESS, host);
		}

		RetrieveSessionListHandler handler = new RetrieveSessionListHandler(host, form);
		yield return handler.RetrieveListAsync();

		if (expectedServerListID == serverListID) //Only resolve result if we haven't refreshed in the meantime
		{
			if (handler.Success)
			{
				if (handler.SessionList != null && handler.SessionList.sessionslist != null)
				{
					if (handler.SessionList.sessionslist.Length == 0)
					{
						serverListNotification.text = "No available sessions";
						serverListNotificationLoading.SetActive(false);
					}
					else
					{
						serverListNotification.gameObject.SetActive(false);
					}

					GameSession[] sessions = handler.SessionList.sessionslist;
					//sessions.RemoveAll(obj => obj.session_state != GameSession.SessionState.Healthy);
					for (int i = 0; i < sessions.Length; i++)
					{
						if (i < gameSessionDisplayPool.Count)
						{
							gameSessionDisplayPool[i].SetGameSession(sessions[i], gameSessionToggleGroup);
						}
						else
						{
							GameSessionDisplay sessionDisplay = GameObject.Instantiate(gameSessionPrefab, gameSessionDisplayParent).GetComponent<GameSessionDisplay>();
							gameSessionDisplayPool.Add(sessionDisplay);
							sessionDisplay.SetGameSession(sessions[i], gameSessionToggleGroup);
							sessionDisplay.selectionCallback = SelectGameSession;
						}
					}
				}
				else
				{
					serverListNotification.gameObject.SetActive(false);
				}
			}
			else
			{
				serverListNotification.gameObject.SetActive(false);
				serverSelectErrorMessage.text = "Failed to fetch session list";
				serverSelectErrorContainer.SetActive(true);
			}

			foreach (Button button in refreshButtons)
			{
				button.interactable = true;
			}
		}
	}

	//OnValueChanged for team name dropdown
	public void OnValueChanged()
	{
		passwordContainer.SetActive(UserRequiresPassword(countryDropDown.options[countryDropDown.value].text));
	}

	private bool UserRequiresPassword(string userName)
	{
		bool requiresPassword = false;
		MspGlobalData globalData = teamImporter.MspGlobalData;
		if (globalData != null)
		{
			if (userName == globalData.user_admin_name || userName == globalData.user_region_manager_name)
			{
				requiresPassword = globalData.user_admin_has_password;
			}
			else
			{
				requiresPassword = globalData.user_common_has_password;
			}
		}

		return requiresPassword;
	}

	//Called by the first Connect button in the login menu (binded on gameobject)
	public void ConnectButtonPressed()
	{
		if (selectedSession == null)
		{
			return;
		}
		else if(selectedSession.session_state == GameSession.SessionState.Request || selectedSession.session_state == GameSession.SessionState.Initializing)
		{
			serverSelectErrorContainer.SetActive(true);
			errorHideTimer = ERROR_DISPLAY_TIME;
			serverSelectErrorMessage.text = "Connecting failed: Session is initializing, please try again later.";
			return;
		}
		else if (selectedSession.session_state != GameSession.SessionState.Healthy)
		{
			serverSelectErrorContainer.SetActive(true);
			errorHideTimer = ERROR_DISPLAY_TIME;
			serverSelectErrorMessage.text = "Connecting failed: Session has been archived or initialization failed.";
			return;
		}
		else
		{
			//Connect to selected server
			//Uri hostUri = new Uri(selectedSession.game_server_address);
			//Server.Host = hostUri.Host;
			//Server.Endpoint = "dev";
			Server.Host = selectedSession.game_server_address;
			Server.Endpoint = selectedSession.endpoint ?? "";
			Server.GameSessionId = selectedSession.id;
			PlayerPrefs.SetString(LOGIN_SERVER_NAME, selectedSession.game_server_address);
		}

		//If Successful
		teamImporter.OnImportComplete += OnTeamImportFinished;
		teamImporter.ImportGlobalData();
		loginServer.SetActive(false);
		serverSelectErrorContainer.SetActive(false);
		loginConnecting.SetActive(true);
	}

	public void ReturnToLoginServer()
	{
		loginConnecting.SetActive(false);
		loginTeam.SetActive(false);
		loginServer.SetActive(true);
		RefreshServerList();
	}

	private void ShowErrorMessage(string message)
	{
		loginErrorContainer.gameObject.SetActive(true);
		errorHideTimer = ERROR_DISPLAY_TIME;
		loginErrorMessage.text = message;
	}

	//Called by the ok button when trying to log in to team (bound on gameobject)
	public void TryLoginToTeam()
	{
		if (countryDropDown.options.Count == 0)
		{
			Debug.LogError("There were no options in the countryDropDown, this should never happen!");
			return;
		}

		if (nameInputField.text == "")
		{
			ShowErrorMessage("Please fill in a Username");
			return;
		}
		else
		{
			//Store username on login attempt
			PlayerPrefs.SetString(LOGIN_USER_NAME, nameInputField.text);
		}

		//Store selected country on loginattempt in playerprefs
		int countryDropdownIndex = countryDropDown.value;
		string countryName = countryDropDown.options[countryDropdownIndex].text;
		PlayerPrefs.SetInt(LOGIN_COUNTRY_INDEX_STR, countryDropdownIndex);
		PlayerPrefs.SetString(LOGIN_COUNTRY_NAME_STR, countryName);
		if (expertiseDropDownContainer.gameObject.activeSelf)
			PlayerPrefs.SetInt(LOGIN_EXPERTISE_INDEX_STR, expertiseDropDown.value);
		else
			PlayerPrefs.SetInt(LOGIN_EXPERTISE_INDEX_STR, -1);

		int countryIndex = teamsIDByCountryName[countryName];

		int buildVersion = 0;
		ApplicationBuildIdentifier buildIdentifier = ApplicationBuildIdentifier.FindBuildIdentifier();
		if (buildIdentifier != null)
		{
			buildVersion = buildIdentifier.GetSvnRevisionNumber();
		}

		NetworkForm form = new NetworkForm();
		form.AddField("country_id", countryIndex);
		if (passwordContainer.activeInHierarchy)
			form.AddField("country_password", passwordInputField.text);
		form.AddField("build_version", buildVersion);
		ServerCommunication.DoRequest<RequestSessionResponse>(Server.RequestSession(), form, (response) => RequestSessionSuccess(response, countryIndex), RequestSessionFailure);
	}

	void RequestSessionSuccess(RequestSessionResponse response, int countryIndex)
	{
		//Continue to game
		loginConnecting.SetActive(true);
		loginTeam.SetActive(false);

		ServerCommunication.SetApiAccessToken(response.api_access_token, response.api_access_recovery_token);
		TeamManager.InitializeUserValues(countryIndex, nameInputField.text, response.session_id, teamImporter.teams);
		Main.MspGlobalData = teamImporter.MspGlobalData;

		SceneManager.LoadScene("MSP2050");
	}

	void RequestSessionFailure(ServerCommunication.ARequest request, string message)
	{
		ShowErrorMessage(message);
		Debug.LogError(message);
	}

	public void OnTeamImportFinished(bool success)
	{
		teamImporter.OnImportComplete -= OnTeamImportFinished;

		if (success)
		{
			loginConnecting.SetActive(false);
			loginTeam.SetActive(true);

			//Populate the dropdown list with countries as soon as the team importer is finished
			List<TMP_Dropdown.OptionData> tOptionList = new List<TMP_Dropdown.OptionData>();
			foreach (KeyValuePair<int, Team> team in teamImporter.teams)
			{
				tOptionList.Add(new TMP_Dropdown.OptionData(team.Value.name));
				if (!teamsIDByCountryName.ContainsKey(team.Value.name))
				{
					teamsIDByCountryName[team.Value.name] = team.Key;
				}
			}
			countryDropDown.ClearOptions();
			countryDropDown.AddOptions(tOptionList);

			//Load countryDropdown index from playerprefs
			string countryName = PlayerPrefs.GetString(LOGIN_COUNTRY_NAME_STR, "");
			bool indexSet = false;
			if (countryName != "")
			{
				int countryIndex = PlayerPrefs.GetInt(LOGIN_COUNTRY_INDEX_STR, -1);
				if (countryIndex != -1)
				{
					if (countryIndex < countryDropDown.options.Count)
					{
						if (countryDropDown.options[countryIndex].text == countryName)
						{
							countryDropDown.value = countryIndex;
							indexSet = true;
						}
					}
				}
			}
			if (!indexSet)
				countryDropDown.value = 0;

			//Load expertise definitions and populate the dropdown
			if (teamImporter.MspGlobalData.expertise_definitions == null || teamImporter.MspGlobalData.expertise_definitions.Length == 0)
			{
				expertiseDropDownContainer.gameObject.SetActive(false);
				PlayerPrefs.SetInt(LOGIN_EXPERTISE_INDEX_STR, -1);
			}
			else
			{
				expertiseDropDownContainer.gameObject.SetActive(true);
				tOptionList = new List<TMP_Dropdown.OptionData>(teamImporter.MspGlobalData.expertise_definitions.Length);
				foreach (var expertise in teamImporter.MspGlobalData.expertise_definitions)
				{
					tOptionList.Add(new TMP_Dropdown.OptionData(expertise.name));
				}

				expertiseDropDown.ClearOptions();
				expertiseDropDown.AddOptions(tOptionList);

				int expertiseIndex = PlayerPrefs.GetInt(LOGIN_EXPERTISE_INDEX_STR, -1);
				if (expertiseIndex >= 0 && expertiseIndex < tOptionList.Count)
					expertiseDropDown.value = expertiseIndex;
				else
					expertiseIndex = 0;
				expertiseDropDown.value = expertiseIndex;
			}

			OnValueChanged();
		}
		else
		{
			loginConnecting.SetActive(false);
			loginServer.SetActive(true);
			errorHideTimer = ERROR_DISPLAY_TIME;
			serverSelectErrorMessage.text = "Connection failed";
			serverSelectErrorContainer.SetActive(true);
		}
	}

	public void QuitApplication()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
		Main.QuitGame();
	}

	private bool IsApiEndpointDropdownVisible()
	{
		return Main.IsDeveloper;
	}

	private bool IsSessionEndpointVisible()
	{
		return true;//Main.IsDeveloper;
	}

	public void SetInfoWindowExpanded(bool expanded)
	{
		serverListInfoObject.SetActive(expanded);
		serverListInfoButton.SetActive(!expanded);
	}
}

public class GameSessionList
{
	public string status;
	public string message;
	public int count;
	public GameSession[] sessionslist;
}

public class GameSession
{
	public enum GameState { Setup, Simulation, Play, Pause, End }
	public enum SessionState { Request, Initializing, Healthy, Failed, Archived }

	public int id;
	public string name;
	public int game_config_version_id;
	public int game_server_id;
	public int watchdog_server_id;
	public int game_creation_time;
	public int game_start_year;
	public int game_end_month;
	public int game_running_til_time;
	public SessionState session_state;
	public GameState game_state;
	//public int game_visibility;
	public int players_active;
	public int players_past_hour;
	public string game_server_name;
	public string game_server_address;
	public string watchdog_name;
	public string watchdog_address;
	public int config_version_version;
	public string config_version_message;
	public string config_file_name;
	public string config_file_description;
	public string region;
	public string endpoint;

	public string GetStartTime()
	{
		System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		dtDateTime = dtDateTime.AddSeconds(game_creation_time).ToLocalTime();
		return dtDateTime.ToShortDateString();
	}

	public string GetEndTime()
	{
		System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		dtDateTime = dtDateTime.AddSeconds(game_running_til_time).ToLocalTime();
		return dtDateTime.ToShortDateString();
	}
}


