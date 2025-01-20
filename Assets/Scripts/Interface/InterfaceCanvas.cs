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
		public LayerInterface layerInterface;
		public ActiveLayerWindow activeLayers;
		public ActivePlanWindow activePlanWindow;
		public PlansList plansList;
		public LoadingScreen loadingScreen;
		public UnLoadingScreen unLoadingScreen;
		public PropertiesWindow propertiesWindow;
		public LayerProbeWindow layerProbeWindow;
		public GameObject networkingBlocker;
		public GenericWindow impactToolWindow;
		public TeamWindow teamWindow;
		public HEBGraph.HEBGraph ImpactToolGraph;
		public NotificationWindow notificationWindow;
		public GameObject mapToolsWindow;

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
		public MenuBarToggle menuBarNotifications;
		public MenuBarToggle menuBarMapTools;

		[Header("KPI")]
		public  KPIOtherValueArea KPIEcologyGroups;

		[Header("Objectives")]
		public ObjectivesMonitor objectivesMonitor;

		[Header("LineMaterials")]
		public Material[] lineMaterials;
		public Sprite[] activeLayerLineSprites;

		[Header("Colours")]
		public ColourAsset accentColour;
		public ColourAsset regionColour;

		private Dictionary<string, Button> buttonUIReferences = new Dictionary<string, Button>();
		private Dictionary<string, Toggle> toggleUIReferences = new Dictionary<string, Toggle>();
		private Dictionary<string, GameObject> genericUIReferences = new Dictionary<string, GameObject>();
		private Dictionary<string, HashSet<GameObject>> tagUIReferences = new Dictionary<string, HashSet<GameObject>>();
		public event Action<string, string[]> interactionEvent;
		public event Action<string, GameObject> uiReferenceNameRegisteredEvent;
		public event Action<string[], GameObject> uiReferenceTagsRegisteredEvent;

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

		public void SetRegion(MspGlobalData globalData)
		{
			gameMenu.SetRegion(globalData);
			regionColour.SetValue(globalData.edition_colour);
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

		public void TriggerInteractionCallback(string name, string[] tags)
		{
			interactionEvent?.Invoke(name, tags);
		}

		public void RegisterUIReference(string name, Button button)
		{
			buttonUIReferences[name] = button;
			uiReferenceNameRegisteredEvent?.Invoke(name, button.gameObject);
		}

		public void RegisterUIReference(string name, Toggle toggle)
		{
			toggleUIReferences[name] = toggle;
			uiReferenceNameRegisteredEvent?.Invoke(name, toggle.gameObject);
		}

		public void RegisterUIReference(string name, GameObject ui)
		{
			genericUIReferences[name] = ui;
			uiReferenceNameRegisteredEvent?.Invoke(name, ui.gameObject);
		}

		public void RegisterUITagsReference(string[] tags, GameObject ui)
		{
			foreach (string tag in tags)
			{
				if (tagUIReferences.TryGetValue(tag, out var list))
					list.Add(ui);
				else
					tagUIReferences.Add(tag, new HashSet<GameObject>() { ui });
			}
			uiReferenceTagsRegisteredEvent?.Invoke(tags, ui.gameObject);
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

		public void UnregisterUITagsReference(string[] tags, GameObject ui)
		{
			foreach (string tag in tags)
			{
				if (tagUIReferences.TryGetValue(tag, out var list))
				{
					if (list.Count == 0)
						tagUIReferences.Remove(tag);
					else
						list.Remove(ui);
				}
			}
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

		public HashSet<GameObject> GetUIWithTag(string tag)
		{
			if (tagUIReferences.TryGetValue(tag, out var result))
				return result;
			return null;
		}
	}
}