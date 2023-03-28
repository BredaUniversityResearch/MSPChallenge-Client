using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class TimeManagerControls : MonoBehaviour {
    
		public enum TimeControlButton { None, Play, Pause, Forward }
		TimeControlButton glowing = TimeControlButton.None;

		private bool interactable = true;
		public Button play, pause, forward, finishSetup;
		public GameObject buttonBlocker;

		public void Start()
		{
			play.onClick.AddListener(TimeManager.Instance.Play);
			pause.onClick.AddListener(TimeManager.Instance.Pause);
			forward.onClick.AddListener(TimeManager.Instance.Forward);  
			finishSetup.onClick.AddListener(TimeManager.Instance.FinishSetup);  
		      
		}

		public bool Interactable
		{
			get
			{
				return interactable;
			}
			set
			{
				buttonBlocker.SetActive(!interactable);
			}
		}

		public void SetGlowTo(TimeControlButton newGlow)
		{
			if (newGlow == glowing)
				return;
			SetGlowFor(glowing, false);
			glowing = newGlow;
			SetGlowFor(glowing, true);
		}

		private void SetGlowFor(TimeControlButton button, bool state)
		{
			switch (button)
			{
				case TimeControlButton.Play:
					play.interactable = !state;
					break;
				case TimeControlButton.Pause:
					pause.interactable = !state;
					break;
				case TimeControlButton.Forward:
					forward.interactable = !state;
					break;
				default:
					break;
			}
		}

		public void HideSetupFinishButton()
		{
			finishSetup.gameObject.SetActive(false);
			play.gameObject.SetActive(true);
			pause.gameObject.SetActive(true);
			forward.gameObject.SetActive(true);
		}
	}
}