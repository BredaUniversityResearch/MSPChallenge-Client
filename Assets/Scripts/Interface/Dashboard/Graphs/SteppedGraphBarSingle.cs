using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MSP2050.Scripts
{
	public class SteppedGraphBarSingle : MonoBehaviour
	{
		[SerializeField] Image m_image;
		[SerializeField] AddTooltip m_tooltip;

		public void SetData(GraphDataStepped a_data, int a_step, int a_cat, float a_xMin, float a_xMax, float a_yMin, float a_yMax)
		{
			RectTransform rect = GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(a_xMin, a_yMin);
			rect.anchorMax = new Vector2(a_xMax, a_yMax);
			m_tooltip.text = $"{a_data.m_categoryNames}: {a_data.m_unit.ConvertUnit(a_data.m_steps[a_step][a_cat].Value).FormatAsString()}";
		}
	}
}
