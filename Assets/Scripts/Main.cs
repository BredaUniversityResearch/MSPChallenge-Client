using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using GeoJSON.Net.Feature;
using UnityEngine.Audio;
using Newtonsoft.Json;
using DotSpatial.Projections;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    public const float SCALE = 1000.0f;
    public const double SCALE_DOUBLE = 1000.0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD 
    public static bool IsDeveloper = true;
#else
    public static bool IsDeveloper = false;
#endif

	public DataVisualizationSettings DataVisualizationSettings;
    public AudioMixer AudioMixer;

    private static Main instance;

	private FSM fsm;
	public bool fsmActive;
	public static FSM FSM => instance.fsm;

    private static bool interceptQuit = true;
    private static bool editingPlanDetailsContent = false;
    private static bool preventPlanAndTabChange = false;

	private static ProjectionInfo mspCoordinateProjection;
    private static ProjectionInfo geoJSONCoordinateProjection;
    public static int currentExpertiseIndex;
    public static MspGlobalData MspGlobalData { get; set; }
	public static SELGameClientConfig SelConfig{ get; set; }

    private ESimulationType availableSimulations;

	public delegate void GlobalDataLoadedDelegate();
	public static event GlobalDataLoadedDelegate OnGlobalDataLoaded;
	public static event Action OnFinishedLoadingLayers; //Called when we finished loading all layers and right before the first tick is requested.
	public static event Action OnPostFinishedLoadingLayers;

	[SerializeField]
	private int debugTargetSessionId = 1;

    protected void Start()
    {
		instance = this;
		System.Threading.Thread.CurrentThread.CurrentCulture = Localisation.NumberFormatting;
		VisualizationUtil.VisualizationSettings = DataVisualizationSettings;
        
        //Setup projection parameters for later conversion
        mspCoordinateProjection = DotSpatial.Projections.ProjectionInfo.FromProj4String("+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs");
        geoJSONCoordinateProjection = DotSpatial.Projections.ProjectionInfo.FromProj4String("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");

        GameSettings.SetAudioMixer(AudioMixer);

        fsm = new FSM();
		fsmActive = true;

		//If we don't have global data yet we probably haven't gone  through the login screen. Load this now.
		if (MspGlobalData == null)
		{
			Server.GameSessionId = debugTargetSessionId;
            LoadGlobalData();
		}
		else
		{
            GlobalDataLoaded();
        }

		GameState.Initialise();
        Application.wantsToQuit += () => 
        {
            StartCoroutine(QuitAtEndOfFrame());
            return !interceptQuit;
        };
    }

    IEnumerator QuitAtEndOfFrame()
    {
        InterfaceCanvas.Instance.unLoadingScreen.Activate();
        yield return new WaitForEndOfFrame();
        interceptQuit = false;
        Application.Quit();
    }

    public static void QuitGame()
    {
        interceptQuit = false;
        if (InterfaceCanvas.Instance != null)
            InterfaceCanvas.Instance.unLoadingScreen.Activate();
        Application.Quit();
    }

    protected void Update()
    {
		if (fsmActive)
		{
			fsm.Update();
		}
		ServerCommunication.Update();

        MaterialManager.Update();
    }

    void GlobalDataLoaded()
    {
        currentExpertiseIndex = PlayerPrefs.GetInt(LoginMenu.LOGIN_EXPERTISE_INDEX_STR, -1);
        ImportLayers();
        if (MspGlobalData.expertise_definitions != null)
            InterfaceCanvas.Instance.menuBarActiveLayers.toggle.isOn = true;
		ParseAvailableSimulations(MspGlobalData.configured_simulations);
		InterfaceCanvas.Instance.SetRegionWithName(MspGlobalData.region);

		if (OnGlobalDataLoaded != null)
		{
			OnGlobalDataLoaded();
		}
	}

    public void StartSetOperations()
    {
        UIManager.StartSetOperations();
        fsm.StartSetOperations();
    }

    public static void AllLayersImported()
    {
        LayerManager.FinishedImportingLayers();
		if (OnFinishedLoadingLayers != null)
		{
			OnFinishedLoadingLayers();
		}

        if (OnPostFinishedLoadingLayers != null)
            OnPostFinishedLoadingLayers();

		//TeamManager.SetEEZs();
		LayerInterface.SortLayerToggles();
        InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("Existing plans");
        instance.StartCoroutine(UpdateData.GetFirstUpdate());
		instance.StartCoroutine(UpdateData.TickServerCoroutine());
        instance.StartCoroutine(VisualizationUtil.UpdateScales());

        ConstraintManager.LoadRestrictions();
	}

    void LoadGlobalData()
    {
        NetworkForm form = new NetworkForm();
        ServerCommunication.DoRequest<MspGlobalData>(Server.GetGlobalData(), form, handleLoadGlobalData);
    }

    private void handleLoadGlobalData(MspGlobalData newGlobalData)
    {
		MspGlobalData = newGlobalData;
		GlobalDataLoaded();       
    }

    public static void FirstUpdateTickComplete()
    {
		InterfaceCanvas.Instance.loadingScreen.OnFinishedLoading();
        instance.StartCoroutine(UpdateData.GetUpdates());
    }

    public void ImportLayers()
    {
        LayerImporter.ImportLayerMetaData();
    }

    public static Plan CurrentlyEditingPlan
    {
        get { return PlanDetails.LayersTab.LockedPlan; }
	}

    public static bool InEditMode
    {
        get { return CurrentlyEditingPlan != null; }
    }

    public static bool EditingPlanDetailsContent
    {
	    get { return editingPlanDetailsContent; }
	    set { editingPlanDetailsContent = value; }
    }

	public static bool PreventPlanAndTabChange
	{
		get { return preventPlanAndTabChange; }
		set { preventPlanAndTabChange = value; }
	}

	public static ETextState GetTextState()
    {
        if (InEditMode)
            return ETextState.Edit;
        if (PlanManager.planViewing == null) //This currently shows current and past (through ViewAtTime)
            return ETextState.Current;
        return ETextState.View;
    }

	private void ParseAvailableSimulations(ESimulationType[] configuredSimulations)
	{
		availableSimulations = ESimulationType.None;
		if (configuredSimulations != null)
		{
			foreach (ESimulationType simType in configuredSimulations)
			{
				availableSimulations |= simType;
			}
		}
	}

	public static bool IsSimulationConfigured(ESimulationType simulationType)
	{
		return (instance.availableSimulations & simulationType) == simulationType;
	}

	public static void GetRealWorldMousePosition(out double x, out double y)
	{
		GetRealWorldPosition(instance.fsm.GetWorldMousePosition(), out x, out y);
	}

	public static void GetRealWorldPosition(Vector3 position, out double x, out double y)
	{
        x = (double)position.x * SCALE_DOUBLE;
        y = (double)position.y * SCALE_DOUBLE;
	}
    
    public static FSM.CursorType CursorType
    {
        get { return instance.fsm.CurrentCursorType; }
        set { instance.fsm.SetCursor(value); }
    }

    public static void InterruptFSMState(Func<FSM, FSMState> creationFunction)
    {
        instance.fsm.SetInterruptState(creationFunction.Invoke(instance.fsm));
    }

    public static void CancelFSMInterruptState()
    {
        instance.fsm.SetInterruptState(null);
    }

    public static bool LayerVisibleForCurrentExpertise(string layerName)
    {
        if (currentExpertiseIndex == -1)
            return false;
        foreach(var layer in MspGlobalData.expertise_definitions[currentExpertiseIndex].visible_layers)
            if (layer == layerName)
                return true;
        return false;
    }

    public static bool LayerSelectedForCurrentExpertise(string layerName)
    {
        if (currentExpertiseIndex == -1)
            return false;
        foreach (var layer in MspGlobalData.expertise_definitions[currentExpertiseIndex].selected_layers)
            if (layer == layerName)
                return true;
        return false;
    }

    public static double[] ConvertToGeoJSONCoordinate(double[] mspCoordinate)
    {
        Reproject.ReprojectPoints(mspCoordinate, new double[] { 1 }, mspCoordinateProjection, geoJSONCoordinateProjection, 0, 1);
        return mspCoordinate;
    }

    public static double[] ConvertToMSPCoordinate(double[] geoJSONCoordinate)
    {
        Reproject.ReprojectPoints(geoJSONCoordinate, new double[] { 1 }, geoJSONCoordinateProjection, mspCoordinateProjection, 0, 1);
        return geoJSONCoordinate;
    }

	public static bool ControlKeyDown => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
}