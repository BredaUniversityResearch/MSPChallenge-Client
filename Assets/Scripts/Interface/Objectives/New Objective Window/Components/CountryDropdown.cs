using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(CustomDropdown))]
public class CountryDropdown : MonoBehaviour
{
	[SerializeField]
    private CustomDropdown dropdown = null;
	[SerializeField]
    private Image countrySelect = null;

    public void Start()
    {
        CreateOptions();
    }

    /// <summary>
    /// This creates the country options using the team colors in ColorPalette
    /// </summary>
    private void CreateOptions()
    {
        // Country dropdown
        List<TMP_Dropdown.OptionData> countryOptions = new List<TMP_Dropdown.OptionData>(TeamManager.TeamCount);
        foreach (KeyValuePair<int, Team> team in TeamManager.GetTeamsByID())
		{
			if (!team.Value.IsManager)
			{
				countryOptions.Add(new TMP_Dropdown.OptionData(team.Value.name));
			}
        }

        countryOptions.Add(new TMP_Dropdown.OptionData("All Teams"));

        dropdown.AddOptions(countryOptions);

        dropdown.onValueChanged.AddListener(SelectCountry);

        if(dropdown.options.Count != 0)
		{
			Team team = TeamManager.FindTeamByName(dropdown.options[dropdown.value].text);
			if (team != null)
			{
				countrySelect.color = team.color;
			}
        }
    }

    /// <summary>
    /// Enables/Disables graphics when making a selection
    /// </summary>
	private void SelectCountry(int optionId)
	{
		Team selectedTeam = TeamManager.FindTeamByName(dropdown.options[optionId].text);
		OnSelectedTeamChanged(selectedTeam);
	}

	public void SetSelectedCountryId(int countryId)
	{
		Team targetTeam = TeamManager.FindTeamByID(countryId);
		int index = dropdown.options.FindIndex(obj => obj.text == targetTeam.name);
		dropdown.value = index;
		OnSelectedTeamChanged(targetTeam);
	}

	private void OnSelectedTeamChanged(Team targetTeam)
	{
		if (targetTeam != null)
		{
			countrySelect.gameObject.SetActive(true);
			countrySelect.color = targetTeam.color;
		}
		else
		{
			countrySelect.gameObject.SetActive(false);
		}
	}

	public int GetSelectedCountryId()
	{
		int countryId = -1; //Assume all countries
		if (dropdown.options.Count > 0)
		{
			Team selectedTeam = TeamManager.FindTeamByName(dropdown.options[dropdown.value].text);
			if (selectedTeam != null)
			{
				countryId = selectedTeam.ID;
			}
		}
		else
		{
			Debug.LogError("Tried querying the selected country from CountryDropdown, but no entries were found in the dropdown");
		}

		return countryId;
	}

	public void Reset()
	{
		dropdown.value = 0;
	}
}