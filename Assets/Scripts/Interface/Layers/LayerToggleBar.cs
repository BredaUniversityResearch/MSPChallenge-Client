﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class LayerToggleBar : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle;
		[SerializeField] TextMeshProUGUI m_name;
		[SerializeField] AddTooltip m_tooltip;
		[SerializeField] Button m_infoButton;

		AbstractLayer m_layer;
		bool m_ignoreToggleCallback;

		private void Start()
		{
			m_toggle.onValueChanged.AddListener(OnToggleChanged);
			LayerManager.Instance.m_onLayerVisibilityChanged += OnLayerVisibilityChanged;
			LayerManager.Instance.m_onLayerVisibilityLockChanged += OnLayerVisibilityLockChanged;
			m_infoButton.onClick.AddListener(OnInfoButtonClicked);
		}

		public void SetContent(AbstractLayer a_layer)
		{
			m_layer = a_layer;
			m_name.text = a_layer.ShortName;
			gameObject.SetActive(true);

			m_toggle.interactable = !LayerManager.Instance.IsLayerVisibilityLocked(a_layer);
			m_toggle.isOn = LayerManager.Instance.LayerIsVisible(a_layer);
			m_tooltip.SetText(a_layer.Tooltip);
			m_infoButton.gameObject.SetActive(!string.IsNullOrEmpty(m_layer.Media));
		}

		void OnToggleChanged(bool a_value)
		{
			if (m_ignoreToggleCallback)
				return;

			m_ignoreToggleCallback = true;
			if (a_value)
				LayerManager.Instance.ShowLayer(m_layer);
			else
				LayerManager.Instance.HideLayer(m_layer);
			m_ignoreToggleCallback = false;
		}

		void OnLayerVisibilityChanged(AbstractLayer a_layer, bool a_visible)
		{
			if (!gameObject.activeSelf || a_layer != m_layer || m_ignoreToggleCallback)
				return;

			m_ignoreToggleCallback = true;
			m_toggle.isOn = a_visible;
			m_ignoreToggleCallback = false;
		}

		void OnLayerVisibilityLockChanged(AbstractLayer a_layer, bool a_locked)
		{
			if (a_layer == m_layer)
			{
				m_toggle.interactable = !a_locked;
			}
		}

		void OnInfoButtonClicked()
		{
			Application.OpenURL(MediaUrl.Parse(m_layer.Media));
		}
	}
}
