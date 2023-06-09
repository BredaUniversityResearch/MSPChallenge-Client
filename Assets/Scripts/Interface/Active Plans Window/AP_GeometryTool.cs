using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_GeometryTool : AP_PopoutWindow
	{
		public delegate void EntityTypeChangeCallback(List<EntityType> newTypes);
		public delegate void TeamChangeCallback(int newTeamID);
		public delegate void ParameterChangeCallback(EntityPropertyMetaData parameter, string value);

		[Header("Toolbar")]
		public ToolBar m_toolBar;

		[Header("Layer types")]
		[SerializeField] Transform m_layerTypeParent;
		[SerializeField] GameObject m_layerTypePrefabSingle;
		[SerializeField] GameObject m_layerTypePrefabMulti;
		public EntityTypeChangeCallback m_typeChangeCallback;
		[SerializeField] ToggleGroup m_layerTypeToggleGroup;

		private Dictionary<EntityType, ActivePlanLayerType> m_layerTypes;
		private ActivePlanLayerType m_multipleTypesEntry;
		private bool m_multiType;
		private bool m_ignoreLayerTypeCallback;//Used to ignore callbacks from above (Main.Instance.StartEditingLayer) and below (ActivePlanLayer.toggle)

		[Header("Country")]
		[SerializeField] GameObject[] m_countrySections;
		[SerializeField] Transform m_countryParent;
		[SerializeField] GameObject m_countryPrefab;
		[SerializeField] GameObject m_countryPrefabMultiple;
		public TeamChangeCallback m_countryChangeCallback;
		[SerializeField] ToggleGroup m_countryToggleGroup;

		private Dictionary<int, Toggle> m_countryToggles;
		private Toggle m_gmCountryToggle, m_multiCountryToggle;
		//private int m_selectedCountry;
		private bool m_gmSelectable = true;
		private bool m_ignoreCountryToggleCallback;

		[Header("Parameters")]
		[SerializeField] GameObject[] m_parameterSections;
		[SerializeField] Transform m_parameterParent;
		[SerializeField] GameObject m_parameterPrefab;
		public ParameterChangeCallback m_parameterChangeCallback;

		private Dictionary<EntityPropertyMetaData, ActivePlanParameter> m_parameters;
		private Dictionary<EntityPropertyMetaData, string> m_originalParameterValues;
		private PlanLayer m_currentlyEditingLayer;

		private bool m_initialised;

		public PlanLayer CurrentlyEditingLayer => m_currentlyEditingLayer;

		void Initialise()
		{
			m_initialised = true;
			if (!SessionManager.Instance.AreWeGameMaster)
			{
				foreach (GameObject go in m_countrySections)
					go.SetActive(false);
			}
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			if (!m_initialised)
				Initialise();
			base.OpenToContent(a_content, a_toggle, a_APWindow);
		}

		private void OnDisable()
		{
			if (m_currentlyEditingLayer != null)
			{
				LayerManager.Instance.SetLayerVisibilityLock(m_currentlyEditingLayer.BaseLayer, false);
				ConstraintManager.Instance.CheckConstraints(m_plan);
				IssueManager.Instance.SetIssueInstancesToPlan(m_plan);
				m_APWindow.RefreshIssueText();
			}
			Main.Instance.fsm.ClearUndoRedo();
			Main.Instance.fsm.StopEditing();
			m_currentlyEditingLayer = null;
		}

		public void OnCountriesLoaded()
		{
			m_countryToggles = new Dictionary<int, Toggle>();
			foreach (Team team in SessionManager.Instance.GetTeams())
				if (!team.IsAreaManager)
					CreateCountryToggle(team);

			//Create the multiple selected toggle
			CreateCountryToggle(null);
		}

		public void StartEditingLayer(PlanLayer a_layer)
		{
			//==== General layer setup ==== 

			Main.Instance.fsm.SetInterruptState(null);
			LayerManager.Instance.SetNonReferenceLayers(new HashSet<AbstractLayer>() { a_layer.BaseLayer }, false, true);
			LayerManager.Instance.ShowLayer(a_layer.BaseLayer);
			LayerManager.Instance.SetLayerVisibilityLock(a_layer.BaseLayer, true);
			LayerManager.Instance.RedrawVisibleLayers();

			//TODO CHECK: assumes the window always closes between layer edits (&OnDisable is called), check this

			//Clear and recreate layer types
			m_multiType = a_layer.BaseLayer.m_multiTypeSelect;
			m_layerTypeToggleGroup.allowSwitchOff = m_multiType;
			ClearLayerTypes();
			foreach (KeyValuePair<int, EntityType> kvp in a_layer.BaseLayer.m_entityTypes)
				CreateLayerType(kvp.Value, kvp.Value.availabilityDate <= a_layer.Plan.StartTime);
			CreateMultipleLayerType();
			SetNoEntityTypesSelected();

			//Clear and recreate parameters
			ClearParameters();
			if (a_layer.BaseLayer.m_propertyMetaData == null || a_layer.BaseLayer.m_propertyMetaData.Count == 0)
				SetParameterSectionActive(false);
			else
			{
				bool activeParamsOnLayer = false;
				foreach (EntityPropertyMetaData param in a_layer.BaseLayer.m_propertyMetaData)
					if (param.ShowInEditMode)
					{
						CreateParameter(param);
						activeParamsOnLayer = true;
					}
				if (activeParamsOnLayer)
					SetParameterSectionActive(true);
				else
					SetParameterSectionActive(false);
			}

			//Set admin country option available/unavailable
			if (SessionManager.Instance.AreWeGameMaster)
				GMSelectable = !a_layer.BaseLayer.IsEnergyLayer();

			m_currentlyEditingLayer = a_layer;
			Main.Instance.fsm.StartEditingLayer(a_layer); //Should be called after content set
		}

		void SetParameterSectionActive(bool a_value)
		{
			foreach (GameObject go in m_parameterSections)
				go.SetActive(a_value);
		}

		public void SetObjectChangeInteractable(bool a_value)
		{
			SetEntityTypeSelectionInteractable(a_value);
			SetParameterInteractability(a_value, false);
			SetCountrySelectionInteractable(a_value);
		}

		public void SetTeamAndTypeToBasicIfEmpty()
		{
			SetEntityTypeToBasicIfEmpty();
			if (SessionManager.Instance.AreWeGameMaster)
				SetTeamToBasicIfEmpty();
		}

		public void SetActivePlanWindowInteractability(bool value, bool parameterValue = false)
		{
			SetParameterInteractability(parameterValue);
			if (!value)
			{
				DeselectAllEntityTypes();
				if (SessionManager.Instance.AreWeGameMaster)
					SelectedTeam = -2;
			}
		}

		public void SetToSelection(List<List<EntityType>> entityTypes, int team, List<Dictionary<EntityPropertyMetaData, string>> selectedParams)
		{
			SetSelectedEntityTypes(entityTypes);
			SetSelectedParameters(selectedParams);
			if (SessionManager.Instance.AreWeGameMaster)
			{
				SelectedTeam = team;
			}
		}

		#region Country Selection
		public void SetTeamToBasicIfEmpty()
		{
			foreach (KeyValuePair<int, Toggle> kvp in m_countryToggles)
				if (kvp.Value.isOn)
					return;
			SelectedTeam = m_countryToggles.GetFirstKey();
		}

		//Is the GM team an option in the dropdown
		public bool GMSelectable
		{
			get { return m_gmSelectable; }
			set
			{
				if (value != m_gmSelectable)
				{
					m_gmSelectable = value;
					m_gmCountryToggle.gameObject.SetActive(m_gmSelectable);
				}
			}
		}

		public void SetCountrySelectionInteractable(bool value)
		{
			foreach (KeyValuePair<int, Toggle> kvp in m_countryToggles)
				kvp.Value.interactable = value;
		}

		//Get/Set selected team by team ID 
		public int SelectedTeam
		{
			get
			{
				foreach (KeyValuePair<int, Toggle> kvp in m_countryToggles)
					if (kvp.Value.isOn)
						return kvp.Key;
				return m_countryToggles.GetFirstKey();
			}
			set
			{
				m_ignoreCountryToggleCallback = true;
				if (value < -1)
				{
					//Select none
					foreach (KeyValuePair<int, Toggle> kvp in m_countryToggles)
						if (kvp.Value.isOn)
							kvp.Value.isOn = false;
					m_gmCountryToggle.isOn = false;
					m_multiCountryToggle.gameObject.SetActive(false);
				}
				else if (value == -1)
				{
					//Multiple selected
					m_multiCountryToggle.gameObject.SetActive(true);
					m_multiCountryToggle.isOn = true;
				}
				else if (m_countryToggles.ContainsKey(value))
				{
					m_countryToggles[value].isOn = true;
				}
				m_ignoreCountryToggleCallback = false;
			}
		}

		private void CountryToggleClicked()
		{
			if (m_ignoreCountryToggleCallback)
				return;
			m_multiCountryToggle.gameObject.SetActive(false);
			if (m_countryChangeCallback != null)
				m_countryChangeCallback(SelectedTeam);
		}
		#endregion

		#region Layer Type Selection
		public void DeselectAllEntityTypes()
		{
			m_multipleTypesEntry.gameObject.SetActive(false);
			SetNoEntityTypesSelected();
		}

		public void SetEntityTypeSelectionInteractable(bool value)
		{
			foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in m_layerTypes)
				kvp.Value.toggle.interactable = value;
		}

		public void SetSelectedEntityTypes(List<List<EntityType>> selectedTypes)
		{
			//If null, display nothing selected
			if (selectedTypes == null || selectedTypes.Count == 0)
			{
				m_multipleTypesEntry.gameObject.SetActive(false);
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
						//TODO OPTIM: this can be greatly optimized if entity types are sorted by key, current worst case: selectedTypes.count * selectedTypes[0].count^2
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
			m_multipleTypesEntry.gameObject.SetActive(true);
			m_multipleTypesEntry.toggle.isOn = true;
		}

		private void SetNoEntityTypesSelected()
		{
			m_ignoreLayerTypeCallback = true;
			foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in m_layerTypes)
				if (kvp.Value.toggle.isOn)
					kvp.Value.toggle.isOn = false;
			m_ignoreLayerTypeCallback = false;
		}

		private void SetSelectedEntityTypes(List<EntityType> selectedTypes)
		{
			m_ignoreLayerTypeCallback = true;
			m_multipleTypesEntry.gameObject.SetActive(false);
			if (selectedTypes == null)
				SetNoEntityTypesSelected();
			else
			{
				//If multitype they're not in the toggle group, so first disable all
				if (m_multiType)
					foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in m_layerTypes)
						if (kvp.Value.toggle.isOn)
							kvp.Value.toggle.isOn = false;

				foreach (EntityType t in selectedTypes)
					m_layerTypes[t].toggle.isOn = true;
			}
			m_ignoreLayerTypeCallback = false;
		}

		private void LayerTypeToggleClicked(bool value)
		{
			if (m_ignoreLayerTypeCallback)
				return;
			if (!value && !m_multiType)
				return;
			m_multipleTypesEntry.gameObject.SetActive(false);
			if (m_typeChangeCallback != null)
				m_typeChangeCallback(GetEntityTypeSelection());
		}

		public List<EntityType> GetEntityTypeSelection()
		{
			List<EntityType> result = new List<EntityType>();

			foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in m_layerTypes)
				if (kvp.Value.toggle.isOn)
					result.Add(kvp.Key);

			if (result.Count == 0)
				result.Add(m_layerTypes.GetFirstKey());
			return result;
		}

		public void SetEntityTypeToBasicIfEmpty()
		{
			foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in m_layerTypes)
			{
				if (kvp.Value.toggle.isOn)
					return;
			}

			//Select first interactable layer type
			foreach (KeyValuePair<EntityType, ActivePlanLayerType> kvp in m_layerTypes)
			{

				if (kvp.Value.toggle.interactable)
					SetSelectedEntityTypes(new List<EntityType>() { m_layerTypes.GetFirstKey() });
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
						m_parameters[parameter].SetValue(m_originalParameterValues[parameter]);
						return;
					}
				}
				else
				{
					if (Util.ParseToInt(value) < 1)
					{
						m_parameters[parameter].SetValue(m_originalParameterValues[parameter]);
						return;
					}
				}
			}
			//Only invoke callback if value changed
			if (m_originalParameterValues != null && m_originalParameterValues[parameter] != value && m_parameterChangeCallback != null)
			{
				m_parameterChangeCallback(parameter, value);
				m_originalParameterValues[parameter] = value;
			}
		}

		public void SetParameterInteractability(bool value, bool reset = true)
		{
			foreach (var kvp in m_parameters)
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
				SetParameterInteractability(true, false);
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

				m_originalParameterValues = new Dictionary<EntityPropertyMetaData, string>();
				//show a value or "multiple" per entity type
				foreach (var kvp in identical)
				{
					//If all identical, use the first. Otherwise a preset value.
					string value = kvp.Value ? selectedParams[0][kvp.Key] : "multiple";
					m_parameters[kvp.Key].SetValue(value);
					m_originalParameterValues[kvp.Key] = value;
				}
				SetParameterInteractability(true, false);
			}
		}

		public void SetParameterValues(Dictionary<EntityPropertyMetaData, string> values)
		{
			m_originalParameterValues = values;
			foreach (var kvp in values)
			{
				m_parameters[kvp.Key].SetValue(kvp.Value);
			}
		}
		#endregion

		#region Object creation
		private void ClearLayerTypes()
		{
			for (int i = 0; i < m_layerTypeParent.transform.childCount; i++)
				Destroy(m_layerTypeParent.transform.GetChild(i).gameObject);
			m_layerTypes = new Dictionary<EntityType, ActivePlanLayerType>();
		}

		private void CreateLayerType(EntityType type, bool interactable)
		{
			ActivePlanLayerType obj = ((GameObject)GameObject.Instantiate(m_multiType ? m_layerTypePrefabMulti : m_layerTypePrefabSingle)).GetComponent<ActivePlanLayerType>();
			obj.transform.SetParent(m_layerTypeParent, false);
			obj.SetToType(type, !interactable);
			if (!m_multiType)
				obj.toggle.group = m_layerTypeToggleGroup;
			obj.toggle.onValueChanged.AddListener((value) => LayerTypeToggleClicked(value));
			obj.DisabledIfNotSelected = !interactable;
			m_layerTypes.Add(type, obj);
		}

		private void CreateMultipleLayerType()
		{
			ActivePlanLayerType obj = ((GameObject)GameObject.Instantiate(m_multiType ? m_layerTypePrefabMulti : m_layerTypePrefabSingle)).GetComponent<ActivePlanLayerType>();
			obj.transform.SetParent(m_layerTypeParent, false);
			obj.SetToMultiple();
			if (!m_multiType)
				obj.toggle.group = m_layerTypeToggleGroup;
			m_multipleTypesEntry = obj;
			obj.gameObject.SetActive(false);
		}


		//If null, create the muliple selected toggle
		private void CreateCountryToggle(Team team)
		{
			ActivePlanCountry obj = ((GameObject)GameObject.Instantiate(team == null ? m_countryPrefabMultiple : m_countryPrefab)).GetComponent<ActivePlanCountry>();
			obj.transform.SetParent(m_countryParent, false);
			obj.toggle.group = m_countryToggleGroup;

			if (team == null)
			{
				m_multiCountryToggle = obj.toggle;
				obj.gameObject.SetActive(false);
			}
			else
			{
				if (team.IsGameMaster)
					m_gmCountryToggle = obj.toggle;
				else
					m_countryToggles.Add(team.ID, obj.toggle);

				obj.ballImage.color = team.color;
				obj.toggle.onValueChanged.AddListener((value) => CountryToggleClicked());
			}
		}

		private void ClearParameters()
		{
			for (int i = 0; i < m_parameterParent.transform.childCount; i++)
				Destroy(m_parameterParent.transform.GetChild(i).gameObject);
			m_parameters = new Dictionary<EntityPropertyMetaData, ActivePlanParameter>();
			m_originalParameterValues = null;
		}

		private void CreateParameter(EntityPropertyMetaData parameter)
		{
			ActivePlanParameter obj = ((GameObject)GameObject.Instantiate(m_parameterPrefab)).GetComponent<ActivePlanParameter>();
			obj.transform.SetParent(m_parameterParent, false);
			obj.SetToParameter(parameter);
			obj.parameterChangedCallback = OnParameterChanged;
			m_parameters.Add(parameter, obj);
		}
		#endregion
	}
}
