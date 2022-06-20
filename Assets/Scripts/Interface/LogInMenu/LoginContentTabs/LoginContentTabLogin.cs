﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LoginContentTabLogin : LoginContentTab
	{
		private const string LOGIN_USER_NAME = "LoginScreenUserName";
		private const string LOGIN_COUNTRY_NAME_STR = "LoginScreenCountryName";
		private const string LOGIN_COUNTRY_INDEX_STR = "LoginScreenCountryIndex";
		private const string LOGIN_EXPERTISE_INDEX_STR = "LoginScreenExpertiseIndex";

		[SerializeField] private CustomInputField m_usernameField;
		[SerializeField] private CustomInputField m_passwordField;
		[SerializeField] private CustomDropdown m_countryDropdown;
		[SerializeField] private CustomDropdown m_expertiseDropdown;
		[SerializeField] private Button m_cancelButton;
		[SerializeField] private Button m_acceptButton;
		[SerializeField] private GameObject m_passwordContainer;
		[SerializeField] private GameObject m_expertiseContainer;

		private int[] m_countryDropdownIndexToID;

		protected override void Initialize()
		{
			base.Initialize();

			m_usernameField.text = PlayerPrefs.GetString(LOGIN_USER_NAME, "");

			m_cancelButton.onClick.AddListener(OnCancelClick);
			m_acceptButton.onClick.AddListener(OnAcceptClick);
			m_countryDropdown.onValueChanged.AddListener(OnCountryChanged);
		}

		public void SetToSession(GameSession a_session)
		{
			//Populate the dropdown list with countries as soon as the team importer is finished
			List<TMP_Dropdown.OptionData> dropdownOptionList = new List<TMP_Dropdown.OptionData>(LoginManager.Instance.m_teamImporter.teams.Count);
			m_countryDropdownIndexToID = new int[LoginManager.Instance.m_teamImporter.teams.Count];
			int index = 0;
			foreach (KeyValuePair<int, Team> team in LoginManager.Instance.m_teamImporter.teams)
			{
				dropdownOptionList.Add(new TMP_Dropdown.OptionData(team.Value.name));
				m_countryDropdownIndexToID[index] = team.Key;
				index++;
			}
			m_countryDropdown.ClearOptions();
			m_countryDropdown.AddOptions(dropdownOptionList);

			//Load countryDropdown index from playerprefs
			string countryName = PlayerPrefs.GetString(LOGIN_COUNTRY_NAME_STR, "");
			bool indexSet = false;
			if (countryName != "")
			{
				int countryIndex = PlayerPrefs.GetInt(LOGIN_COUNTRY_INDEX_STR, -1);
				if (countryIndex != -1)
				{
					if (countryIndex < m_countryDropdown.options.Count)
					{
						if (m_countryDropdown.options[countryIndex].text == countryName)
						{
							m_countryDropdown.value = countryIndex;
							indexSet = true;
						}
					}
				}
			}
			if (!indexSet)
				m_countryDropdown.value = 0;

			//Load expertise definitions and populate the dropdown
			if (LoginManager.Instance.m_teamImporter.MspGlobalData.expertise_definitions == null || LoginManager.Instance.m_teamImporter.MspGlobalData.expertise_definitions.Length == 0)
			{
				m_expertiseContainer.gameObject.SetActive(false);
				PlayerPrefs.SetInt(LOGIN_EXPERTISE_INDEX_STR, -1);
			}
			else
			{
				m_expertiseContainer.gameObject.SetActive(true);
				dropdownOptionList = new List<TMP_Dropdown.OptionData>(LoginManager.Instance.m_teamImporter.MspGlobalData.expertise_definitions.Length);
				foreach (var expertise in LoginManager.Instance.m_teamImporter.MspGlobalData.expertise_definitions)
				{
					dropdownOptionList.Add(new TMP_Dropdown.OptionData(expertise.name));
				}

				m_expertiseDropdown.ClearOptions();
				m_expertiseDropdown.AddOptions(dropdownOptionList);

				int expertiseIndex = PlayerPrefs.GetInt(LOGIN_EXPERTISE_INDEX_STR, -1);
				if (expertiseIndex >= 0 && expertiseIndex < dropdownOptionList.Count)
					m_expertiseDropdown.value = expertiseIndex;
				else
					expertiseIndex = 0;
				m_expertiseDropdown.value = expertiseIndex;
			}

			OnCountryChanged(0);
		}

		void OnCountryChanged(int a_newIndex)
		{
			bool isAdmin = TeamManager.IsGameMaster(m_countryDropdownIndexToID[m_countryDropdown.value]);
			if (isAdmin)
			{
				m_passwordContainer.SetActive(LoginManager.Instance.m_teamImporter.MspGlobalData.user_admin_has_password);
			}
			else
			{
				m_passwordContainer.SetActive(LoginManager.Instance.m_teamImporter.MspGlobalData.user_common_has_password);
			}
		}

		void OnCancelClick()
		{
			LoginManager.Instance.SetTabActive(LoginManager.ELoginMenuTab.Sessions);
		}

		public void OnAcceptClick()
		{
			if (m_usernameField.text == "")
			{
				DialogBoxManager.instance.NotificationWindow("No username set", "Please fill in a username and try again", null, "Continue");
				return;
			}
			else if (m_passwordContainer.activeInHierarchy && string.IsNullOrEmpty(m_passwordField.text))
			{
				DialogBoxManager.instance.NotificationWindow("Incorrect password", "The provided password is incorrect, please try again", null, "Continue");
				return;
			}
			else
			{
				//Store username on login attempt
				PlayerPrefs.SetString(LOGIN_USER_NAME, m_usernameField.text);
			}

			//Store selected country on loginattempt in playerprefs
			string countryName = m_countryDropdown.options[m_countryDropdown.value].text;
			PlayerPrefs.SetInt(LOGIN_COUNTRY_INDEX_STR, m_countryDropdown.value);
			PlayerPrefs.SetString(LOGIN_COUNTRY_NAME_STR, countryName);
			if (m_expertiseContainer.gameObject.activeSelf)
				PlayerPrefs.SetInt(LOGIN_EXPERTISE_INDEX_STR, m_expertiseDropdown.value);
			else
				PlayerPrefs.SetInt(LOGIN_EXPERTISE_INDEX_STR, -1);

			int countryID = m_countryDropdownIndexToID[m_countryDropdown.value];
			LoginManager.Instance.SetLoadingOverlayActive(true);
			ServerCommunication.RequestSession(
				countryID, m_usernameField.text, (response) => RequestSessionSuccess(response, countryID),
				RequestSessionFailure, m_passwordContainer.activeInHierarchy ? m_passwordField.text : null
			);
		}

		void RequestSessionSuccess(ServerCommunication.RequestSessionResponse response, int countryIndex)
		{
			ServerCommunication.SetApiAccessToken(response.api_access_token, response.api_access_recovery_token);
			TeamManager.InitializeUserValues(countryIndex, m_usernameField.text, response.session_id,
				LoginManager.Instance.m_teamImporter.teams, m_passwordContainer.activeInHierarchy ? m_passwordField.text : null);
			Main.MspGlobalData = LoginManager.Instance.m_teamImporter.MspGlobalData;

			SceneManager.LoadScene("MSP2050");
		}

		void RequestSessionFailure(ServerCommunication.ARequest request, string message)
		{
			LoginManager.Instance.SetLoadingOverlayActive(false);
			DialogBoxManager.instance.NotificationWindow("Connecting failed", message.Split('\n')[0], null, "Continue");
			Debug.LogError(message);
		}
	}
}
