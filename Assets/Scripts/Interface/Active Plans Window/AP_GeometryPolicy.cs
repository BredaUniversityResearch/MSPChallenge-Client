using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MSP2050.Scripts
{
	public class AP_GeometryPolicy : MonoBehaviour
	{
		[SerializeField] private Button barButton = null;
		[SerializeField] private Toggle policyToggle = null;
		[SerializeField] private TextMeshProUGUI nameText = null;

		public AP_GeometryTool.GeometryPolicyChangeCallback changedCallback;

		private EntityPropertyMetaData parameter;

		void Start()
		{
			//TODO: toggle value change
			//TODO: button open window
		}

		public void SetValue(Dictionary<Entity,string> values)
		{
			//TODO
		}

		public void SetInteractable(bool value, bool reset = true)
		{
			barButton.interactable = value;
			policyToggle.interactable = value;

			if (reset)
			{
				policyToggle.isOn = false;
			}
		}

		void ValueChanged(string newvalue)
		{
			if (parameterChangedCallback != null)
				parameterChangedCallback(parameter, newvalue);
		}

		public void SetToPolicy(EntityPropertyMetaData parameter)
		{
			//TODO

			this.parameter = parameter;
			valueInput.contentType = parameter.ContentType;
			nameText.text = parameter.DisplayName;
			unit.text = parameter.Unit;
			unit.gameObject.SetActive(!string.IsNullOrEmpty(parameter.Unit));
			SetInteractable(false);
			if (!string.IsNullOrEmpty(parameter.SpriteName))
				icon.sprite = Resources.Load<Sprite>(parameter.SpriteName);
			else
				icon.gameObject.SetActive(false);
		}
	}
}
