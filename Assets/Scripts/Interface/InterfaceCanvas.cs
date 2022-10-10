using System.Collections.Generic;
using ColourPalette;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace MSP2050.Scripts
{
	public class InterfaceCanvas : MonoBehaviour
	{
		private static InterfaceCanvas singleton;

		public static InterfaceCanvas Instance
		{
			get
			{
				return singleton;
			}
		}
    
		[HideInInspector]
		public Canvas canvas;

		[Header("References")]
		public TimeBar timeBar;
		public MapScale mapScale;
		public ToolBar toolBar;
		public LayerInterface layerInterface;
		public GenericWindow layerSelect;
		public ActiveLayerWindow activeLayers;
		public ActivePlanWindow activePlanWindow;
		public PlanWizard planWizard;
		public PlansMonitor plansMonitor;
		public LoadingScreen loadingScreen;
		public UnLoadingScreen unLoadingScreen;
		public PropertiesWindow propertiesWindow;
		public LayerProbeWindow layerProbeWindow;
		public WebViewWindow webViewWindow;
		public GameObject networkingBlocker;
		public GenericWindow impactToolWindow;
		public HEBGraph.HEBGraph ImpactToolGraph;

		[Header("Game Menu")]
		public GameMenu gameMenu;
		public Options options;

		[Header("Menu Bar")]
		public MenuBarLogo menuBarLogo;
		public MenuBarToggle menuBarLayers;
		public MenuBarToggle menuBarPlanWizard;
		public MenuBarToggle menuBarObjectivesMonitor;
		public MenuBarToggle menuBarPlansMonitor;
		public MenuBarToggle menuBarImpactTool;
		public MenuBarToggle menuBarActiveLayers;
		public MenuBarToggle menuBarGameMenu;

		[Header("KPI")]
		public KPIRoot KPIEcology;

		[Header("Objectives")]
		public ObjectivesMonitor objectivesMonitor;
		public NewObjectiveWindow newObjectiveWindow;

		[Header("LineMaterials")]
		public Material[] lineMaterials;
		public Sprite[] activeLayerLineSprites;

		[Header("Colours")]
		public ColourAsset accentColour;
		public ColourAsset regionColour;
		public RegionSettingsAsset regionSettings;

		private List<Button> ToolbarButtons = new List<Button>();

		[HideInInspector]
		public bool ignoreLayerToggleCallback;//If this is true the layer callback labda functions will return immediately

		private Dictionary<string, Button> buttonUIReferences = new Dictionary<string, Button>();
		private Dictionary<string, Toggle> toggleUIReferences = new Dictionary<string, Toggle>();
		private Dictionary<string, GameObject> genericUIReferences = new Dictionary<string, GameObject>();
		public event Action<string, string[]> interactionEvent;
		public event Action<string, GameObject> uiReferenceRegisteredEvent;

		private void Awake()
		{
			singleton = this;
		}

		private void OnDestroy()
		{
			singleton = null;
		}

		void Start()
		{
			canvas.scaleFactor = (GameSettings.Instance.UIScale + 1f) / 4f;
			menuBarActiveLayers.toggle.isOn = true;
			for (int i = 0; i < lineMaterials.Length; i++)
			{
				lineMaterials[i] = new Material(lineMaterials[i]);
			}
		}

		public void SetRegionWithName(string name)
		{
			SetRegion(regionSettings.GetRegionInfo(name));
		}

		public void SetRegion(RegionInfo region)
		{
			menuBarLogo.SetRegionLogo(region);
			gameMenu.SetRegion(region);
			regionColour.SetValue(region.colour);
		}

		public void SetAccent(Color a_color)
		{
			accentColour.SetValue(a_color);

		}

		public static void ShowNetworkingBlocker()
		{
			Instance.networkingBlocker.transform.SetAsLastSibling();
			Instance.networkingBlocker.SetActive(true);
		}

		public static void HideNetworkingBlocker()
		{
			Instance.networkingBlocker.SetActive(false);
		}

		public void StartEditingLayer(AbstractLayer layer)
		{
			ToolbarVisibility(true);
			toolBar.ShowToolBar(true);
			ToolbarTitleVisibility(true, FSM.ToolbarInput.Create);
			ToolbarTitleVisibility(true, FSM.ToolbarInput.Delete);
			ToolbarVisibility(false, FSM.ToolbarInput.Difference, FSM.ToolbarInput.Intersect, FSM.ToolbarInput.Union);
			ToolbarTitleVisibility(false, FSM.ToolbarInput.Union);
			toolBar.SetCreateButtonSprite(layer);
			ToolbarEnable(true);
		}

		public static void SetLineMaterialTiling(float tiling)
		{
			foreach (Material mat in Instance.lineMaterials)
			{
				if (mat != null)
				{
					if (mat.HasProperty("_MainTex"))
						mat.mainTextureScale = new Vector2(tiling, 1f);
				}
			}
		}

		public void StopEditing()
		{
			ToolbarEnable(false);
			Instance.toolBar.ShowToolBar(false);
		}
		
		public void ToolbarTitleVisibility(bool enabled, FSM.ToolbarInput button)
		{
			for (int i = 0; i < ToolbarButtons.Count; i++)
			{
				if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == button)
				{
					ToolbarButtons[i].transform.parent.parent.Find("Label").gameObject.SetActive(enabled);
					ToolbarButtons[i].transform.parent.gameObject.SetActive(enabled);
				}
			}
		}

		public void ToolbarVisibility(bool enabled, params FSM.ToolbarInput[] buttons)
		{
			for (int i = 0; i < ToolbarButtons.Count; i++)
			{
				if (buttons.Length <= 0)
				{
					ToolbarButtons[i].gameObject.SetActive(enabled);
				}
				else
				{
					for (int j = 0; j < buttons.Length; j++)
					{
						if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == buttons[j])
						{
							ToolbarButtons[i].gameObject.SetActive(enabled);
						}
					}
				}
			}
		}

		public void SetToolbarMode(ToolBar.DrawingMode drawingMode)
		{
			if (drawingMode == ToolBar.DrawingMode.Create)
			{
				toolBar.CreateMode();
			}
			else if (drawingMode == ToolBar.DrawingMode.Edit)
			{
				toolBar.EditMode();
			}
		}

		public void ToolbarEnable(bool enabled, params FSM.ToolbarInput[] buttons)
		{
			for (int i = 0; i < ToolbarButtons.Count; i++)
			{
				if (buttons.Length <= 0)
				{
					toolBar.SetActive(ToolbarButtons[i], enabled);
				}
				else
				{
					for (int j = 0; j < buttons.Length; j++)
					{
						if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == buttons[j])
						{
							toolBar.SetActive(ToolbarButtons[i], enabled);
						}
					}
				}
			}
		}
		
		public static List<EntityType> GetCurrentEntityTypeSelection()
		{
			return Instance.activePlanWindow.GetEntityTypeSelection();
		}

		public static int GetCurrentTeamSelection()
		{
			return Instance.activePlanWindow.SelectedTeam;
		}

		public void SetActiveplanWindowToSelection(List<List<EntityType>> entityTypes, int team, List<Dictionary<EntityPropertyMetaData, string>> selectedParams)
		{
			activePlanWindow.SetSelectedEntityTypes(entityTypes);
			activePlanWindow.SetSelectedParameters(selectedParams);
			if (SessionManager.Instance.AreWeGameMaster)
			{
				activePlanWindow.SelectedTeam = team;
			}
		}

		public void SetTeamAndTypeToBasicIfEmpty()
		{
			activePlanWindow.SetEntityTypeToBasicIfEmpty();
			if (SessionManager.Instance.AreWeGameMaster)
				activePlanWindow.SetTeamToBasicIfEmpty();
		}

		public void SetActivePlanWindowInteractability(bool value, bool parameterValue = false)
		{
			activePlanWindow.SetParameterInteractability(parameterValue);
			if (!value)
			{
				activePlanWindow.DeselectAllEntityTypes();
				if (SessionManager.Instance.AreWeGameMaster)
					activePlanWindow.SelectedTeam = -2;
			}
		}

		public void SetActivePlanWindowChangeable(bool value)
		{
			activePlanWindow.SetObjectChangeInteractable(value);
		}

		public void RegisterToolbarButton(Button button)
		{
			ToolbarButtons.Add(button);
		}

		public void TriggerInteractionCallback(string name, string[] tags)
		{
			interactionEvent?.Invoke(name, tags);
		}

		public void RegisterUIReference(string name, Button button)
		{
			buttonUIReferences[name] = button;
			uiReferenceRegisteredEvent?.Invoke(name, button.gameObject);
		}

		public void RegisterUIReference(string name, Toggle toggle)
		{
			toggleUIReferences[name] = toggle;
			uiReferenceRegisteredEvent?.Invoke(name, toggle.gameObject);
		}

		public void RegisterUIReference(string name, GameObject ui)
		{
			genericUIReferences[name] = ui;
			uiReferenceRegisteredEvent?.Invoke(name, ui.gameObject);
		}

		public void UnregisterUIReference(string name)
		{
			if (buttonUIReferences.ContainsKey(name))
				buttonUIReferences.Remove(name);
			else if (toggleUIReferences.ContainsKey(name))
				toggleUIReferences.Remove(name);
			else if (genericUIReferences.ContainsKey(name))
				genericUIReferences.Remove(name);
		}

		public Button GetUIButton(string name)
		{
			if (buttonUIReferences.TryGetValue(name, out var result))
				return result;
			return null;
		}

		public Toggle GetUIToggle(string name)
		{
			if (toggleUIReferences.TryGetValue(name, out var result))
				return result;
			return null;
		}

		public GameObject GetUIObject(string name)
		{
			if (genericUIReferences.TryGetValue(name, out var result))
				return result;
			if (buttonUIReferences.TryGetValue(name, out var result1))
				return result1.gameObject;
			if (toggleUIReferences.TryGetValue(name, out var result2))
				return result2.gameObject;
			return null;
		}

		public GameObject GetUIGeneric(string name)
		{
			if (genericUIReferences.TryGetValue(name, out var result))
				return result;
			return null;
		}
	}
}