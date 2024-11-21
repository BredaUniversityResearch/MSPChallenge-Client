using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class GraphLegendEntry : MonoBehaviour
	{
		[SerializeField] Image m_colourImage;
		[SerializeField] TextMeshProUGUI m_nameText;
		[SerializeField] AddTooltip m_tooltip;
		

		public int m_height;
		public int m_preferredWidth;

		public void SetData(string a_name, Color a_colour, float a_xMin, float a_xMax, float a_xOffset, float a_yOffset)
		{
			//xOffset is spacing from sides
			//yOffset is offset from top

			gameObject.SetActive(true);
			m_nameText.text = a_name; 
			m_colourImage.color = a_colour;
			m_tooltip.text = a_name;

			RectTransform rect = GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(a_xMin, 1f);
			rect.anchorMax = new Vector2(a_xMax, 1f);
			rect.offsetMin = new Vector2(a_xOffset, -(a_yOffset + m_height));
			rect.offsetMax = new Vector2(-a_xOffset, -a_yOffset);
		}
	}
}
