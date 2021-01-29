using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ColourPalette;

public class InterfaceCanvas : MonoBehaviour
{
    private static InterfaceCanvas singleton;

    public static InterfaceCanvas Instance
    {
        get
        {
            //if (singleton == null)
            //    singleton = (InterfaceCanvas)FindObjectOfType(typeof(InterfaceCanvas));
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

    [Header("Game Menu")]
    public GameMenu gameMenu;
	public Options options;

    [Header("Menu Bar")]
    public MenuBarLogo menuBarLogo;
    public MenuBarToggle menuBarLayers;
    public MenuBarToggle menuBarPlanWizard;
    public MenuBarToggle menuBarObjectivesMonitor;
    public MenuBarToggle menuBarPlansMonitor;
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

	private void Awake()
	{
		singleton = this;
	}

	void Start()
	{
		canvas.scaleFactor = GameSettings.UIScale;
        //ColorPalette.instance.ApplyAccent();
        menuBarActiveLayers.toggle.isOn = true;

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
		Instance.networkingBlocker.SetActive(true);
	}

	public static void HideNetworkingBlocker()
	{
		Instance.networkingBlocker.SetActive(false);
	}
}