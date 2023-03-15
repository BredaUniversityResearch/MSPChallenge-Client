using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
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

		[Header("Rect")]
		public RectTransform thisRect;

		[Header("Objectives")]
		public MonitorObjective objectivePrefab;
		public Transform objectiveLocation;
		[SerializeField] NewObjectiveWindow newObjectivesWindow;
		[SerializeField] Button newObjectivesButton;

		private List<MonitorObjective> objectives = new List<MonitorObjective>(16);

		[Header("Filters")]
		[SerializeField] KPICountrySelector filterCountrySelector = null;
		[SerializeField] CustomDropdown filterStateDropdown = null;
		[SerializeField]  EraDropdown filterEraDeadline = null;

		private ECompletionStateFilter completionFilterState = ECompletionStateFilter.AnyState;

		private void Start()
		{
			newObjectivesButton.onClick.AddListener(newObjectivesWindow.OpenToNewObjective);
			gameObject.SetActive(false); //Hide the window immediately. We can't set the state to disabled in the editor since we need all the constructors to run (including Awake & Start)

			filterCountrySelector.onTeamSelectionChanged.AddListener(OnCountryFilterChanged);
			filterEraDeadline.OnValueChanged.AddListener(OnDeadlineFilterChanged);
			filterStateDropdown.onValueChanged.AddListener(OnCompletionStateFilterChanged);
			filterStateDropdown.ClearOptions();
			filterStateDropdown.options.Add(new TMP_Dropdown.OptionData("In progress"));
			filterStateDropdown.options.Add(new TMP_Dropdown.OptionData("Completed"));
			filterStateDropdown.options.Add(new TMP_Dropdown.OptionData("Any state"));
			filterStateDropdown.value = 2;

		}

		private void OnEnable()
		{
			thisGenericWindow.CenterWindow();
			StartCoroutine(thisGenericWindow.LimitPositionEndFrame());
		}

		private void OnDisable()
		{
			if (InterfaceCanvas.Instance.menuBarObjectivesMonitor.toggle.isOn)
			{
				InterfaceCanvas.Instance.menuBarObjectivesMonitor.toggle.isOn = false;
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
					existingObjective.SetObjectiveDetails(objectiveDetails, this);
				}
				else
				{
					CreateObjective(objectiveDetails);
				}
			}

			FilterObjectives();
		}

		private MonitorObjective CreateObjective(ObjectiveDetails details)
		{
			MonitorObjective obj = Instantiate(objectivePrefab, objectiveLocation, false);
			objectives.Add(obj);

			// Track objective if 
			obj.SetObjectiveDetails(details, this);
		
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

			FilterObjectives();
		}

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
			int currentTeamId = SessionManager.Instance.CurrentUserTeamID;
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

		public void CopyObjective(ObjectiveDetails objective)
		{
			newObjectivesWindow.CloneObjective(objective);
		}
	}
}