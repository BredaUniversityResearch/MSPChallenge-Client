using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

public class NewObjectiveWindow : MonoBehaviour
{
	private static NewObjectiveWindow singleton;

	public static NewObjectiveWindow instance
	{
		get
		{
			if (singleton == null)
				singleton = (NewObjectiveWindow)FindObjectOfType(typeof(NewObjectiveWindow));
			return singleton;
		}
	}

	[SerializeField]
	private GenericWindow thisGenericWindow = null;

	[SerializeField]
	public ObjectivePanel objectivePanel;

	// Validation
	private bool validateTitle;
	private bool validateTaskCount;
	private bool validateTaskCategory;
	private bool validateTaskValue;

	[SerializeField]
	private Button acceptObjectiveButton = null;
	[SerializeField]
	private TextMeshProUGUI errorMessageDisplay = null;

	public void Awake()
	{
		//CreateSectors();
		acceptObjectiveButton.interactable = false;
		objectivePanel.SetValidateAction(SetObjectiveValid);
        acceptObjectiveButton.onClick.AddListener(CreateNewObjective);
    }

	void OnEnable()
	{
		thisGenericWindow.CenterWindow();

	}

	public void CreateNewObjective()
	{
		// Block used for interface
		string title = objectivePanel.Title;
		string description = objectivePanel.Description;
		int deadlineMonth = objectivePanel.DeadlineYear;
		int appliesToCountry = objectivePanel.TargetCountry;

		NetworkForm form = new NetworkForm();
		form.AddField("country", appliesToCountry);
		form.AddField("title", title);
		form.AddField("description", description);
		form.AddField("deadline", deadlineMonth);
		ServerCommunication.DoRequest(Server.SendObjective(), form);
        gameObject.SetActive(false);
	}

	public void CloneObjective(ObjectiveDetails objectiveDetails)
	{
		// Clone objective info
		objectivePanel.SetFromObjectiveDetails(objectiveDetails);
	}

	private void SetObjectiveValid(bool titleValid, bool descriptionValid, bool eraValid, bool countryValid)
	{
		if (!titleValid)
		{
			errorMessageDisplay.text = "The objective is missing a title";
		}
		else if (!descriptionValid)
		{
			errorMessageDisplay.text = "The objective is missing a description";
		}
		else if (!eraValid)
		{
			errorMessageDisplay.text = "Please select a valid era deadline";
		}
		else if (!countryValid)
		{
			errorMessageDisplay.text = "The selected country is invalid";
		}

		acceptObjectiveButton.interactable = titleValid && descriptionValid && eraValid && countryValid;
		errorMessageDisplay.gameObject.SetActive(!acceptObjectiveButton.interactable);
	}
}