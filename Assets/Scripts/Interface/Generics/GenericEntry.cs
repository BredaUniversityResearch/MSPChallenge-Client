using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using TMPro;

public class GenericEntry : MonoBehaviour
{
    //[HideInInspector]
    public TextMeshProUGUI label;
    //[HideInInspector]
    public TextMeshProUGUI value;
    //[HideInInspector]
    public Button valueButton;
    [HideInInspector]
    public RawImage rawImage;
    [HideInInspector]
    public TypeCode typeCode;
    [HideInInspector]
    public Type type = null;
    [HideInInspector]
    public object obj;

    public delegate void ButtonDelegate();
    public ButtonDelegate ConfirmButtonDelegate = null;

    /// <summary>
    /// Set a property label and Texture 
    /// </summary>
    public void PropertyImage<T>(string name, T param)
    {
        valueButton.gameObject.SetActive(false);
        obj = param;
        typeCode = Type.GetTypeCode(typeof(T));
        if (typeCode == TypeCode.Object)
        {
            type = typeof(T);
        }
        if(type == typeof(Texture) || type == typeof(RenderTexture))
        {
            rawImage = gameObject.AddComponent<RawImage>();
            Texture tTex = (Texture)obj;
            rawImage.texture = tTex;
            rawImage.uvRect = new Rect(rawImage.uvRect.x, rawImage.uvRect.y, rawImage.uvRect.height, rawImage.uvRect.height);
        }
        label.text = name;
    }

    /// <summary>
    /// Set a property label by declaring type, name, and a parameter value
    /// </summary>
    public void PropertyLabel<T>(string name, T param)
    {
        obj = param;
        typeCode = Type.GetTypeCode(typeof(T));
        if (typeCode == TypeCode.Object)
        {
            type = typeof(T);
        }
        label.text = name;
        value.text = param.ToString();
    }

	public void PropertyLabel<T>(string name, T param, UnityAction callBack)
	{
		obj = param;
		typeCode = Type.GetTypeCode(typeof(T));
		if (typeCode == TypeCode.Object)
		{
			type = typeof(T);
		}
		label.text = name;
		value.text = param.ToString();
		valueButton.onClick.AddListener(callBack);
	}

    /// <summary>
    /// Hide the content
    /// </summary>
    public void Hide(bool toggle)
    {
        label.gameObject.SetActive(toggle);
        value.gameObject.SetActive(toggle);
    }

    public void InvokeConfirmButtonDelegate()
    {
        if (ConfirmButtonDelegate != null)
        {
            ConfirmButtonDelegate();
        }
    }
}
