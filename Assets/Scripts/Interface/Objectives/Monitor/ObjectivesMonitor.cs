using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class ObjectivesMonitor : MonoBehaviour
{
	[Flags]
	private enum ECompletionStateFilter
	{
		Completed = (1 << 0),
		InProgress = (1 << 1),
		AnyState = Completed | InProgress
	}

	public GenericWindow thisGenericWindow;
	public GameObject raycastBlocker;

	[Header("Rect")]
	public RectTransform thisRect;

	[Header("Objective Prefab")]
	public MonitorObjective objectivePrefab;
	public Transform objectiveLocation;
	private List<MonitorObjective> objectives = new List<MonitorObjective>(16);

	[Header("Filters")]
	[SerializeField]
	private KPICountrySelector filterCountrySelector = null;
    [SerializeField]
    private CustomDropdown filterStateDropdown = null;
	[SerializeField]
	private EraDropdown filterEraDeadline = null;

	private ECompletionStateFilter completionFilterState = ECompletionStateFilter.AnyState;

	public MenuBarToggle objectivesToggle;
	public ToolbarCounter objectivesToggleCounter;

	private void Start()
	{
		SetWindowActive(false); //Hide the window immediately. We can't set the state to disabled in the editor since we need all the constructors to run (including Awake & Start)

		filterCountrySelector.onTeamSelectionChanged.AddListener(OnCountryFilterChanged);
		filterEraDeadline.OnValueChanged.AddListener(OnDeadlineFilterChanged);
        filterStateDropdown.onValueChanged.AddListener(OnCompletionStateFilterChanged);
        filterStateDropdown.ClearOptions();
        filterStateDropdown.options.Add(new TMP_Dropdown.OptionData("In progress"));
        filterStateDropdown.options.Add(new TMP_Dropdown.OptionData("Completed"));
        filterStateDropdown.options.Add(new TMP_Dropdown.OptionData("Any state"));
        filterStateDropdown.value = 2;

    }

    public void SetWindowActive(bool activeState)
	{
		gameObject.SetActive(activeState);
	}

	private void OnEnable()
	{
		thisGenericWindow.CenterWindow();
        StartCoroutine(thisGenericWindow.LimitPositionEndFrame());
    }

    private void OnDisable()
	{
		if (objectivesToggle.toggle.isOn)
		{
			objectivesToggle.toggle.isOn = false;
		}
	}

	public void UpdateObjectivesFromServer(IEnumerable<ObjectiveObject> aList)
	{
		foreach (ObjectiveObject objectiveObject in aList)
		{
			MonitorObjective existingObjective = FindObjectiveById(objectiveObject.objective_id);
			if (!objectiveObject.active)
			{
				if (existingObjective != null)
				{
					RemoveObjectiveFromUI(existingObjective);
				}
				continue;
			}

			ObjectiveDetails objectiveDetails = new ObjectiveDetails(objectiveObject);
			if (existingObjective != null)
			{
				existingObjective.SetObjectiveDetails(objectiveDetails);
			}
			else
			{
				CreateObjective(objectiveDetails);
			}

			if (!gameObject.activeSelf)
			{
				objectivesToggleCounter?.AddValue();
			}
		}

		FilterObjectives();
	}

	private MonitorObjective CreateObjective(ObjectiveDetails details)
	{
		MonitorObjective obj = Instantiate(objectivePrefab, objectiveLocation, false);
		objectives.Add(obj);

		// Track objective if 
		obj.SetOwner(this);
		obj.SetObjectiveDetails(details);
		
		SortObjectives();

		return obj;
	}

	private MonitorObjective FindObjectiveById(int objectiveId)
	{
		return objectives.Find(obj => obj.ObjectiveDetails.objectiveId == objectiveId);
	}

	public void RemoveObjectiveFromUI(MonitorObjective obj)
	{
		objectives.Remove(obj);
		Destroy(obj.gameObject);
	}

	private void OnCountryFilterChanged(int selectedCountryId)
	{
		FilterObjectives();
	}

	private void OnCompletionStateFilterChanged(int newState)
	{
		switch (newState)
		{
		case 0:
			completionFilterState = ECompletionStateFilter.InProgress;
			break;
		case 1:
			completionFilterState = ECompletionStateFilter.Completed;
			break;
		case 2:
			completionFilterState = ECompletionStateFilter.AnyState;
			break;
		default:
			Debug.LogError("Unknown completion filter state " + completionFilterState);
			break;
		}

		//UpdateCompletionStateFilterText();
		FilterObjectives();
	}

	//private void UpdateCompletionStateFilterText()
	//{
	//	string text;
	//	switch (completionFilterState)
	//	{
	//	case ECompletionStateFilter.Completed:
	//		text = "Completed";
	//		break;
	//	case ECompletionStateFilter.InProgress:
	//		text = "In progress";
	//		break;
	//	case ECompletionStateFilter.AnyState:
	//		text = "Any state";
	//		break;
	//	default:
	//		Debug.LogError("Unknown completion state filter state " + completionFilterState);
	//		text = "???";
	//		break;
	//	}

	//	filterCycleStateButtonText.text = text;
	//}

	private void OnDeadlineFilterChanged(int selectedDeadlineIndex)
	{
		FilterObjectives();
	}

	private void FilterObjectives()
	{
		int selectedDeadlineMonth = filterEraDeadline.GetSelectedMonth();
		for (int i = 0; i < objectives.Count; i++)
		{
			if (objectives[i].TeamId != -1)
			{
				ObjectiveDetails details = objectives[i].ObjectiveDetails;
				bool isActive = filterCountrySelector.IsEnabled(objectives[i].TeamId) && 
								(IsCompletionStateFilterEnabled(ECompletionStateFilter.Completed) || !details.completed) &&
								(IsCompletionStateFilterEnabled(ECompletionStateFilter.InProgress) || details.completed) && 
								(selectedDeadlineMonth == -1 || selectedDeadlineMonth == details.deadlineMonth);
				objectives[i].gameObject.SetActive(isActive);
			}
		}
	}

	private bool IsCompletionStateFilterEnabled(ECompletionStateFilter filterState)
	{
		return (completionFilterState & filterState) == filterState;
	}

	private void SortObjectives()
	{
		int currentTeamId = TeamManager.CurrentUserTeamID;
		for (int i = 0; i < objectives.Count; i++)
		{
			if (objectives[i].TeamId != currentTeamId)
			{
				objectives[i].transform.SetAsLastSibling();
			}
		}
	}

	public void OnObjectiveUIStateChanged()
	{
		FilterObjectives();
	}
}