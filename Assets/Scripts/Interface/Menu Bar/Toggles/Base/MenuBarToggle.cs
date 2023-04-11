using Sirenix.OdinInspector;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class MenuBarToggle : MonoBehaviour
	{
		public enum Selection { Logo, Layers, PlansList, ObjectivesMonitor, ImpactTool, ActiveLayers, GameMenu, CreatePlan, Notifications, MapTools, Other };

		[Header("Connects to the correct toggle")]
		public Selection connectTo;

		[ShowIf("connectTo", Selection.Other)]
		public GameObject otherWindow;

		public CustomToggle toggle;

		private void Start()
		{
			Initialise();
		}

		protected void Initialise()
		{
			toggle = GetComponent<CustomToggle>();

			switch (connectTo) 
			{
				case Selection.Logo:
					toggle.isOn = InterfaceCanvas.Instance.teamWindow.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.teamWindow.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.Layers:
					toggle.isOn = InterfaceCanvas.Instance.layerInterface.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.layerInterface.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.PlansList:
					toggle.isOn = InterfaceCanvas.Instance.plansList.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.plansList.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.ObjectivesMonitor:
					if (InterfaceCanvas.Instance.objectivesMonitor) 
					{
						toggle.isOn = false; // Init
						toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.objectivesMonitor.gameObject.SetActive(toggle.isOn));
					}
					break;
				case Selection.ImpactTool:
					toggle.isOn = InterfaceCanvas.Instance.impactToolWindow.gameObject.activeSelf;
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.impactToolWindow.gameObject.SetActive(toggle.isOn));
					toggle.gameObject.SetActive(SessionManager.Instance.MspGlobalData.dependencies != null);
					break;
				case Selection.ActiveLayers:
					toggle.isOn = InterfaceCanvas.Instance.activeLayers.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.activeLayers.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.GameMenu:
					toggle.isOn = InterfaceCanvas.Instance.gameMenu.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.gameMenu.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.CreatePlan:
					toggle.onValueChanged.AddListener((b) => PlanManager.Instance.BeginPlanCreation());
					break;
				case Selection.Notifications:
					toggle.isOn = InterfaceCanvas.Instance.notificationWindow.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.notificationWindow.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.MapTools:
					toggle.isOn = InterfaceCanvas.Instance.mapToolsWindow.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.mapToolsWindow.SetActive(toggle.isOn));
					break;
				case Selection.Other:
					toggle.isOn = otherWindow.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => otherWindow.SetActive(toggle.isOn));
					break;
			}
		}

		//Called from close buttons of associated windows
		public void ToggleValue()
		{
			toggle.isOn = !toggle.isOn;
		}
	}
}