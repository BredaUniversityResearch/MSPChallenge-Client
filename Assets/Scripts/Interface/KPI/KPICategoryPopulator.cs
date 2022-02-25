using System;
using System.Collections.Generic;
using KPI;
using UnityEngine;
using UnityEngine.Events;

public class KPICategoryPopulator: MonoBehaviour
{
	[Serializable]
	private class KPIValueToggledEvent : UnityEvent<KPIValue, bool>
	{
	}

	[Serializable]
	private class KPIColorChangedEvent : UnityEvent<KPIValue, Color>
	{
	}

	[SerializeField]
	private RectTransform targetContainer = null;

	[SerializeField, Tooltip("OPTIONAL: Animations that might be linked to the KPI category. Sets the \"Expanded\" flag when one or more categories are open")]
	private Animator animator = null;

	[SerializeField]
	private EKPICategory targetCategory = EKPICategory.Ecology;

	[SerializeField]
	private GameObject categoryPrefab = null;
	[SerializeField]
	private GameObject valuePrefab = null;

    [SerializeField] private bool showGraphToggle = true;

	[SerializeField]
	private KPIValueToggledEvent onKPIValueToggled = null;

	[SerializeField]
	private KPIColorChangedEvent onKPIColorChanged = null;

	[SerializeField]
	private KPIValueProceduralColorScheme colorScheme = null;

	//[SerializeField] private bool initialiseAfterLayerLoad;

	private Dictionary<string, KPIBar> kpiBarsByValueName = new Dictionary<string, KPIBar>(16); //Also includes the categories
    private Dictionary<string, KPIBar> kpiCategoriesByCategoryName = new Dictionary<string, KPIBar>(8);
	private Stack<KPIBar> inactiveKpiBarsPool = new Stack<KPIBar>(16);
	private KPIValueCollection targetCollection = null;
	private int targetTeamId = -1;

	private int displayMonth = -1;
	private bool automaticallyFollowLatestMonth = true;

	private void Awake()
	{
		Main.OnPostFinishedLoadingLayers += OnGameFinishedLoading;	
	}

    private void OnGameFinishedLoading()
    {
        Main.OnPostFinishedLoadingLayers -= OnGameFinishedLoading;
        Initialise();
    }

    private void Initialise()
    {
        if (targetTeamId == -1)
        {
            if (TeamManager.CurrentTeam.IsManager)
            {
                //Setup target country for first team.
                foreach (Team team in TeamManager.GetTeams())
                {
                    targetTeamId = team.ID;
                    break;
                }
            }
            else
            {
                targetTeamId = TeamManager.CurrentTeam.ID;
            }
        }

        KPIValueCollection collection = KPIManager.GetKPIValuesForCategory(targetCategory, targetTeamId);
        if (collection != null)
        {
            CreateValuesForCollection(collection);
            SetTargetCollection(collection);
        }
    }

    private void OnDestroy()
	{
		SetTargetCollection(null);
	}

	private void DestroyAllValueBars()
	{
		foreach (KPIBar bar in kpiBarsByValueName.Values)
		{
			Destroy(bar.gameObject);
		}

		kpiBarsByValueName.Clear();
	}

	//Callback to use with UnityEvents.
	public void DisplayValuesForMonth(int month)
	{
		displayMonth = month;
		automaticallyFollowLatestMonth = false;
		if (targetCollection != null)
		{
			UpdateDisplayValues(targetCollection, displayMonth);
		}
	}

	//Callback to use with UnityEvents
	public void DisplayValuesForTeam(int teamId)
	{
		if (targetTeamId != teamId)
		{
			List<string> toggledValueNames = null;
			if (targetCollection != null)
			{
				toggledValueNames = ClearActiveToggledValues();
			}
	
			targetTeamId = teamId;
			SetTargetCollection(KPIManager.GetKPIValuesForCategory(targetCategory, targetTeamId));

			if (targetCollection != null)
			{
				if (toggledValueNames != null)
				{
					ToggleValuesOn(toggledValueNames);
				}
			}
		}
	}

	private void SetTargetCollection(KPIValueCollection newTarget)
	{
		if (targetCollection != null)
		{
			targetCollection.OnKPIValuesUpdated -= OnKPIValuesUpdated;
			targetCollection.OnKPIValueDefinitionsChanged -= OnKPIValueDefinitionsChanged;
		}

		targetCollection = newTarget;

		if (targetCollection != null)
		{
			UpdateDisplayValues(targetCollection, displayMonth);
			targetCollection.OnKPIValuesUpdated += OnKPIValuesUpdated;
			targetCollection.OnKPIValueDefinitionsChanged += OnKPIValueDefinitionsChanged;
		}
	}

	private void OnKPIValueDefinitionsChanged(KPIValueCollection sourceCollection)
	{
		DestroyAllValueBars();
		CreateValuesForCollection(sourceCollection);
	}

	private void OnKPIValuesUpdated(KPIValueCollection sourceCollection, int previousMostRecentMonthReceived, int mostRecentMonthReceived)
	{
		if (automaticallyFollowLatestMonth)
		{
			displayMonth = mostRecentMonthReceived;
		}

		if (displayMonth == mostRecentMonthReceived)
		{
			UpdateDisplayValues(sourceCollection, mostRecentMonthReceived);
		}
	}

	private void UpdateDisplayValues(KPIValueCollection valueCollection, int month)
	{
		if (kpiBarsByValueName.Count == 0)
		{
			return; //Prevent the game from logging warnings before the category has been initialized, as would happen with Energy on load.
		}

		HashSet<string> valuesToRemove = new HashSet<string>(kpiBarsByValueName.Keys);
		foreach (KPIValue value in valueCollection.GetValues())
		{
			valuesToRemove.Remove(value.name);

			KPIBar bar;
			if (!kpiBarsByValueName.TryGetValue(value.name, out bar))
			{
				if (kpiCategoriesByCategoryName.TryGetValue(value.owningCategoryName, out KPIBar categoryBar))
				{
					bar = CreateKPIBar(valuePrefab, categoryBar.childContainer.transform, value);
				}
			}

			if (bar != null)
			{
				bar.SetStartValue((float)value.GetKpiValueForMonth(0));
				bar.SetActual((float)value.GetKpiValueForMonth(month), value.targetCountryId == KPIValue.CountryGlobal? 0 : value.targetCountryId);
			}
			else
			{
				Debug.LogWarning("Tried updating KPI Value " + value.name + " but a KPI bar could not be created for the value?");
			}
		}

		foreach (string valueToRemove in valuesToRemove)
		{
			if (kpiBarsByValueName.TryGetValue(valueToRemove, out KPIBar bar))
			{
				bar.gameObject.SetActive(false);
				bar.graphToggle.onValueChanged.RemoveAllListeners();
				inactiveKpiBarsPool.Push(bar);
				kpiBarsByValueName.Remove(valueToRemove);
			}
			else
			{
				Debug.LogError("Could not find value bar for a value that we are supposed to remove...");
			}
		}
	}

	private void CreateValuesForCollection(KPIValueCollection collection)
	{
		if (collection != null)
		{
			foreach (KPICategory category in collection.GetCategories())
			{
				KPIBar categoryBar = CreateKPIBar(categoryPrefab, GetTargetContainerForCategory(category), category);

				KPICategory categoryLocal = category;
				categoryBar.SetBarExpandedStateChangedCallback((expandedState) => OnKPICategoryToggled(categoryLocal, categoryBar));

				kpiCategoriesByCategoryName.Add(category.name, categoryBar);

				foreach (KPIValue value in category.GetChildValues())
				{
					CreateKPIBar(valuePrefab, categoryBar.childContainer.transform, value);
				}
			}
		}
	}

	protected virtual RectTransform GetTargetContainerForCategory(KPICategory category)
	{
		return targetContainer;
	}

	private void OnKPICategoryToggled(KPICategory category, KPIBar categoryBar)
	{
		foreach (KPIValue childValue in category.GetChildValues())
		{
			KPIBar childBar;
			if (kpiBarsByValueName.TryGetValue(childValue.name, out childBar))
			{
				childBar.graphToggle.isOn = false;
			}
		}

		CheckAnimationExpandedState();
	}

	private KPIBar CreateKPIBar(GameObject prefab, Transform parent, KPIValue value)
	{
		KPIBar kpiBar = null;
		if (inactiveKpiBarsPool.Count == 0)
		{
			GameObject categoryObject = Instantiate(prefab, parent);
			kpiBar = categoryObject.GetComponent<KPIBar>();
		}
		else
		{
			kpiBar = inactiveKpiBarsPool.Pop();
			kpiBar.transform.SetParent(parent, false);
			kpiBar.transform.SetAsLastSibling();
			kpiBar.gameObject.SetActive(true);
		}

		kpiBar.ValueName = value.name;
		kpiBar.title.text = value.displayName;
		kpiBar.unit = value.unit;
		if (showGraphToggle)
        {
            kpiBar.graphToggle.onValueChanged.AddListener((isOn) => { ToggleGraph(isOn, kpiBar); });
        }
        else
        {
            kpiBar.graphToggle.gameObject.SetActive(false);
        }
		
		kpiBarsByValueName.Add(value.name, kpiBar);

		return kpiBar;
	}

	private void ToggleGraph(bool isOnState, KPIBar targetBar)
	{
		KPIValue targetValue = targetCollection.FindValueByName(targetBar.ValueName);

		if (!isOnState)
		{
			Color graphColor = Color.white;
			targetBar.SetDisplayedGraphColor(graphColor);
		}

		onKPIValueToggled.Invoke(targetValue, isOnState);

		KPIValueProceduralColorScheme.Context context = new KPIValueProceduralColorScheme.Context();
		foreach (string toggledKPIValue in GetActiveToggledValues())
		{
			KPIValue value = targetCollection.FindValueByName(toggledKPIValue);
			Color newColor = GetCurrentKPIColor(value, context);
			if (kpiBarsByValueName.TryGetValue(value.name, out KPIBar kpiBar))
			{
				kpiBar.SetDisplayedGraphColor(newColor);
			}

			onKPIColorChanged.Invoke(value, newColor);
		}
	}

	private Color GetCurrentKPIColor(KPIValue targetValue, KPIValueProceduralColorScheme.Context context)
	{
		Color graphColor;
		KPICategory category = targetCollection.FindCategoryByName(targetValue.owningCategoryName);
		if (category != null && category.kpiValueColorScheme == EKPIValueColorScheme.ProceduralColor)
		{
			graphColor = colorScheme.GetColor(context);
		}
		else
		{
			graphColor = targetValue.graphColor;
		}

		return graphColor;
	}

	public void ResetContent()
	{
		foreach (KPIBar bar in kpiBarsByValueName.Values)
		{
			if (bar.isParent)
			{
				bar.SetExpandedState(false);
			}
		}
	}

	private List<string> GetActiveToggledValues()
	{
		List<string> result = new List<string>(16);
		foreach (KPIBar value in kpiBarsByValueName.Values)
		{
			if (value.graphToggle.isOn)
			{
				result.Add(value.ValueName);
			}
		}

		return result;
	}

	private List<string> ClearActiveToggledValues()
	{
		List<string> result = new List<string>(16);
		foreach (KPIBar value in kpiBarsByValueName.Values)
		{
			if (value.graphToggle.isOn)
			{
				value.graphToggle.isOn = false;
				result.Add(value.ValueName);
			}
		}

		return result;
	}

	private void ToggleValuesOn(List<string> valueNamesToToggle)
	{
		foreach (string valueName in valueNamesToToggle)
		{
			KPIBar bar;
			if (kpiBarsByValueName.TryGetValue(valueName, out bar))
			{
				bar.graphToggle.isOn = true;
			}
		}
	}

	private void CheckAnimationExpandedState()
	{
		if (animator != null)
		{
			foreach (KPIBar bar in kpiBarsByValueName.Values)
			{
				if (bar.isParent)
				{
					if (bar.isExpanded)
					{
						animator.SetBool("Expand", true);
						break;
					}
					else
					{
						animator.SetBool("Expand", false);
					}
				}
			}
		}
	}
}
   
