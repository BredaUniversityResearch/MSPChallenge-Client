using UnityEngine;

namespace MSP2050.Scripts
{
	public class MenuBarToggle : MonoBehaviour
	{
		public enum Selection { Logo, Layers, PlansList, ObjectivesMonitor, ImpactTool, ActiveLayers, GameMenu, CreatePlan, Notifications, MapTools };

		[Header("Connects to the correct toggle")]
		public Selection connectTo;
	
		public CustomToggle toggle;

		void Start()
		{
			toggle = GetComponent<CustomToggle>();

			switch (connectTo) 
			{
				case Selection.Logo:
					SetRegionButtonCallback();                          
					break;
				case Selection.Layers:
					toggle.isOn = InterfaceCanvas.Instance.layerInterface.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.layerInterface.gameObject.SetActive(toggle.isOn));
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.layerInterface.DisableLayerSelect(toggle.isOn));
					break;
				case Selection.PlansList:
					toggle.isOn = InterfaceCanvas.Instance.plansList.gameObject.activeSelf; // Init
					toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.plansList.gameObject.SetActive(toggle.isOn));
					break;
				case Selection.ObjectivesMonitor:
					if (InterfaceCanvas.Instance.objectivesMonitor) 
					{
						toggle.isOn = false; // Init
						toggle.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.objectivesMonitor.SetWindowActive(toggle.isOn));
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
					toggle.onValueChanged.AddListener((b) => PlanManager.Instance.CreateNewPlanForEditing());
					break;
				case Selection.Notifications:
					//TODO
					break;
				case Selection.MapTools:
					//TODO
					break;
			}
		}

		//Called from close buttons of associated windows
		public void ToggleValue()
		{
			toggle.isOn = !toggle.isOn;
		}

		void SetRegionButtonCallback()
		{
			toggle.onValueChanged.AddListener((b) =>
			{
				float scale = InterfaceCanvas.Instance.canvas.scaleFactor;
				InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow(SessionManager.Instance.MspGlobalData.region_base_url + '/' + SessionManager.Instance.CurrentTeam.name);
			});
	
		}
	}
}