using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class KPICountrySelector: MonoBehaviour
	{
		[Serializable]
		public class OnTeamSelectionChangedDelegate : UnityEvent<int>
		{ }

		[SerializeField] GameObject m_countryButtonPrefab = null;
		[SerializeField] RectTransform m_countryButtonParent = null;
		[SerializeField] ToggleGroup m_toggleGroup;
		[SerializeField] bool m_addAllButton = false;

		public OnTeamSelectionChangedDelegate onTeamSelectionChanged = null;

		//private Dictionary<int, KPICountrySelection> m_buttonsByTeam = new Dictionary<int, KPICountrySelection>(16);
		private HashSet<int> currentSelectedTeam = new HashSet<int>();

		private void Start()
		{
			InitializeButtons();
		}

		private void InitializeButtons()
		{
			Team currentTeam = SessionManager.Instance.CurrentTeam;
			m_toggleGroup.allowSwitchOff = true;

			foreach (Team team in SessionManager.Instance.GetTeams())
			{
				if (team.IsManager)
					continue;
				KPICountrySelection button = Instantiate(m_countryButtonPrefab, m_countryButtonParent).GetComponent<KPICountrySelection>();
				button.SetTeamColor(team.color, m_toggleGroup);
				int teamId = team.ID;
				button.SetToggleChangeHandler((b) => OnToggleChanged(b, teamId));

				if (teamId == currentTeam.ID || (currentTeam.IsManager && currentSelectedTeam.Count == 0))
				{
					button.SetSelected(true);
				}
			}
			if (m_addAllButton)
			{
				KPICountrySelection button = Instantiate(m_countryButtonPrefab, m_countryButtonParent).GetComponent<KPICountrySelection>();
				button.SetTeamColor(Color.white, m_toggleGroup);
				button.SetToggleChangeHandler((b) => OnToggleChanged(b, 0));
			}
			m_toggleGroup.allowSwitchOff = false;
		}

		private void OnToggleChanged(bool a_selected, int a_teamId)
		{
			if (a_selected)
			{
				currentSelectedTeam.Add(a_teamId);
				if (onTeamSelectionChanged != null)
				{
					onTeamSelectionChanged.Invoke(a_teamId);
				}
			}
			else
			{
				currentSelectedTeam.Remove(a_teamId);
			}
		}

		public bool IsEnabled(int teamId)
		{
			return currentSelectedTeam.Contains(teamId) || (m_addAllButton && currentSelectedTeam.Contains(0));
		}
	}
}
