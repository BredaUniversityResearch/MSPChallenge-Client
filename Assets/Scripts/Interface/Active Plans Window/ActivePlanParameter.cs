using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ActivePlanParameter : MonoBehaviour {

	[SerializeField] private Image icon = null;
	[SerializeField] private TextMeshProUGUI nameText = null;
	[SerializeField] private InputField valueInput = null;
	[SerializeField] private TextMeshProUGUI unit = null;

	public ActivePlanWindow.ParameterChangeCallback parameterChangedCallback;
	private EntityPropertyMetaData parameter;
	
	void Start ()
	{
		valueInput.onEndEdit.AddListener(ValueChanged);
	}

	public void SetValue(string value)
	{
		valueInput.text = value;
	}

	public void SetInteractable(bool value, bool reset = true)
	{
		valueInput.interactable = value;

		if(reset)
		{
			valueInput.text = "";
		}
	}

	void ValueChanged(string newvalue)
	{
		if (parameterChangedCallback != null)
			parameterChangedCallback(parameter, newvalue);
	}

	public void SetToParameter(EntityPropertyMetaData parameter)
	{
		this.parameter = parameter;
        valueInput.contentType = parameter.ContentType;
        nameText.text = parameter.DisplayName;
		unit.text = parameter.Unit;
		SetInteractable(false);
		if (!string.IsNullOrEmpty(parameter.SpriteName))
			icon.sprite = Resources.Load<Sprite>(parameter.SpriteName);
		else
			icon.gameObject.SetActive(false);
	}
}
