using System.Collections.Generic;
using ColourPalette;
using UnityEngine;
using UnityEngine.UI;

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

		[Header("Prefabs")]
		[SerializeField]
		private Transform genericWindowParent = null;
		[SerializeField]
		private GameObject genericWindowPrefab = null;

		[Header("References")]
		public List<GenericWindow> genericWindow = new List<GenericWindow>();
		public TimeBar timeBar;
		public MapScale mapScale;
		public ToolBar toolBar;
		public LayerPanel layerPanel;
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
		public LayerInterface layerInterface;

		[HideInInspector]
		public bool ignoreLayerToggleCallback;//If this is true the layer callback labda functions will return immediately

		private void Awake()
		{
			singleton = this;
			layerInterface = layerPanel.GetComponent<LayerInterface>();
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

		/// <summary>
		/// Create a generic window
		/// </summary>
		public GenericWindow CreateGenericWindow () {

			GenericWindow window = GenerateGenericWindow();

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="centered">Center the window?</param>
		public GenericWindow CreateGenericWindow(bool centered, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			if (centered) {
				window.CenterWindow();
			}

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="title">The text in the title bar of the window</param>
		public GenericWindow CreateGenericWindow(string title, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			window.SetTitle(title);

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="title">The text in the title bar of the window</param>
		/// <param name="centered">Center the window?</param>
		public GenericWindow CreateGenericWindow(string title, bool centered, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			window.SetTitle(title);

			if (centered) {
				window.CenterWindow();
			}

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="width">The width of the window</param>
		public GenericWindow CreateGenericWindow(float width, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			window.SetWidth(width);

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="centered">Center the window?</param>
		/// <param name="width">The width of the window</param>
		public GenericWindow CreateGenericWindow(bool centered, float width, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			window.SetWidth(width);

			if (centered) {
				window.CenterWindow();
			}

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="title">The text in the title bar of the window</param>
		/// <param name="width">The width of the window</param>
		public GenericWindow CreateGenericWindow(string title, float width, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			window.SetTitle(title);
			window.SetWidth(width);

			return window;
		}

		/// <summary>
		/// Create a generic window
		/// </summary>
		/// <param name="title">The text in the title bar of the window</param>
		/// <param name="centered">Center the window?</param>
		/// <param name="width">The width of the window</param>
		public GenericWindow CreateGenericWindow(string title, bool centered, float width, bool isProperty = false) {

			GenericWindow window = GenerateGenericWindow();

			window.SetTitle(title);
			window.SetWidth(width);

			if (centered) {
				window.CenterWindow();
			}

			return window;
		}

		/// <summary>
		/// Generate window
		/// </summary>
		private GenericWindow GenerateGenericWindow() {
			// Instantiate prefab
			GameObject go = Instantiate(genericWindowPrefab);

			// Store component
			GenericWindow window = go.GetComponent<GenericWindow>();

			// Add to list
			genericWindow.Add(window);

			// Assign parent
			go.transform.SetParent(genericWindowParent, false);		

			return window;
		}

		/// <summary>
		/// Remove window from list and destroy the gameobject
		/// </summary>
		public void DestroyGenericWindow(GenericWindow window) {
			genericWindow.Remove(window);
			Destroy(window.gameObject);
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

		public static void ShowNetworkingBlocker()
		{
			Instance.networkingBlocker.transform.SetAsLastSibling();
			Instance.networkingBlocker.SetActive(true);
		}

		public static void HideNetworkingBlocker()
		{
			Instance.networkingBlocker.SetActive(false);
		}
		
		//====================================== Below used to be InterfaceCanvas ===============================================

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

		public void StopEditing()
		{
			ToolbarEnable(false);
			Instance.toolBar.ShowToolBar(false);
		}
		
		public static void ShowLayerBar(bool show)
		{
			Instance.layerPanel.gameObject.SetActive(show);
		}

		public static void ShowTimeBar(bool show)
		{
			Instance.timeBar.gameObject.SetActive(show);
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
					//ToolbarButtons[i].interactable = enabled;
					toolBar.SetActive(ToolbarButtons[i], enabled);
				}
				else
				{
					for (int j = 0; j < buttons.Length; j++)
					{
						if (ToolbarButtons[i].GetComponent<ToolbarButtonType>().buttonType == buttons[j])
						{
							//ToolbarButtons[i].interactable = enabled;
							toolBar.SetActive(ToolbarButtons[i], enabled);
						}
					}
				}
			}
		}
		
		public static void CreatePropertiesWindow(SubEntity subentity, Vector3 worldSamplePosition, Vector3 windowPosition)
		{
			InterfaceCanvas.Instance.propertiesWindow.ShowPropertiesWindow(subentity, worldSamplePosition, windowPosition);
		}

		public static void CreateLayerProbeWindow(List<SubEntity> subentities, Vector3 worldSamplePosition, Vector3 windowPosition)
		{
			InterfaceCanvas.Instance.layerProbeWindow.ShowLayerProbeWindow(subentities, worldSamplePosition, windowPosition);
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
	}
}