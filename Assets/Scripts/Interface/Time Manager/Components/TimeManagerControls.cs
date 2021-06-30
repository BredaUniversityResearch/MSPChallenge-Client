using UnityEngine;
using UnityEngine.UI;

public class TimeManagerControls : MonoBehaviour {
    
    public enum TimeControlButton { None, Play, Pause, Forward }
    TimeControlButton glowing = TimeControlButton.None;

    private bool interactable = true;
    public Button play, pause, forward, finishSetup;
    public GameObject buttonBlocker;

    public void Start()
    {
        play.onClick.AddListener(TimeManager.instance.Play);
        pause.onClick.AddListener(TimeManager.instance.Pause);
        forward.onClick.AddListener(TimeManager.instance.Forward);  
        finishSetup.onClick.AddListener(TimeManager.instance.FinishSetup);  
		      
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