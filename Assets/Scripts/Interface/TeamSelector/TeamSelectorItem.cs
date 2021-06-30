using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TeamSelectorItem : MonoBehaviour {

    //Should be set in the inspector
    public TeamSelector teamSelector;
	public Toggle toggle;
    public Text text;
    public Image teamColorImage;

    private int index = -1;

    private void Start()
    {
        //Get SetValues called by the teamselector
        teamSelector.LayerTypeSelectorItemCreated(this); 
    }

    public void SetValues(int index, string text, Color color)
    {
        this.index = index;
        this.text.text = text;
        teamColorImage.color = color;
    }

	public void SetValues()
	{
		this.text.text = "Multiple";
		teamColorImage.color = Color.white;
		toggle.interactable = false;
	}

    public void OnTogglePressed()
    {
		//This callback cant be done from code because then the dropdown is closed before this function is called. 
		//If set in the inspector this function is called first.

		if (index != -1)
		{
			teamSelector.SetSelectedTeamIndex(index);
		}
    }
}
