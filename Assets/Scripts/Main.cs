using System;
using System.Collections;
using DotSpatial.Projections;
using HEBGraph;
using UnityEngine;
using UnityEngine.Audio;

namespace MSP2050.Scripts
{
	public class Main : MonoBehaviour
	{
		public const float SCALE = 1000.0f;
		public const double SCALE_DOUBLE = 1000.0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD 
		public static bool IsDeveloper = true;
#else
    public static bool IsDeveloper = false;
#endif

		private static Main singleton;
		public static Main Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<Main>();
				return singleton;
			}
		}

		public AudioMixer audioMixer;
		public LayerImporter layerImporter;
		public LayerPickerUI layerPickerUI;

		[HideInInspector] public FSM fsm;
		[HideInInspector] public bool fsmActive;

		private bool interceptQuit = true;
		private bool editingPlanDetailsContent = false;
		private bool preventPlanAndTabChange = false;

		private ProjectionInfo mspCoordinateProjection;
		private ProjectionInfo geoJSONCoordinateProjection;
		[HideInInspector] public int currentExpertiseIndex;
		[HideInInspector] public SELGameClientConfig SelConfig{ get; set; }

		private ESimulationType availableSimulations;

		[HideInInspector] public event Action OnFinishedLoadingLayers; //Called when we finished loading all layers and right before the first tick is requested.
		[HideInInspector] public event Action OnPostFinishedLoadingLayers;

		protected void Awake()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;

			System.Threading.Thread.CurrentThread.CurrentCulture = Localisation.NumberFormatting;
        
			//Setup projection parameters for later conversion
			mspCoordinateProjection = DotSpatial.Projections.ProjectionInfo.FromProj4String("+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs");
			geoJSONCoordinateProjection = DotSpatial.Projections.ProjectionInfo.FromProj4String("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");

			GameSettings.Instance.SetAudioMixer(audioMixer);

			fsm = new FSM();
			fsmActive = true;
			Application.wantsToQuit += OnApplicationQuit;

			currentExpertiseIndex = PlayerPrefs.GetInt(LoginContentTabLogin.LOGIN_EXPERTISE_INDEX_STR, -1);
			layerImporter = new LayerImporter(layerPickerUI); //This starts importing meta
			if (SessionManager.Instance.MspGlobalData.expertise_definitions != null)
				InterfaceCanvas.Instance.menuBarActiveLayers.toggle.isOn = true;
			ParseAvailableSimulations(SessionManager.Instance.MspGlobalData.configured_simulations);
			InterfaceCanvas.Instance.SetRegionWithName(SessionManager.Instance.MspGlobalData.region);

			if (SessionManager.Instance.MspGlobalData.dependencies != null)
			{
				HEBGraphData HEBData = SessionManager.Instance.MspGlobalData.dependencies.ToObject<HEBGraphData>();
				if (HEBData?.groups == null || HEBData.links == null)
				{
					Debug.LogWarning("Impact tool data did not match expected format, it will be disabled.");
					SessionManager.Instance.MspGlobalData.dependencies = null;
				}
				else
					InterfaceCanvas.Instance.ImpactToolGraph.Initialise(HEBData);
			}
		}

		void OnDestroy()
		{
			Application.wantsToQuit -= OnApplicationQuit;
		}

		bool OnApplicationQuit()
		{
			if (interceptQuit)
			{
				NetworkForm form = new NetworkForm();
				form.AddField("session_id", SessionManager.Instance.CurrentSessionID);
				ServerCommunication.Instance.DoPriorityRequest(Server.CloseSession(), form, CloseSessionSuccess, CloseSessionFail);
				UpdateManager.Instance?.WsServerCommunicationInteractor?.Stop();
				//StartCoroutine(QuitAtEndOfFrame());
			}
			return !interceptQuit;
		}

		void CloseSessionSuccess(string result)
		{
			StartCoroutine(QuitAtEndOfFrame());
		}

		void CloseSessionFail(ARequest request, string result)
		{
			StartCoroutine(QuitAtEndOfFrame());
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
			if (Instance != null)
			{
				NetworkForm form = new NetworkForm();
				form.AddField("session_id", SessionManager.Instance.CurrentSessionID);
				ServerCommunication.Instance.DoPriorityRequest(Server.CloseSession(), form, Instance.CloseSessionSuccess, Instance.CloseSessionFail);
				UpdateManager.Instance.WsServerCommunicationInteractor?.Stop();
			}
			else
			{
				Application.Quit();
			}
		}

		protected void Update()
		{
			if (fsmActive)
			{
				fsm.Update();
			}
			ServerCommunication.Instance.UpdateCommunication();

			MaterialManager.Instance.Update();
		}

		public void AllLayersImported()
		{
			layerPickerUI = null;
			LayerManager.Instance.FinishedImportingLayers();
			if (OnFinishedLoadingLayers != null)
			{
				OnFinishedLoadingLayers();
				OnFinishedLoadingLayers = null;
			}

			if (OnPostFinishedLoadingLayers != null)
			{
				OnPostFinishedLoadingLayers();
				OnPostFinishedLoadingLayers = null;
			}

			LayerInterface.SortLayerToggles();
			InterfaceCanvas.Instance.loadingScreen.SetNextLoadingItem("Existing plans");
			StartCoroutine(UpdateManager.Instance.GetFirstUpdate());

			ConstraintManager.Instance.LoadRestrictions();
		}

		public void FirstUpdateTickComplete()
		{
			InterfaceCanvas.Instance.loadingScreen.OnFinishedLoading();
			StartCoroutine(UpdateManager.Instance.GetUpdates());
		}

		public static Plan CurrentlyEditingPlan
		{
			get { return PlanDetails.LayersTab.LockedPlan; }
		}

		public static bool InEditMode
		{
			get { return CurrentlyEditingPlan != null; }
		}

		public bool EditingPlanDetailsContent
		{
			get { return editingPlanDetailsContent; }
			set { editingPlanDetailsContent = value; }
		}

		public bool PreventPlanAndTabChange
		{
			get { return preventPlanAndTabChange; }
			set { preventPlanAndTabChange = value; }
		}

		public static ETextState GetTextState()
		{
			if (InEditMode)
				return ETextState.Edit;
			if (PlanManager.Instance.planViewing == null) //This currently shows current and past (through ViewAtTime)
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

		public bool IsSimulationConfigured(ESimulationType simulationType)
		{
			return (availableSimulations & simulationType) == simulationType;
		}

		public void GetRealWorldMousePosition(out double x, out double y)
		{
			GetRealWorldPosition(fsm.GetWorldMousePosition(), out x, out y);
		}

		public static void GetRealWorldPosition(Vector3 position, out double x, out double y)
		{
			x = (double)position.x * SCALE_DOUBLE;
			y = (double)position.y * SCALE_DOUBLE;
		}
    
		public FSM.CursorType CursorType
		{
			get { return fsm.CurrentCursorType; }
			set { fsm.SetCursor(value); }
		}

		public void InterruptFSMState(Func<FSM, FSMState> creationFunction)
		{
			fsm.SetInterruptState(creationFunction.Invoke(fsm));
		}

		public void CancelFSMInterruptState()
		{
			fsm.SetInterruptState(null);
		}

		public bool LayerVisibleForCurrentExpertise(string layerName)
		{
			if (currentExpertiseIndex == -1)
				return false;
			foreach(var layer in SessionManager.Instance.MspGlobalData.expertise_definitions[currentExpertiseIndex].visible_layers)
				if (layer == layerName)
					return true;
			return false;
		}

		public bool LayerSelectedForCurrentExpertise(string layerName)
		{
			if (currentExpertiseIndex == -1)
				return false;
			foreach (var layer in SessionManager.Instance.MspGlobalData.expertise_definitions[currentExpertiseIndex].selected_layers)
				if (layer == layerName)
					return true;
			return false;
		}

		public double[] ConvertToGeoJSONCoordinate(double[] mspCoordinate)
		{
			Reproject.ReprojectPoints(mspCoordinate, new double[] { 1 }, mspCoordinateProjection, geoJSONCoordinateProjection, 0, 1);
			return mspCoordinate;
		}

		public double[] ConvertToMSPCoordinate(double[] geoJSONCoordinate)
		{
			Reproject.ReprojectPoints(geoJSONCoordinate, new double[] { 1 }, geoJSONCoordinateProjection, mspCoordinateProjection, 0, 1);
			return geoJSONCoordinate;
		}

		public static bool ControlKeyDown => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
	}
}