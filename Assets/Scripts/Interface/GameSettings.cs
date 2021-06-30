using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;

public static class GameSettings
{
    // Audio
    private static AudioMixer audioMixer;
    private static float masterVolume;
    private static float sfxVolume;

    //Graphics
    public static float UIScale { get; private set; }
    public static int DisplayResolution { get; private set; }
    public static bool Fullscreen { get; private set; }
	public static int GraphicsSettings { get; private set; }

	public static List<Vector2> Resolutions = new List<Vector2>();

    static GameSettings()
	{
		for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            if (Resolutions.FindIndex(res => res.x == Screen.resolutions[i].width && res.y == Screen.resolutions[i].height) != -1)
                continue;

            float width = Screen.resolutions[i].width;
            float height = Screen.resolutions[i].height;

            if (width > 1000) // dont include tiny resolutions
            {
                Resolutions.Add(new Vector2(width, height));
            }
        }

        if (Resolutions.Count == 0)
            Debug.LogError("No suitable resolutions found for the current screen.");
        LoadAllSettings();
    }

	private static void LoadAllSettings()
	{
		masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
		sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

		GraphicsSettings = PlayerPrefs.GetInt("GraphicsSettings", QualitySettings.GetQualityLevel());
		Fullscreen = PlayerPrefs.GetInt("Fullscreen", Convert.ToInt32(Screen.fullScreen)) == 1;

		DisplayResolution = PlayerPrefs.GetInt("DisplayResolution", -1); // Get this from the set resolution from the start

		if (Resolutions.Count > 0)
		{
			if (DisplayResolution >= Resolutions.Count || DisplayResolution == -1)
			{
				//No valid resolution set, so select current or biggest
				for(int i = 0; i < Resolutions.Count; i++)
				{
					DisplayResolution = i;
					if (Resolutions[i].x == Screen.currentResolution.width && Resolutions[i].y == Screen.currentResolution.height)
						break;
				}
			}

			UIScale = Mathf.Min(PlayerPrefs.GetFloat("UIScale", Screen.dpi / 100f), GetMaxUIScaleForWidth(Resolutions[DisplayResolution].x));
		}
	}

	public static void ApplyCurrentSettings(bool save = true)
	{
		SetQualityLevel(GraphicsSettings, false);
		SetResolution(DisplayResolution, false, false);
		SetFullscreen(Fullscreen, false);
		SetUIScale(UIScale, false, false, true);
		SetMasterVolume(masterVolume, false);
		SetSFXVolume(sfxVolume, false);
		if (save)
			SavePlayerPrefs();
	}

	public static void SavePlayerPrefs()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        PlayerPrefs.SetFloat("UIScale", UIScale);
        PlayerPrefs.SetInt("GraphicsSettings", GraphicsSettings);
        PlayerPrefs.SetInt("DisplayResolution", DisplayResolution);
        PlayerPrefs.SetInt("Fullscreen", Fullscreen ? 1 : 0);
    }

	public static void SetAudioMixer(AudioMixer audioMixer)
	{
		GameSettings.audioMixer = audioMixer;
		ApplyMasterVolume();
		ApplySFXVolume();
	}

	public static float GetMasterVolume()
    {
        return masterVolume;
    }

    public static void SetMasterVolume(float volume, bool save = true)
    {
        volume = Mathf.Clamp(volume, 0.001f, 1.0f);
        masterVolume = volume;
        ApplyMasterVolume();
		if(save)
			SavePlayerPrefs();
    }

    public static void ApplyMasterVolume()
    {
		if (audioMixer != null)
		{
			audioMixer.SetFloat("MasterVolume", SliderToVolume(masterVolume));
		}
	}

    public static float GetSFXVolume()
    {
        return sfxVolume;
    }

    public static void SetSFXVolume(float volume, bool save = true)
    {
        volume = Mathf.Clamp(volume, 0.0f, 1.0f);
        sfxVolume = volume;
        ApplySFXVolume();
		if(save)
			SavePlayerPrefs();
    }

    public static void ApplySFXVolume()
    {
		if (audioMixer != null)
		{
			audioMixer.SetFloat("SFXVolume", SliderToVolume(sfxVolume));
		}
	}

    private static float SliderToVolume(float sliderValue)
    {
        //return sliderValue * 100 - 80;
        return Mathf.Log(sliderValue) * 20f;
    }

    public static void SetUIScale(float scale, bool updateGameScale = true, bool save = true, bool force = false)
    {
		if (scale != UIScale || force)
		{
			float oldScale = UIScale;
			UIScale = scale;

			if (InterfaceCanvas.Instance != null)
			{
				InterfaceCanvas.Instance.canvas.scaleFactor = scale;
				//InterfaceCanvas.instance.activeLayers.HandleResolutionOrScaleChange();
				InterfaceCanvas.Instance.plansMonitor.thisGenericWindow.HandleResolutionOrScaleChange(oldScale, true);
				InterfaceCanvas.Instance.objectivesMonitor.thisGenericWindow.HandleResolutionOrScaleChange(oldScale, true);
			}
			else
			{
				Canvas currentCanvas = GameObject.FindObjectOfType<Canvas>();
				if (currentCanvas != null)
					currentCanvas.scaleFactor = scale;
			}
			if(save)
				SavePlayerPrefs();

            if (updateGameScale)
                UpdateFullGameScale();
		}
	}

    /// <summary>
    /// Returns the size of the new resolution
    /// </summary>
	public static Vector2 SetResolution(int level, bool updateGameScale = true, bool save = true)
	{
		if (Resolutions.Count == 0)
		{
			return Vector2.one;
		}

		if (level < 0 || level > Resolutions.Count)
		{
			Debug.Log("Invalid Resolution Setting, defaulting to 0");
			level = 0;
		}

		DisplayResolution = level;

		// Don't apply resolution in editor or when running in headless mode.
		if (!Application.isEditor && Resolutions.Count > DisplayResolution)
		{
			int width = (int) Resolutions[DisplayResolution].x;
			int height = (int) Resolutions[DisplayResolution].y;
			Screen.SetResolution(width, height, Fullscreen);
		}
		if(save)
			SavePlayerPrefs();
		if (InterfaceCanvas.Instance != null)
		{
			//InterfaceCanvas.instance.activeLayers.HandleResolutionOrScaleChange();
			//InterfaceCanvas.instance.plansMonitor.plansMinMax.HandleResolutionOrScaleChange();
		}

		if (CameraManager.Instance != null)
		{
			CameraManager.Instance.UpdateBounds();
		}

        if (updateGameScale)
            UpdateFullGameScale();

        return Resolutions[DisplayResolution];
	}

    private static void UpdateFullGameScale()
    {
		if(CameraManager.Instance != null)
		{
			CameraManager.Instance.cameraZoom.UpdateUIScale();
			LayerManager.UpdateLayerScales(CameraManager.Instance.gameCamera);
		}
    }

    public static void SetFullscreen(bool fullscreen, bool save = true)
    {
		if (Resolutions.Count == 0)
		{
			return; //Don't do this in batch mode.
		}

		Fullscreen = fullscreen;
        //if (fullscreen)
        //{
        //    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        //}
        //else
        //{
        //    Screen.fullScreenMode = FullScreenMode.Windowed;
        //}
        Screen.fullScreen = fullscreen;
		if(save)
			SavePlayerPrefs();
    }

    public static void SetQualityLevel(int level, bool save = true)
    {
        if (level < 0 || level > 3)
        {
            Debug.Log("Invalid Quality Setting, defaulting to 0");
            level = 0;
        }

        GraphicsSettings = level;
        QualitySettings.SetQualityLevel(GraphicsSettings);
		if(save)
			SavePlayerPrefs();
	}

    public static float GetMaxUIScaleForWidth(float screenWidth)
    {
        return Mathf.Max(0, (screenWidth - 900f)) / 1000f + 1f;
    }
}
