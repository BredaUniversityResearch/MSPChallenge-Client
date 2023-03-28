using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_LayerSelectCategory : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_name;

		public void Initialise(string a_name)
		{
			m_name.text = a_name;
		}
	}
}
