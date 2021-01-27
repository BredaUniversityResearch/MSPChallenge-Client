using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Options : MonoBehaviour
{
	public GenericWindow thisGenericWindow;

	private float oldScale;
	private int oldDisplayResolution;
	private int oldGraphicsSettings;
	private bool oldFullscreen;

	private float oldMasterVolume;
	private float oldSoundEffects;

	// Graphics
	public CustomSlider uiScale;
	public CustomDropdown displayResolution;
	public CustomDropdown qualitySettings;
	public Toggle fullscreenToggle;

	// Audio
	public Slider masterVolume;
	public Slider soundEffects;

	// Other
	public Toggle developerModeToggle;
    public TextMeshProUGUI buildDateText;
	public TextMeshProUGUI buildRevisionText;
	public TextMeshProUGUI apiEndpointText;

    public Button cancel, accept;

	protected void Awake()
	{
		if (thisGenericWindow == null)
			thisGenericWindow = GetComponent<GenericWindow>();

		displayResolution.ClearOptions();
		cancel.onClick.AddListener(OnCancel);
		accept.onClick.AddListener(OnAccept);
	}

	protected void Start()
	{
		List<string> resNames = new List<string>();

		foreach (Vector2 res in GameSettings.Resolutions)
		{
			// format 16:10 - 1680 x 1050
			string name = "" + res.x + " x " + res.y;
			resNames.Add(name);
		}

		displayResolution.AddOptions(resNames);

		fullscreenToggle.isOn = GameSettings.Fullscreen;

		developerModeToggle.isOn = Main.IsDeveloper;
		uiScale.m_onRelease.AddListener(OnUIScaleSliderUp);

		SetBuildInformation();
		SetAPIEndpointInfo();
		SetOptions();
	}

	public void OnAccept()
	{
		this.gameObject.SetActive(false);
	}

	// Only apply this on pointer up
	public void OnUIScaleSliderUp()
	{
		GameSettings.SetUIScale(uiScale.value);
		InterfaceCanvas.Instance?.propertiesWindow.Close();
		UpdatePosition();
	}

	public void OnCancel()
	{
		GameSettings.SetUIScale(oldScale);
		GameSettings.SetQualityLevel(oldGraphicsSettings);
		GameSettings.SetResolution(oldDisplayResolution);
		GameSettings.SetMasterVolume(oldMasterVolume);
		GameSettings.SetSFXVolume(oldSoundEffects);
		GameSettings.SetFullscreen(oldFullscreen);
		UpdatePosition();

		this.gameObject.SetActive(false);
	}

	protected void OnEnable()
	{
		StartCoroutine(LateUpdatePosition());
		SetOptions();
	}

	private void SetBuildInformation()
	{
		ApplicationBuildIdentifier identifier = ApplicationBuildIdentifier.FindBuildIdentifier();
		if (identifier != null)
		{
			buildDateText.text = identifier.GetBuildTime();
			buildRevisionText.text = identifier.GetSvnRevisionNumber().ToString();
		}
	}

	private void SetAPIEndpointInfo()
	{
		apiEndpointText.text = Server.Url;
	}

	private void SetOptions()
	{
		oldScale = GameSettings.UIScale;
		oldGraphicsSettings = GameSettings.GraphicsSettings;
		oldMasterVolume = GameSettings.GetMasterVolume();
		oldSoundEffects = GameSettings.GetSFXVolume();
		oldFullscreen = GameSettings.Fullscreen;

		if (Application.isEditor) // This is to fix it for the editor, it only ever displays one resolution
			oldDisplayResolution = 0;
		else
			oldDisplayResolution = GameSettings.DisplayResolution;

		uiScale.value = oldScale;
        uiScale.maxValue = GameSettings.GetMaxUIScaleForWidth(Camera.main.pixelWidth);

        qualitySettings.value = oldGraphicsSettings;
		displayResolution.value = oldDisplayResolution;
		masterVolume.value = oldMasterVolume;
		soundEffects.value = oldSoundEffects;

		uiScale.onValueChanged.RemoveAllListeners();
		qualitySettings.onValueChanged.RemoveAllListeners();
		displayResolution.onValueChanged.RemoveAllListeners();
		masterVolume.onValueChanged.RemoveAllListeners();
		soundEffects.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        masterVolume.onValueChanged.RemoveAllListeners();

        //uiScale.slider.onValueChanged.AddListener((value) => { GameSettings.SetUIScale(value); });
        qualitySettings.onValueChanged.AddListener((value) => { GameSettings.SetQualityLevel(value); });
		displayResolution.onValueChanged.AddListener(OnResolutionChanged);
		fullscreenToggle.onValueChanged.AddListener(b => GameSettings.SetFullscreen(b));
		developerModeToggle.onValueChanged.AddListener((value) => { Main.IsDeveloper = value; });
		masterVolume.onValueChanged.AddListener((value) => { GameSettings.SetMasterVolume(value); playSoundEffect(AudioMain.VOLUME_TEST); });
		soundEffects.onValueChanged.AddListener((value) => { GameSettings.SetSFXVolume(value); playSoundEffect(AudioMain.VOLUME_TEST); });
	}

	private void OnResolutionChanged(int resolutionIndex)
	{
		Vector2 newResolution = GameSettings.SetResolution(resolutionIndex);
        float maxUIScale = GameSettings.GetMaxUIScaleForWidth(newResolution.x);
        uiScale.maxValue = maxUIScale;
        if (GameSettings.UIScale > maxUIScale)
            GameSettings.SetUIScale(maxUIScale);
		StartCoroutine(LateUpdatePosition());
		if(InterfaceCanvas.Instance != null)
			StartCoroutine(InterfaceCanvas.Instance.gameMenu.LateUpdatePosition());
	}

	private void playSoundEffect(string audioID)
	{
		AudioSource audioSource = AudioMain.GetAudioSource(audioID);
		if (audioSource != null && !audioSource.isPlaying)
		{
			audioSource.Play();
		}
	}

	private IEnumerator LateUpdatePosition()
	{
		yield return new WaitForFixedUpdate();
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		//Canvas.ForceUpdateCanvases();
		//thisGenericWindow.CenterWindow();
		////thisGenericWindow.SetPosition(new Vector2(Screen.width/2f, Screen.height/2f));
		//InterfaceCanvas.instance.gameMenu.thisGenericWindow.CenterWindow();
	}
}