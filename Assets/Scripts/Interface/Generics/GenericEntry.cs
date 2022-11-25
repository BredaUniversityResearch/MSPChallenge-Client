using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class GenericEntry : MonoBehaviour
	{
		public TextMeshProUGUI label;
		public TextMeshProUGUI value;
		public Button valueButton;
		public Image iconImage;

		public delegate void ButtonDelegate();
		public ButtonDelegate ConfirmButtonDelegate = null;

		public void SetContent(string name, string valueText)
		{
			gameObject.SetActive(true);
			label.text = name;
			value.text = valueText;
		}

		public void SetContent(string name, string valueText, UnityAction callBack)
		{
			gameObject.SetActive(true);
			label.text = name;
			value.text = valueText;
			valueButton.onClick.AddListener(callBack);
		}

		public void SetContent(string name, string valueText, Sprite icon, Color color)
		{
			gameObject.SetActive(true);
			label.text = name;
			value.text = valueText;
			iconImage.sprite = icon;
			iconImage.color = color;
		}
	}
}
