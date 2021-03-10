using System;
using UnityEngine;
using UnityEngine.UI;

public class GenericEntryIcon : GenericEntry {

	public Image iconImage;

	/// <summary>
	/// Set a property label by declaring type, name, sprite and a parameter value
	/// </summary>
	public void PropertyLabel<T>(string name, T param, Sprite icon, Color color)
	{
		obj = param;
		typeCode = Type.GetTypeCode(typeof(T));
		if (typeCode == TypeCode.Object)
		{
			type = typeof(T);
		}
		label.text = name;
		if (param == null)
			value.text = "";
		else
			value.text = param.ToString();
		iconImage.sprite = icon;
		iconImage.color = color;
	}
}
