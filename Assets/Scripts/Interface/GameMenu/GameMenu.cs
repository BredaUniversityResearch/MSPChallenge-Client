using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class GameMenu : MonoBehaviour
	{
		public GenericWindow thisGenericWindow;

		public DynamicLogo logo;
		public TextMeshProUGUI editionText;

		public Button continueGame, options, credits, exit, tutorial, serverLogin;

	
		void Awake()
		{
			if (thisGenericWindow == null)
				thisGenericWindow = GetComponent<GenericWindow>();
		}

		void Start()
		{
			continueGame.onClick.AddListener(delegate () { gameObject.SetActive(false); });
			credits.onClick.AddListener(delegate () 
			{
				InterfaceCanvas.Instance.webViewWindow.CreateWebViewWindow("https://community.mspchallenge.info/wiki/Credits");
				thisGenericWindow.Hide();
			});
			options.onClick.AddListener(delegate () { InterfaceCanvas.Instance.options.gameObject.SetActive(true); });
			tutorial.onClick.AddListener(() => 
			{
				gameObject.SetActive(false);
				TutorialManager.Instance.StartTutorial(Resources.Load<TutorialData>("MainTutorialData"));
			});
			exit.onClick.AddListener(() => 
			{
				Main.QuitGame();
			});
			serverLogin.onClick.AddListener(() =>
			{
				SceneManager.LoadScene(0);
			});
		}

		void OnEnable()
		{
			thisGenericWindow.CreateModalBackground();
		}

		void OnDisable()
		{
			InterfaceCanvas.Instance.menuBarGameMenu.toggle.isOn = false;
			thisGenericWindow.DestroyModalWindow();
		}

		public void SetRegion(MspGlobalData globalData)
		{
			logo.SetContent(globalData.edition_colour, globalData.edition_letter);
			editionText.text = globalData.edition_name;
		}
	
		public IEnumerator LateUpdatePosition()
		{
			yield return new WaitForFixedUpdate();
			UpdatePosition();
		}

		private void UpdatePosition()
		{
			Canvas.ForceUpdateCanvases();
			thisGenericWindow.CenterWindow();
		}
	}
}