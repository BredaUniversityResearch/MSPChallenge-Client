using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActiveLayer : MonoBehaviour {

		public TextMeshProUGUI layerName;
		public CustomToggle visibilityToggle;
		public CustomToggle expandToggle;
		public CustomToggle layerTextToggle;
		public CustomToggle pinToggle;//TODO: make work
		public CustomButton closeButton;
		public Transform contentLocation;
		
	
		[Header("Prefabs")]
		public GameObject entityTypeEntryPrefab;
		
		[HideInInspector] public AbstractLayer layerRepresenting;

		private void Start()
		{
			expandToggle.onValueChanged.AddListener((b) => SetExpanded(b));
		}

		public void ShowCloseButton(bool show)
		{
			closeButton.gameObject.SetActive(show);
		}

		public void SetLayerRepresenting(AbstractLayer layer, bool forceTextHidden)
		{
			layerRepresenting = layer;
			layerName.text = string.IsNullOrEmpty(layer.ShortName) ? layer.FileName : layer.ShortName;
			foreach (EntityType entityType in layerRepresenting.GetEntityTypesSortedByKey())
			{
				ActiveLayerEntityType mapKey = Instantiate(entityTypeEntryPrefab, contentLocation).GetComponent<ActiveLayerEntityType>();
				mapKey.SetContent(layerRepresenting, entityType);
			}
			if (layerRepresenting.textInfo == null)
			{
				layerTextToggle.gameObject.SetActive(false);
			}
			else
			{
				if (forceTextHidden && layer.LayerTextVisible)
					layer.LayerTextVisible = false;
				layerTextToggle.isOn = layer.LayerTextVisible;
				layerTextToggle.onValueChanged.AddListener((value) =>
				{
					layerRepresenting.LayerTextVisible = value;
					InterfaceCanvas.Instance.activeLayers.TextShowingChanged(value);
				});
			}
		}

		public void SetExpanded(bool value)
		{
			if (contentLocation.gameObject.activeInHierarchy == value)
				return;

			contentLocation.gameObject.SetActive(value);
			InterfaceCanvas.Instance.activeLayers.LayerExpansionChanged(value);
		}

		public void SetVisibilityLocked(bool value)
		{
			visibilityToggle.interactable = !value;
		}

		public void Destroy()
		{
			if (contentLocation.gameObject.activeInHierarchy)
			{
				InterfaceCanvas.Instance.activeLayers.LayerExpansionChanged(false);
				InterfaceCanvas.Instance.activeLayers.TextShowingChanged(false);
			}
			Destroy(gameObject);
		}
	}
}
