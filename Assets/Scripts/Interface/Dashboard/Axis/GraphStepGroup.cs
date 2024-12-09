using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public class GraphStepGroup : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_groupName;

		public void SetContent(string a_name, float a_anchorMin, float a_anchorMax)
		{
			RectTransform rect = GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(a_anchorMin, 0f);
			rect.anchorMax = new Vector2(a_anchorMax, 1f);
			m_groupName.text = a_name;
		}
	}
}
