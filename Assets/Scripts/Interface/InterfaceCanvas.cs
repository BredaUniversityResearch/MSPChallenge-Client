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
		
		public LayerPanel layerPanel;
		public GenericWindow layerSelect;
		public ActiveLayerWindow activeLayers;
		public ActivePlanWindow activePlanWindow;
		public PlansList plansList;
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
		public MenuBarToggle menuBarObjectivesMonitor;
		public MenuBarToggle menuBarImpactTool;
		public MenuBarToggle menuBarActiveLayers;
		public MenuBarToggle menuBarGameMenu;
		public MenuBarToggle menuBarPlansList;
		public MenuBarToggle menuBarCreatePlan;

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


		[HideInInspector]
		public LayerInterface layerInterface;

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
			layerInterface = layerPanel.GetComponent<LayerInterface>();
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

		//TODO: replace all references by using geometrytool directly
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
			return Instance.activePlanWindow.m_geometryTool.GetEntityTypeSelection();
		}

		public static int GetCurrentTeamSelection()
		{
			return Instance.activePlanWindow.m_geometryTool.SelectedTeam;
		}

		public void SetActiveplanWindowToSelection(List<List<EntityType>> entityTypes, int team, List<Dictionary<EntityPropertyMetaData, string>> selectedParams)
		{
			activePlanWindow.m_geometryTool.SetSelectedEntityTypes(entityTypes);
			activePlanWindow.m_geometryTool.SetSelectedParameters(selectedParams);
			if (SessionManager.Instance.AreWeGameMaster)
			{
				activePlanWindow.m_geometryTool.SelectedTeam = team;
			}
		}

		public void SetTeamAndTypeToBasicIfEmpty()
		{
			//TODO: remove and replace with direct call
			activePlanWindow.m_geometryTool.SetEntityTypeToBasicIfEmpty();
			if (SessionManager.Instance.AreWeGameMaster)
				activePlanWindow.m_geometryTool.SetTeamToBasicIfEmpty();
		}

		public void SetActivePlanWindowInteractability(bool value, bool parameterValue = false)
		{
			activePlanWindow.m_geometryTool.SetParameterInteractability(parameterValue);
			if (!value)
			{
				activePlanWindow.m_geometryTool.DeselectAllEntityTypes();
				if (SessionManager.Instance.AreWeGameMaster)
					activePlanWindow.m_geometryTool.SelectedTeam = -2;
			}
		}

		public void SetActivePlanWindowChangeable(bool value)
		{
			activePlanWindow.m_geometryTool.SetObjectChangeInteractable(value);
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