using ColourPalette;
using System.Security.Policy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class ActivePlanLayerType : MonoBehaviour {

		public Toggle toggle;
		public TextMeshProUGUI typeName;
		public Button infoButton;

		//Should this toggle be disabled if it's not selected
		private bool disabledIfNotSelected;

		public void SetToType(EntityType type, bool addAvailabilityDate = false)
		{
			if(addAvailabilityDate)
				typeName.text = $"{type.Name} ({Util.MonthToText(type.availabilityDate, true)})";
			else
				typeName.text = type.Name;

			if (!string.IsNullOrEmpty(type.media))
			{
				infoButton.onClick.AddListener(() =>
				{
					Application.OpenURL(MediaUrl.Parse(type.media));
				});
			}
			else
				infoButton.gameObject.SetActive(false);

			toggle.onValueChanged.AddListener(SetInteractabilityForState);
		}

		public bool DisabledIfNotSelected
		{
			set
			{
				disabledIfNotSelected = value;
				if (!toggle.isOn)
					toggle.interactable = !disabledIfNotSelected;
			}
		}

		void SetInteractabilityForState(bool value)
		{
			if (!value)
			{
				if (disabledIfNotSelected)
					toggle.interactable = false;
			}
			else if (!toggle.interactable)
				toggle.interactable = true;
		}

		public void SetToMultiple()
		{
			typeName.text  = "Multiple different";
			infoButton.gameObject.SetActive(false);
		}
	}
}
