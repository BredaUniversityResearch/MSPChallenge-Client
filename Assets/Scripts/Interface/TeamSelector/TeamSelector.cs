using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TeamSelector : MonoBehaviour
{
    public delegate void TeamChangeCallback(int newTeamID);

    public TeamChangeCallback teamChangeCallback;

    //Should be set in the inspector, not initialised on code
    public Dropdown dropdown;
    public Text selectedLabel;
    public Image selectedCountryColor, dropdownArrow;

    private Dictionary<int, int> teamIndexToID;
	private int multipleIndex;
    private int currentItemIndex;   //For keeping index of next item to be created
    private int selectedIndex;      //Currently selected team index
    private bool gmSelectable;      //is the GM an option in the dropdown

    private void Start()
    {
        dropdown.onValueChanged.AddListener(OnDropDownValueChanged);
        LayerImporter.OnDoneImporting += () =>
        {
            if (!TeamManager.IsGameMaster)
                gameObject.SetActive(false);
        };
    }

    private void RecreateDropdownOptions()
    {
        currentItemIndex = 0;
        //Create empty dropdown options, as the text is set after instantiation.
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        for (int i = 0; i < teamIndexToID.Count - 1; i++)
            options.Add(new Dropdown.OptionData("")); //Multiple option also added
        if (GMSelectable)
            options.Add(new Dropdown.OptionData(""));
        dropdown.options = options;
    }

    private void OnDropDownValueChanged(int newDropDownValue)
    {
        if (teamChangeCallback != null && newDropDownValue >= 0 && newDropDownValue != multipleIndex)
            teamChangeCallback(teamIndexToID[newDropDownValue]);
    }

    /// <summary>
    /// Initializes the dropdown for the teams that were loaded. Should be called after teams are loaded.
    /// </summary>
    public void FillTeamDropdown()
    {
        teamIndexToID = new Dictionary<int, int>();
        int index = 0;
        foreach (Team team in TeamManager.GetTeams())
        {
            if (team.ID != TeamManager.GM_ID)
            {
                teamIndexToID.Add(index, team.ID);
                index++;
            }
        }
		//Multiple option
		multipleIndex = index;
		teamIndexToID.Add(index, -1);
		//GM
		teamIndexToID.Add(index + 1, TeamManager.GM_ID);
        RecreateDropdownOptions();
    }

    /// <summary>
    /// Called by items after they have been created. Gets their index, color and text.
    /// </summary>
    public void LayerTypeSelectorItemCreated(TeamSelectorItem item)
    {
		if (teamIndexToID[currentItemIndex] == -1)
		{
			item.SetValues();
		}
		else
		{
			Team nextTeam = TeamManager.GetTeamByTeamID(teamIndexToID[currentItemIndex]);
			item.SetValues(currentItemIndex, nextTeam.name, nextTeam.color);
		}
        currentItemIndex++;
        if (currentItemIndex == (gmSelectable ? teamIndexToID.Count : teamIndexToID.Count - 1))
            currentItemIndex = 0;
    }

    //Get/Set selected team by team ID 
    public int SelectedTeam
    {
        get {
            if(selectedIndex >= 0)
                return teamIndexToID[selectedIndex];
            return 3;
        }
        set
        {
            if (value < 0)
                SetSelectedTeamIndex(value);
            else
                foreach (KeyValuePair<int, int> kvp in teamIndexToID)
                    if (kvp.Value == value)
                        SetSelectedTeamIndex(kvp.Key);
        }
    }

    //Sets the selected index by ID. Called when one of the items is clicked.
    public void SetSelectedTeamIndex(int index)
    {
        if (index < -1)
        {
            //None selected
            selectedIndex = -2;
            selectedLabel.text = "No Geometry";
            selectedCountryColor.color = Color.clear;
            dropdownArrow.color = Color.white;
        }
        else if (index == -1)
        {
            //Multiple selected
            selectedIndex = -1;
			dropdown.value = multipleIndex;
            selectedLabel.text = "Multiple";
			selectedCountryColor.color = Color.white;
            dropdownArrow.color = Color.white;
        }
        else
        {
            selectedIndex = index;
			dropdown.value = index;
            Team team = TeamManager.GetTeamByTeamID(teamIndexToID[selectedIndex]);
			selectedLabel.text = team.name;
            selectedCountryColor.color = team.color;
            dropdownArrow.color = team.color;
        }
    }

    //Is the GM team an option in the dropdown
    public bool GMSelectable
    {
        get { return gmSelectable; }
        set
        {
            if (value != gmSelectable)
            {
                gmSelectable = value;
                RecreateDropdownOptions();
            }
        }
    }

    public void SetTeamToBasicIfEmpty()
    {
        if (selectedIndex < 0)
            SetSelectedTeamIndex(0);
    }

    public void SetDropdownInteractivity(bool value)
    {
        if (dropdown.interactable != value)
        {
            dropdown.interactable = value;
            if (!value)
                SetSelectedTeamIndex(-2);
        }
    }
}
