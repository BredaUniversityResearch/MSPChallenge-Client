using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_LayerSelectSubcategory : MonoBehaviour
	{
		[SerializeField] Transform m_contentContainer;
		[SerializeField] TextMeshProUGUI m_name;
		[SerializeField] Image m_icon;	

		public Transform ContentContainer => m_contentContainer;

		public void Initialise(string a_name, Sprite a_icon)
		{
			m_name.text = a_name;
			m_icon.sprite = a_icon;
		}
	}
}
