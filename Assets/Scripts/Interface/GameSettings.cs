﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class GameSettings : MonoBehaviour
	{
		private static GameSettings singleton;
		public static GameSettings Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<GameSettings>();
				return singleton;
			}
		}

		// Audio
		private AudioMixer audioMixer;
		private float masterVolume;
		private float sfxVolume;

		//Graphics
		public float UIScale { get; private set; }
		public int DisplayResolution { get; private set; }
		public bool Fullscreen { get; private set; }
		public int GraphicsSettings { get; private set; }

		[HideInInspector] public List<Vector2> Resolutions = new List<Vector2>();

		void Awake()
		{
			if (singleton != null && singleton != this)
			{
				Destroy(this);
				return;
			}
			else
			{
				singleton = this;
				DontDestroyOnLoad(gameObject);
			}
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
		
		private void LoadAllSettings()
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

				UIScale = Mathf.Round(Mathf.Clamp(PlayerPrefs.GetFloat("UIScale", 3f), 0f, 7f));
			}
		}

		public void ApplyCurrentSettings(bool save = true)
		{
			SetResolution(DisplayResolution, false, false);
			SetFullscreen(Fullscreen, false);
			SetUIScale(UIScale, false, false, true);
			SetMasterVolume(masterVolume, false);
			SetSFXVolume(sfxVolume, false);
			if (save)
				SavePlayerPrefs();
		}

		public void SavePlayerPrefs()
		{
			PlayerPrefs.SetFloat("MasterVolume", masterVolume);
			PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
			PlayerPrefs.SetFloat("UIScale", UIScale);
			PlayerPrefs.SetInt("GraphicsSettings", GraphicsSettings);
			PlayerPrefs.SetInt("DisplayResolution", DisplayResolution);
			PlayerPrefs.SetInt("Fullscreen", Fullscreen ? 1 : 0);
		}

		public void SetAudioMixer(AudioMixer audioMixer)
		{
			this.audioMixer = audioMixer;
			ApplyMasterVolume();
			ApplySFXVolume();
		}

		public float GetMasterVolume()
		{
			return masterVolume;
		}

		public void SetMasterVolume(float volume, bool save = true)
		{
			volume = Mathf.Clamp(volume, 0.001f, 1.0f);
			masterVolume = volume;
			ApplyMasterVolume();
			if(save)
				SavePlayerPrefs();
		}

		public void ApplyMasterVolume()
		{
			if (audioMixer != null)
			{
				audioMixer.SetFloat("MasterVolume", SliderToVolume(masterVolume));
			}
		}

		public float GetSFXVolume()
		{
			return sfxVolume;
		}

		public void SetSFXVolume(float volume, bool save = true)
		{
			volume = Mathf.Clamp(volume, 0.0f, 1.0f);
			sfxVolume = volume;
			ApplySFXVolume();
			if(save)
				SavePlayerPrefs();
		}

		public void ApplySFXVolume()
		{
			if (audioMixer != null)
			{
				audioMixer.SetFloat("SFXVolume", SliderToVolume(sfxVolume));
			}
		}

		private float SliderToVolume(float sliderValue)
		{
			return Mathf.Log(sliderValue) * 20f;
		}

		public void SetUIScale(float scale, bool updateGameScale = true, bool save = true, bool force = false)
		{
			if (scale != UIScale || force)
			{
				float oldScale = UIScale;
				UIScale = scale;

				if (InterfaceCanvas.Instance != null)
				{
					InterfaceCanvas.Instance.canvas.scaleFactor = (scale + 1f) / 4f;
					InterfaceCanvas.Instance.objectivesMonitor.thisGenericWindow.HandleResolutionOrScaleChange(oldScale, true);
					InterfaceCanvas.Instance.impactToolWindow.HandleResolutionOrScaleChange(oldScale, true);
					TutorialManager.Instance.HandleResolutionOrScaleChange();
					if(DashboardManager.Instance != null)
						DashboardManager.Instance.HandleResolutionOrScaleChange();
					InterfaceCanvas.Instance.menuBarImpactTool.toggle.isOn = false;
				}
				else
				{
					Canvas currentCanvas = GameObject.FindObjectOfType<Canvas>();
					if (currentCanvas != null)
						currentCanvas.scaleFactor = (scale+1f)/4f;
					RoundingManager.SetUIScale((int)scale);
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
		public void SetResolution(int level, bool updateGameScale = true, bool save = true)
		{
			if (!Fullscreen)
				return;

			if (Resolutions.Count == 0)
			{
				return;
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

			if (CameraManager.Instance != null)
			{
				CameraManager.Instance.UpdateBounds();
			}

			if (updateGameScale)
				UpdateFullGameScale();
		}

		private void UpdateFullGameScale()
		{
			if(CameraManager.Instance != null)
			{
				CameraManager.Instance.cameraZoom.UpdateUIScale();
				LayerManager.Instance.UpdateLayerScales(CameraManager.Instance.gameCamera);
			}
		}

		public void SetFullscreen(bool fullscreen, bool save = true)
		{
			if (Resolutions.Count == 0)
			{
				return; //Don't do this in batch mode.
			}

			Fullscreen = fullscreen;
			Screen.fullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
			if (save)
				SavePlayerPrefs();
		}
	}
}
