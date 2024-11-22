using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class GraphAxisEntry : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_text;
		[SerializeField] bool m_horizontal;
		[SerializeField] float m_baseTextOffset = -12f;

		public void SetValueStretch(string a_name, float a_anchorMin, float a_anchorMax)
		{
			gameObject.SetActive(true);
			m_text.text = a_name;

			RectTransform rect = GetComponent<RectTransform>();
			if(m_horizontal)
			{
				rect.anchorMin = new Vector2(a_anchorMin, 1f);
				rect.anchorMax = new Vector2(a_anchorMax, 1f);
			}
			else
			{
				rect.anchorMin = new Vector2(1f, a_anchorMin);
				rect.anchorMax = new Vector2(1f, a_anchorMax);
			}
			rect.sizeDelta = Vector2.zero;
		}

		public void SetValuePoint(string a_name, float a_anchorPosition, float a_leftOffset, float a_textOffset)
		{
			m_text.text = a_name; 
			gameObject.SetActive(true);

			RectTransform rect = GetComponent<RectTransform>();
			if (m_horizontal)
			{
				rect.anchorMin = new Vector2(a_anchorPosition, 1f);
				rect.anchorMax = new Vector2(a_anchorPosition, 1f);
			}
			else
			{
				rect.anchorMin = new Vector2(0f, a_anchorPosition);
				rect.anchorMax = new Vector2(1f, a_anchorPosition);
			}
			rect.sizeDelta = Vector2.zero;
			rect.offsetMin = new Vector2(a_leftOffset, 0f);
			rect.offsetMax = Vector2.zero;

			m_text.GetComponent<RectTransform>().anchoredPosition = new Vector2(m_baseTextOffset, a_textOffset);
		}
	}
}
