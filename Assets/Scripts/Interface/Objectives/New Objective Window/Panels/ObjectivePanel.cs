using UnityEngine;
using UnityEngine.UI;

public class ObjectivePanel : MonoBehaviour
{
	public delegate void OnValidateObjective(bool titleValid, bool descriptionValid, bool eraValid, bool countryValid);

	[SerializeField]
	private CustomInputField title = null;
	[SerializeField]
    private CustomInputField description = null;
	[SerializeField]
    private EraDropdown deadline = null;
	[SerializeField]
    private CountryDropdown countryDropdown = null;
	[SerializeField]
    private GameObject allCountries = null;

	private OnValidateObjective onValidateCallback = null;

	public string Title
	{
		get
		{
			return title.text;
		}
	}

	public string Description
	{
		get
		{
			return description.text;
		}
	}

	public int TargetCountry
	{
		get
		{
			Team currentTeam = TeamManager.CurrentTeam;
			if (currentTeam.IsManager)
			{
				return countryDropdown.GetSelectedCountryId();
			}

			return currentTeam.ID;
		}
	}

	public int DeadlineYear
	{
		get
		{
			return deadline.GetSelectedMonth();
		}
	}

	private void Start()
    {
        allCountries.SetActive(TeamManager.AreWeManager);

		title.onValueChanged.AddListener(OnValueChanged);
		description.onValueChanged.AddListener(OnValueChanged);
	}

	private void OnValueChanged(string arg0)
	{
		if (onValidateCallback != null)
		{
			bool validTitle = title.text != "";
			bool validDescription = description.text != "";
			bool validEra = true;
			bool validCountry = true;
			onValidateCallback(validTitle, validDescription, validEra, validCountry);
		}
	}

	private void OnEnable()
    {
        title.text = "";
        description.text = "";
        deadline.Reset();
        countryDropdown.Reset();
    }

	public void SetValidateAction(OnValidateObjective onValidateObjective)
	{
		onValidateCallback = onValidateObjective;
	}

	public void SetFromObjectiveDetails(ObjectiveDetails objectiveDetails)
	{
		title.text = objectiveDetails.title;
		description.text = objectiveDetails.description;
		deadline.SetSelectedMonth(objectiveDetails.deadlineMonth);
		countryDropdown.SetSelectedCountryId(objectiveDetails.appliesToCountry);
	}
}