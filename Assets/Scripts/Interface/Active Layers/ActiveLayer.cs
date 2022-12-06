using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActiveLayer : MonoBehaviour {

		[SerializeField] TextMeshProUGUI m_layerName;
		[SerializeField] CustomToggle m_visibilityToggle;
		[SerializeField] CustomToggle m_expandToggle;
		[SerializeField] CustomToggle m_layerTextToggle;
		[SerializeField] CustomToggle m_pinToggle;
		[SerializeField] CustomButton m_closeButton;
	
		[Header("Prefabs")]
		[SerializeField] GameObject m_entityTypeEntryPrefab;
		[SerializeField] Transform m_contentLocation;
		
		AbstractLayer m_layerRepresenting;
		bool m_ignoreToggleCallback;

		private void Start()
		{
			m_expandToggle.onValueChanged.AddListener((b) => SetExpanded(b));
			m_pinToggle.onValueChanged.AddListener(OnPinToggleChanged);
			m_visibilityToggle.onValueChanged.AddListener(OnVisibilityToggleChanged);
			m_closeButton.onClick.AddListener(OnCloseButtonPressed);
		}

		public void Destroy()
		{
			if (m_contentLocation.gameObject.activeInHierarchy)
			{
				InterfaceCanvas.Instance.activeLayers.LayerExpansionChanged(false);
				InterfaceCanvas.Instance.activeLayers.TextShowingChanged(false);
			}
			LayerManager.Instance.m_onLayerVisibilityLockChanged -= OnLayerVisibilityLockChanged;
			Destroy(gameObject);
		}

		public void SetLayerRepresenting(AbstractLayer a_layer, bool a_forceTextHidden, bool a_pinnedInvisible = false)
		{
			m_layerRepresenting = a_layer;
			m_visibilityToggle.isOn = !a_pinnedInvisible;
			m_pinToggle.isOn = a_pinnedInvisible;
			m_layerName.text = string.IsNullOrEmpty(a_layer.ShortName) ? a_layer.FileName : a_layer.ShortName;
			LayerManager.Instance.m_onLayerVisibilityLockChanged += OnLayerVisibilityLockChanged;
			foreach (EntityType entityType in m_layerRepresenting.GetEntityTypesSortedByKey())
			{
				ActiveLayerEntityType mapKey = Instantiate(m_entityTypeEntryPrefab, m_contentLocation).GetComponent<ActiveLayerEntityType>();
				mapKey.SetContent(m_layerRepresenting, entityType);
			}
			if (m_layerRepresenting.textInfo == null)
			{
				m_layerTextToggle.gameObject.SetActive(false);
			}
			else
			{
				if (a_forceTextHidden && a_layer.LayerTextVisible)
					a_layer.LayerTextVisible = false;
				m_layerTextToggle.isOn = a_layer.LayerTextVisible;
				m_layerTextToggle.onValueChanged.AddListener((value) =>
				{
					m_layerRepresenting.LayerTextVisible = value;
					InterfaceCanvas.Instance.activeLayers.TextShowingChanged(value);
				});
			}
		}

		void OnPinToggleChanged(bool a_value)
		{
			m_visibilityToggle.gameObject.SetActive(a_value);
			m_closeButton.gameObject.SetActive(!a_value);
		}

		void OnVisibilityToggleChanged(bool a_value)
		{
			if (m_ignoreToggleCallback)
				return;

			m_ignoreToggleCallback = true;
			if (a_value)
			{
				LayerManager.Instance.ShowLayer(m_layerRepresenting);
			}
			else
			{
				LayerManager.Instance.HideLayer(m_layerRepresenting);
			}
			m_ignoreToggleCallback = false;
		}

		void OnCloseButtonPressed()
		{
			if (PlanManager.Instance.planViewing == null || !PlanManager.Instance.planViewing.IsLayerpartOfPlan(m_layerRepresenting))
			{
				InterfaceCanvas.Instance.activeLayers.RemoveLayer(m_layerRepresenting);
				LayerManager.Instance.HideLayer(m_layerRepresenting);
			}
		}

		public void SetExpanded(bool a_value)
		{
			if (m_contentLocation.gameObject.activeInHierarchy == a_value)
				return;

			m_contentLocation.gameObject.SetActive(a_value);
			InterfaceCanvas.Instance.activeLayers.LayerExpansionChanged(a_value);
		}

		public void SetTextActive(bool a_active)
		{
			m_layerTextToggle.isOn = a_active;
		}

		public void OnLayerVisibilityChanged(bool a_visible)
		{
			if (m_ignoreToggleCallback)
				return;

			m_ignoreToggleCallback = true;
			if (m_pinToggle.isOn)
			{
				m_visibilityToggle.isOn = a_visible;
			}
			else if (!a_visible)
			{
				InterfaceCanvas.Instance.activeLayers.RemoveLayer(m_layerRepresenting);
			}
			m_ignoreToggleCallback = false;
		}

		void OnLayerVisibilityLockChanged(AbstractLayer a_layer, bool a_locked)
		{
			if (a_layer == m_layerRepresenting)
			{
				m_visibilityToggle.interactable = !a_locked;
				m_closeButton.interactable = !a_locked;
			}
		}
	}
}
