using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class KPICountrySelector: MonoBehaviour
{
	[Serializable]
	public class OnTeamSelectionChangedDelegate : UnityEvent<int>
	{
	}

	[SerializeField]
	private GameObject countryButtonPrefab = null;

	[SerializeField]
	private RectTransform targetContainer = null;

	[SerializeField]
	private bool allowMultipleSelected = false;

    [SerializeField]
    private bool addAllButton = false;

    public OnTeamSelectionChangedDelegate onTeamSelectionChanged = null;

	private Dictionary<int, KPICountrySelection> buttonsByTeam = new Dictionary<int, KPICountrySelection>(16);
	private HashSet<int> currentSelectedTeam = new HashSet<int>();

	private void Start()
	{
		if (TeamManager.TeamCount > 0)
		{
			InitializeButtons();
		}
		else
		{
			TeamManager.OnTeamsLoadComplete += OnTeamsLoaded;
		}
	}

	private void OnTeamsLoaded()
	{
		TeamManager.OnTeamsLoadComplete -= OnTeamsLoaded;
		InitializeButtons();
	}

	private void InitializeButtons()
	{
		Team currentTeam = TeamManager.CurrentTeam;

		foreach (Team team in TeamManager.GetTeams())
		{
			if (team.IsManager)
				continue;
			KPICountrySelection button = Instantiate(countryButtonPrefab, targetContainer).GetComponent<KPICountrySelection>();
			button.SetTeamColor(team.color);
			int teamId = team.ID;
			button.SetOnClickHandler(() => SelectTeam(teamId));

			buttonsByTeam.Add(teamId, button);

			if (/*allowMultipleSelected ||*/ 
				(teamId == currentTeam.ID || (currentTeam.IsManager && currentSelectedTeam.Count == 0)))
			{
				SelectTeam(teamId);
			}
		}
        if (addAllButton)
        {
            KPICountrySelection button = Instantiate(countryButtonPrefab, targetContainer).GetComponent<KPICountrySelection>();
            button.SetTeamColor(Color.white);
            button.SetOnClickHandler(() => SelectTeam(0));
            buttonsByTeam.Add(0, button);
        }
	}

	private void SelectTeam(int newTeamId)
	{
		if (!allowMultipleSelected)
		{
			DeselectAll();
			Select(newTeamId);
		}
		else
		{
			ToggleSelection(newTeamId);
		}

		if (onTeamSelectionChanged != null)
		{
			onTeamSelectionChanged.Invoke(newTeamId);
		}
	}

	private void Select(int newTeamId)
	{
		if (!currentSelectedTeam.Contains(newTeamId))
		{
			currentSelectedTeam.Add(newTeamId);
			buttonsByTeam[newTeamId].SetSelected(true);
		}
	}

	private void DeselectAll()
	{
		foreach (int selected in currentSelectedTeam)
		{
			buttonsByTeam[selected].SetSelected(false);
		}

		currentSelectedTeam.Clear();
	}

	private void ToggleSelection(int teamId)
	{
		if (currentSelectedTeam.Contains(teamId))
		{
			buttonsByTeam[teamId].SetSelected(false);
			currentSelectedTeam.Remove(teamId);
		}
		else
		{
			buttonsByTeam[teamId].SetSelected(true);
			currentSelectedTeam.Add(teamId);
		}
	}

	public bool IsEnabled(int teamId)
	{
		return currentSelectedTeam.Contains(teamId) || (addAllButton && currentSelectedTeam.Contains(0));
	}
}
