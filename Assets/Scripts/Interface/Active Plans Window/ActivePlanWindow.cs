using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActivePlanWindow : MonoBehaviour
{
	public delegate void EntityTypeChangeCallback(List<EntityType> newTypes);
	public delegate void TeamChangeCallback(int newTeamID);
	public delegate void ParameterChangeCallback(EntityPropertyMetaData parameter, string value);

	[Header("General")]
	public GenericWindow window;

	[Header("Start Editing")]
	public Button startEditingButton;

	[Header("Plan name")]
	public Image countryBall;
	public TextMeshProUGUI planNameAndDate;
    public Button zoomToPlanButton;

	[Header("View mode")]
	public GameObject viewModeSection;
	public Toggle viewAllToggle;
	public Toggle viewPlanToggle;
	public Toggle viewBaseToggle;

	[Header("Layers")]
	public GameObject layerSection;
	public Transform layerParent;
	public Object layerPrefab;
	public ToggleGroup layerToggleGroup;

	private Dictionary<PlanLayer, ActivePlanLayer> layers;
	private bool ignoreLayerCallback;

	[Header("Layer types")]
	public GameObject layerTypeSection;
	public Transform layerTypeParent;
	public Object layerTypePrefabSingle;
	public Object layerTypePrefabMulti;
	public EntityTypeChangeCallback typeChangeCallback;
	public ToggleGroup layerTypeToggleGroup;

	private Dictionary<EntityType, ActivePlanLayerType> layerTypes;
	private ActivePlanLayerType multipleTypesEntry;
	private bool multiType;
	private bool ignoreLayerTypeCallback;//Used to ignore callbacks from above (Main.StartEditingLayer) and below (ActivePlanLayer.toggle)

	[Header("Country")]
	public GameObject countrySection;
	public Transform countryParent;
	public Object countryPrefab;
	public Object countryPrefabMultiple;
	public TeamChangeCallback countryChangeCallback;
	public ToggleGroup countryToggleGroup;

	private Dictionary<int, Toggle> countryToggles;
	private Toggle gmCountryToggle, multiCountryToggle;
	private int selectedCountry;
	private bool gmSelectable = true;
	private bool ignoreCountryToggleCallback;

	[Header("Parameters")]
	public GameObject parameterSection;
	public Transform parameterParent;
	public Object parameterPrefab;
	public ParameterChangeCallback parameterChangeCallback;

	private Dictionary<EntityPropertyMetaData, ActivePlanParameter> parameters;
	private Dictionary<EntityPropertyMetaData, string> originalParameterValues;

	//General
	private Plan selectedPlan;
	private DialogBox cancelChangesConfirmationWindow = null;

	void Awake()
	{
		startEditingButton.onClick.AddListener(() =>
		{
			if (selectedPlan != null)
			{
				PlanDetails.SelectPlan(selectedPlan);
				PlanDetails.instance.TabSelect(PlanDetails.EPlanDetailsTab.Layers);
				PlanDetails.instance.editTabContentButton.onClick.Invoke();
				//PlanManager.RequestPlanLockForEditing(selectedPlan);
			}
		});

		viewAllToggle.onValueChanged.AddListener((value) =>
		{
			if (value)
				PlanManager.SetPlanViewState(PlanManager.PlanViewState.All);
		});

		viewPlanToggle.onValueChanged.AddListener((value) =>
		{
			if (value)
				PlanManager.SetPlanViewState(PlanManager.PlanViewState.Changes);
		});

		viewBaseToggle.onValueChanged.AddListener((value) =>
		{
			if (value)
				PlanManager.SetPlanViewState(PlanManager.PlanViewState.Base);
		});

		zoomToPlanButton.onClick.AddListener(() =>
        {
            selectedPlan.ZoomToPlan();
        });

		window.OnAttemptHideWindow = OnAttemptHideWindow;
	}

	private bool OnAttemptHideWindow()
	{
		if (Main.InEditMode || Main.EditingPlanDetailsContent)
		{
			if (cancelChangesConfirmationWindow == null || !cancelChangesConfirmationWindow.isActiveAndEnabled)
			{
				UnityEngine.Events.UnityAction lb = () => { };
				UnityEngine.Events.UnityAction rb = () =>
				{
					if (Main.InEditMode)				
						PlanDetails.LayersTab.ForceCancelChanges();
					else
						PlanDetails.instance.CancelEditingContent();

					PlanManager.HideCurrentPlan();
					gameObject.SetActive(false);
				};
				cancelChangesConfirmationWindow = DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "All changes made to the plan will be lost. Are you sure you want to cancel?", lb, rb);
			}

			return false;
		}
		else
		{
			PlanManager.HideCurrentPlan();
			return true;
		}
	}

	public void OnCountriesLoaded()
	{
		countryToggles = new Dictionary<int, Toggle>();
		foreach (Team team in TeamManager.GetTeams())
			if(!team.IsAreaManager)
				CreateCountryToggle(team);

		//Create the multiple selected toggle
		CreateCountryToggle(null);
	}

	public void SetToPlan(Plan plan)
	{
		gameObject.SetActive(true);
		selectedPlan = plan;
		countryBall.color = TeamManager.FindTeamByID(plan.Country).color;
		UpdateNameAndDate();
		UpdateEditButtonActivity();
	}

	public void UpdateNameAndDate()
	{
		planNameAndDate.text = string.Format("{0} ({1})", selectedPlan.Name, Util.MonthToText(selectedPlan.StartTime, true));

	}

	public void UpdateEditButtonActivity()
	{
		startEditingButton.gameObject.SetActive(!Main.InEditMode && !Main.EditingPlanDetailsContent
			&& selectedPlan != null 
			&& selectedPlan.State == Plan.PlanState.DESIGN
			&& (TeamManager.AreWeManager || selectedPlan.Country == TeamManager.CurrentUserTeamID)
			&& selectedPlan.PlanLayers.Count > 0);
	}

	public void CloseWindow()
	{
		gameObject.SetActive(false);
	}

	public void CloseEditingUI()
	{
		viewModeSection.SetActive(true);
		UpdateEditButtonActivity();

		layerTypeSection.SetActive(false);
		layerSection.SetActive(false);
		countrySection.SetActive(false);
        parameterSection.SetActive(false);
    }

	public void OpenEditingUI(PlanLayer editingLayer)
	{
		//Disable viewing and edit sections
		if (!viewAllToggle.isOn)
			viewAllToggle.isOn = true;
		viewModeSection.SetActive(false);
		startEditingButton.gameObject.SetActive(false);

		//If not a layerless plan, enable editing UI
		if (editingLayer != null)
		{
			//Activate editing sections
			layerTypeSection.SetActive(true);
			layerSection.SetActive(true);
			countrySection.SetActive(TeamManager.AreWeManager);

			//Clear and create new layers
			ClearLayers();
			foreach (PlanLayer pl in selectedPlan.PlanLayers)
				CreateLayer(pl);

			StartEditingLayer(editingLayer);
		}
	}

	public void StartEditingLayer(PlanLayer layer)
	{
		//Handle visual selection
		if (!ignoreLayerCallback)
		{
			ignoreLayerCallback = true;
			layers[layer].toggle.isOn = true;
			ignoreLayerCallback = false;
		}

		//Clear and recreate layer types
		multiType = layer.BaseLayer.MultiTypeSelect;
		layerTypeToggleGroup.allowSwitchOff = multiType;
		ClearLayerTypes();
		foreach (KeyValuePair<int, EntityType> kvp in layer.BaseLayer.EntityTypes)
			CreateLayerType(kvp.Value, kvp.Value.availabilityDate <= layer.Plan.StartTime);
		CreateMultipleLayerType();
		SetNoEntityTypesSelected();

		//Clear and recreate parameters
		ClearParameters();
		if (layer.BaseLayer.propertyMetaData == null || layer.BaseLayer.propertyMetaData.Count == 0)
			parameterSection.SetActive(false);
		else
		{
			bool activeParamsOnLayer = false;
			foreach (EntityPropertyMetaData param in layer.BaseLayer.propertyMetaData)
				if (param.ShowInEditMode)
				{
					CreateParameter(param);
					activeParamsOnLayer = true;
				}
			if(activeParamsOnLayer)
				parameterSection.SetActive(true);
			else
				parameterSection.SetActive(false);
		}

        //Set admin country option available/unavailable
        if (TeamManager.AreWeGameMaster)
            GMSelectable = !layer.BaseLayer.IsEnergyLayer();
    }

	public void ActivePlanLayerCallback(PlanLayer planLayer)
	{
		//Ignore if we just set the planlayer to active
		if (ignoreLayerCallback)
			return;

		//Ignore callback from Main.StartEditingLayer
		ignoreLayerCallback = true;
		PlanDetails.LayersTab.StartEditingLayer(planLayer);
		ignoreLayerCallback = false;
	}

	public void SetObjectChangeInteractable(bool value)
	{
		SetEntityTypeSelectionInteractable(value);
		SetParameterInteractability(value, false);
		SetCountrySelectionInteractable(value);
	}

	#region Country Selection
	public void SetTeamToBasicIfEmpty()
	{
		foreach (KeyValuePair<int, Toggle> kvp in countryToggles)
			if (kvp.Value.isOn)
				return;
		SelectedTeam = countryToggles.GetFirstKey();
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
				gmCountryToggle.gameObject.SetActive(gmSelectable);
			}
		}
	}

	public void SetCountrySelectionInteractable(bool value)
	{
		foreach (KeyValuePair<int, Toggle> kvp in countryToggles)
			kvp.Value.interactable = value;
	}

	//Get/Set selected team by team ID 
	public int SelectedTeam
	{
		get
		{
			foreach (KeyValuePair<int, Toggle> kvp in countryToggles)
				if (kvp.Value.isOn)
					return kvp.Key;
			return countryToggles.GetFirstKey();
		}
		set
		{
			ignoreCountryToggleCallback = true;
			if (value < -1)
			{
				//Select none
				foreach (KeyValuePair<int, Toggle> kvp in countryToggles)
					if (kvp.Value.isOn)
						kvp.Value.isOn = false;
				gmCountryToggle.isOn = false;
				multiCountryToggle.gameObject.SetActive(false);
			}
			else if (value == -1)
			{
				//Multiple selected
				multiCountryToggle.gameObject.SetActive(true);
				multiCountryToggle.isOn = true;
			}
			else if (countryToggles.ContainsKey(value))
			{
				countryToggles[value].isOn = true;
			}
			ignoreCountryToggleCallback = false;
		}
	}

	private void CountryToggleClicked()
	{
		if (ignoreCountryToggleCallback)
			return;
		multiCountryToggle.gameObject.SetActive(false);
		if (countryChangeCallback != null)
			countryChangeCallback(SelectedTeam);
	}
	#endregion

	#region Layer Type Selection
	public void DeselectAllEntityTypes()
	{
		multipleTypesEntry.gameObject.SetActive(false);
		SetNoEntityTypesSelected();
	}

	public void SetEntityTypeSelectionInteractable(bool value)
	{
		foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in layerTypes)
			kvp.Value.toggle.interactable = value;
	}

	public void SetSelectedEntityTypes(List<List<EntityType>> selectedTypes)
	{
		//If null, display nothing selected
		if (selectedTypes == null || selectedTypes.Count == 0)
		{
			multipleTypesEntry.gameObject.SetActive(false);
			SetNoEntityTypesSelected();
		}
		//One geom selected, show its type
		else if (selectedTypes.Count == 1)
		{
			SetSelectedEntityTypes(selectedTypes[0]);
		}
		//Multiple geom selected, determine if we should show types
		else
		{

			bool identical = true;
			int count = selectedTypes[0].Count;
			for (int i = 1; i < selectedTypes.Count && identical; i++)
			{
				if (selectedTypes[i].Count != count)
				{
					identical = false;
					break;
				}
				for (int a = 0; a < count; a++)
				{
					//TODO: this can be greatly optimized if entity types are sorted by key, current worst case: selectedTypes.count * selectedTypes[0].count^2
					if (!selectedTypes[i].Contains(selectedTypes[0][a]))
					{
						identical = false;
						break;
					}
				}
			}

			//Check of all entity types are the same
			if (identical)
				SetSelectedEntityTypes(selectedTypes[0]);
			else
				SetMultipleEntityTypesSelected();
		}
	}

	private void SetMultipleEntityTypesSelected()
	{
		//if (multiType)
		SetNoEntityTypesSelected();
		multipleTypesEntry.gameObject.SetActive(true);
        multipleTypesEntry.toggle.isOn = true;
    }

	private void SetNoEntityTypesSelected()
	{
		ignoreLayerTypeCallback = true;
        foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in layerTypes)
			if (kvp.Value.toggle.isOn)
				kvp.Value.toggle.isOn = false;
		ignoreLayerTypeCallback = false;
	}

	private void SetSelectedEntityTypes(List<EntityType> selectedTypes)
	{
		ignoreLayerTypeCallback = true;
		multipleTypesEntry.gameObject.SetActive(false);
		if (selectedTypes == null)
			SetNoEntityTypesSelected();
		else
		{
			//If multitype they're not in the toggle group, so first disable all
			if(multiType)
				foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in layerTypes)
					if (kvp.Value.toggle.isOn)
						kvp.Value.toggle.isOn = false;

			foreach (EntityType t in selectedTypes)
				layerTypes[t].toggle.isOn = true;
		}
		ignoreLayerTypeCallback = false;
	}

	private void LayerTypeToggleClicked(bool value)
	{
		if (ignoreLayerTypeCallback)
			return;
		if (!value && !multiType)
			return;
		multipleTypesEntry.gameObject.SetActive(false);
		if (typeChangeCallback != null)
			typeChangeCallback(GetEntityTypeSelection());
	}

	public List<EntityType> GetEntityTypeSelection()
	{
		List<EntityType> result = new List<EntityType>();

		foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in layerTypes)		
			if (kvp.Value.toggle.isOn)
				result.Add(kvp.Key);

		if (result.Count == 0)
			result.Add(layerTypes.GetFirstKey());
		return result;
	}

	public void SetEntityTypeToBasicIfEmpty()
	{
		foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in layerTypes)
		{
			if (kvp.Value.toggle.isOn)
				return;
		}

		//Select first interactable layer type
		foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in layerTypes)
		{

			if(kvp.Value.toggle.interactable)
			SetSelectedEntityTypes(new List<EntityType>() { layerTypes.GetFirstKey() });
		}
	}
	#endregion

	#region Parameters
	public void OnParameterChanged(EntityPropertyMetaData parameter, string value)
    {
        //If we expect a numeric value and it isnt, put the original value back
        if (parameter.ContentValidation != LayerInfoPropertiesObject.ContentValidation.None)
        {
            if (parameter.ContentValidation == LayerInfoPropertiesObject.ContentValidation.ShippingWidth)
            {
                if (Util.ParseToFloat(value) < 0)
                {
                    parameters[parameter].SetValue(originalParameterValues[parameter]);
                    return;
                }
            }
            else
            {
                if (Util.ParseToInt(value) < 1)
                {
                    parameters[parameter].SetValue(originalParameterValues[parameter]);
                    return;
                }
            }
        }
        //Only invoke callback if value changed
        if (originalParameterValues != null && originalParameterValues[parameter] != value && parameterChangeCallback != null)
		{
			parameterChangeCallback(parameter, value);
			originalParameterValues[parameter] = value;
		}
	}

	public void SetParameterInteractability(bool value, bool reset = true)
	{
		foreach (var kvp in parameters)
			kvp.Value.SetInteractable(value, reset);
	}

	public void SetSelectedParameters(List<Dictionary<EntityPropertyMetaData, string>> selectedParams)
	{
		if (selectedParams == null || selectedParams.Count == 0)
		{
			//Deselect
			SetParameterInteractability(false);
		}
		else if (selectedParams.Count == 1)
		{
			//Show single selected
			SetParameterValues(selectedParams[0]);
		}
		else
		{
			Dictionary<EntityPropertyMetaData, bool> identical = new Dictionary<EntityPropertyMetaData, bool>();
			foreach (var kvp in selectedParams[0])
			{
				identical[kvp.Key] = true;
			}

			//Check if objects have idental values per param
			for (int i = 1; i < selectedParams.Count; i++)
			{
				bool canCutOff = true;
				foreach (var kvp in selectedParams[i])
				{
					//Already found to not be identical
					if (!identical[kvp.Key])
						continue;

					//Wasn't already false, so the check is useful
					canCutOff = false;

					//Check if param idental to the first
					if (selectedParams[0][kvp.Key] != kvp.Value)
						identical[kvp.Key] = false;
				}
				if (canCutOff)
					break;
			}

			originalParameterValues = new Dictionary<EntityPropertyMetaData, string>();
			//show a value or "multiple" per entity type
			foreach (var kvp in identical)
			{
				//If all identical, use the first. Otherwise a preset value.
				string value = kvp.Value ? selectedParams[0][kvp.Key] : "multiple";
				parameters[kvp.Key].SetValue(value);				
				originalParameterValues[kvp.Key] = value;
			}
		}
	}

	public void SetParameterValues(Dictionary<EntityPropertyMetaData, string> values)
	{
		originalParameterValues = values;
		foreach (var kvp in values)
		{
			parameters[kvp.Key].SetValue(kvp.Value);
		}
	}
	#endregion

	#region Object creation
	private void ClearLayerTypes()
	{
		for(int i = 0; i < layerTypeParent.transform.childCount; i++)
			Destroy(layerTypeParent.transform.GetChild(i).gameObject);
		layerTypes = new Dictionary<EntityType, ActivePlanLayerType>();
	}

	private void CreateLayerType(EntityType type, bool interactable)
	{
		ActivePlanLayerType obj = ((GameObject)GameObject.Instantiate(multiType ? layerTypePrefabMulti : layerTypePrefabSingle)).GetComponent<ActivePlanLayerType>();
		obj.transform.SetParent(layerTypeParent, false);
		obj.SetToType(type, window, !interactable);
		if (!multiType)
			obj.toggle.group = layerTypeToggleGroup;
		obj.toggle.onValueChanged.AddListener((value) => LayerTypeToggleClicked(value));
		obj.DisabledIfNotSelected = !interactable;
		layerTypes.Add(type, obj);
	}

	private void CreateMultipleLayerType()
	{
		ActivePlanLayerType obj = ((GameObject)GameObject.Instantiate(multiType ? layerTypePrefabMulti : layerTypePrefabSingle)).GetComponent<ActivePlanLayerType>();
		obj.transform.SetParent(layerTypeParent, false);
		obj.SetToMultiple();
		if (!multiType)
			obj.toggle.group = layerTypeToggleGroup;
		multipleTypesEntry = obj;
		obj.gameObject.SetActive(false);
	}

	private void ClearLayers()
	{
		for (int i = 0; i < layerParent.transform.childCount; i++)
			Destroy(layerParent.transform.GetChild(i).gameObject);
		layers = new Dictionary<PlanLayer, ActivePlanLayer>();
	}

	private void CreateLayer(PlanLayer layer)
	{
		ActivePlanLayer obj = ((GameObject)GameObject.Instantiate(layerPrefab)).GetComponent<ActivePlanLayer>();
		obj.transform.SetParent(layerParent, false);
		obj.SetToLayer(layer);
		obj.toggle.group = layerToggleGroup;
		layers.Add(layer, obj);
	}

	//If null, create the muliple selected toggle
	private void CreateCountryToggle(Team team)
	{
		ActivePlanCountry obj = ((GameObject)GameObject.Instantiate(team == null ? countryPrefabMultiple : countryPrefab)).GetComponent<ActivePlanCountry>();
		obj.transform.SetParent(countryParent, false);
		obj.toggle.group = countryToggleGroup;

		if (team == null)
		{
			multiCountryToggle = obj.toggle;
			obj.gameObject.SetActive(false);
		}
		else
		{
			if (team.IsGameMaster)
				gmCountryToggle = obj.toggle;
			else			
				countryToggles.Add(team.ID, obj.toggle);
			
			obj.ballImage.color = team.color;
			obj.toggle.onValueChanged.AddListener((value) => CountryToggleClicked());
		}
	}

	private void ClearParameters()
	{
		for (int i = 0; i < parameterParent.transform.childCount; i++)
			Destroy(parameterParent.transform.GetChild(i).gameObject);
		parameters = new Dictionary<EntityPropertyMetaData, ActivePlanParameter>();
		originalParameterValues = null;
	}

	private void CreateParameter(EntityPropertyMetaData parameter)
	{
		ActivePlanParameter obj = ((GameObject)GameObject.Instantiate(parameterPrefab)).GetComponent<ActivePlanParameter>();
		obj.transform.SetParent(parameterParent, false);
		obj.SetToParameter(parameter);
		obj.parameterChangedCallback = OnParameterChanged;
		parameters.Add(parameter, obj);
	}
	#endregion
}
