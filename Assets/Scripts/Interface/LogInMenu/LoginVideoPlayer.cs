using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.Video;

namespace MSP2050.Scripts
{
	public class LoginVideoPlayer : MonoBehaviour
	{
		[Header("Generic")]
		[SerializeField] private VideoPlayer m_videoPlayer;
		[SerializeField] private RenderTexture m_rendertexture;
		[SerializeField] private VideoClip[] m_videos;
		[SerializeField] private Sprite[] m_videoThumbnails;

		[Header("Intro carousel")]
		[SerializeField] private Button m_nextButton;
		[SerializeField] private Button m_previousButton;
		[SerializeField] private Image m_leftImage, m_rightImage;
		[SerializeField] private Button m_playButton;
		[SerializeField] private Button m_pauseButton;
		[SerializeField] private Button m_fullScreenButton;
		[SerializeField] private RawImage m_videoDisplayImage;

		[Header("Fullscreen player")]
		[SerializeField] private GameObject m_fsContainer;
		[SerializeField] private GameObject m_fsControlsContainer;
		[SerializeField] private float m_fsControlDisplayThreshold;
		[SerializeField] private Button m_fsPlayButton;
		[SerializeField] private Button m_fsPauseButton;
		[SerializeField] private Button m_fsVideoButton;
		[SerializeField] private Button m_fsMinimizeButton;
		[SerializeField] private Slider m_fsTimeSlider;

		private int m_currentVideoIndex;
		private bool m_ignoreSliderCallback = false;
		private Vector2 m_prevMousePosition;
		private float m_fsControlsVisibilityTime = -1f;

		void Start()
		{
			m_nextButton.onClick.AddListener(OnNextVideo);
			m_previousButton.onClick.AddListener(OnPreviousVideo);
			m_rightImage.GetComponent<Button>().onClick.AddListener(OnNextVideo);
			m_leftImage.GetComponent<Button>().onClick.AddListener(OnPreviousVideo);
			m_playButton.onClick.AddListener(OnPlay);
			m_pauseButton.onClick.AddListener(OnPause);
			m_fsPlayButton.onClick.AddListener(OnPlay);
			m_fsPauseButton.onClick.AddListener(OnPause);
			m_fsVideoButton.onClick.AddListener(OnTogglePlay);
			m_fullScreenButton.onClick.AddListener(OnMaximize);
			m_fsMinimizeButton.onClick.AddListener(OnMinimize);
			m_fsTimeSlider.onValueChanged.AddListener(OnTimeSliderChange);
			SetVideoIndex(0);
		}

		void Update()
		{
			if (m_videoPlayer.isPlaying)
			{
				m_ignoreSliderCallback = true;
				m_fsTimeSlider.value = (float)(m_videoPlayer.time / m_videoPlayer.clip.length);
				m_ignoreSliderCallback = false;
			}
			if (Input.GetKeyDown(KeyCode.Space))
			{
				if(m_videoPlayer.isPlaying)
					OnPause();
				else
					OnPlay();
			}
			if (m_fsContainer.activeSelf)
			{
				if(Input.GetKeyDown(KeyCode.Escape))
					OnMinimize();
				m_fsControlsVisibilityTime -= Time.deltaTime;
				if (Vector2.Distance(Input.mousePosition, m_prevMousePosition) > m_fsControlDisplayThreshold)
				{
					m_fsControlsVisibilityTime = 2f;
					m_fsControlsContainer.SetActive(true);
				}
				else if (m_fsControlsVisibilityTime < 0f)
				{
					m_fsControlsContainer.SetActive(false);
				}
				m_prevMousePosition = Input.mousePosition;
			}
		}

		void OnNextVideo()
		{
			if (m_videos.Length - 1 == m_currentVideoIndex)
				SetVideoIndex(0);
			else
				SetVideoIndex(m_currentVideoIndex + 1);
		}

		void OnPreviousVideo()
		{
			if(0 == m_currentVideoIndex)
				SetVideoIndex(m_videos.Length - 1);
			else
				SetVideoIndex(m_currentVideoIndex-1);
		}

		void SetVideoIndex(int a_newIndex)
		{
			m_currentVideoIndex = a_newIndex;

			OnPause();
			m_videoPlayer.Stop();
			m_videoPlayer.clip = m_videos[m_currentVideoIndex];
			m_leftImage.sprite = m_currentVideoIndex == 0 ? m_videoThumbnails[m_videoThumbnails.Length - 1] : m_videoThumbnails[m_currentVideoIndex - 1];
			m_rightImage.sprite = m_currentVideoIndex == m_videoThumbnails.Length - 1 ? m_videoThumbnails[0] : m_videoThumbnails[m_currentVideoIndex + 1];
			m_videoDisplayImage.texture = m_videoThumbnails[m_currentVideoIndex].texture;
		}

		void OnTogglePlay()
		{
			if(m_videoPlayer.isPlaying)
				OnPause();
			else
				OnPlay();
		}

		void OnPlay()
		{
			m_videoPlayer.Play();
			m_videoDisplayImage.texture = m_rendertexture;
			m_fsPlayButton.gameObject.SetActive(false);
			m_playButton.gameObject.SetActive(false);
			m_fsPauseButton.gameObject.SetActive(true);
			m_pauseButton.gameObject.SetActive(true);
		}

		void OnPause()
		{
			m_videoPlayer.Pause();
			m_fsPlayButton.gameObject.SetActive(true);
			m_playButton.gameObject.SetActive(true);
			m_fsPauseButton.gameObject.SetActive(false);
			m_pauseButton.gameObject.SetActive(false);
		}

		void OnMinimize()
		{
			m_fsContainer.SetActive(false);
		}

		void OnMaximize()
		{
			m_fsContainer.SetActive(true);
			m_prevMousePosition = Input.mousePosition;
			m_fsControlsVisibilityTime = 2f;
		}

		void OnTimeSliderChange(float a_newValue)
		{
			if (m_ignoreSliderCallback)
				return;
			m_videoPlayer.time = m_videoPlayer.clip.length * a_newValue;
		}

		public void OnDisplaySettingsChanged()
		{

		}
	}
}
