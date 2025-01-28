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
			if (a_data.m_selectedCountries == null)
			{
				m_image.color = DashboardManager.Instance.ColourList.GetColour(a_data.m_absoluteCategoryIndices[a_cat]);
				m_tooltip.text = $"{a_data.m_selectedDisplayIDs[a_cat]}: {a_data.FormatValue(a_data.m_steps[a_step][a_cat].Value)} {a_data.GetUnitString()}";
			}
			else
			{
				int iDIndex = a_cat % a_data.m_selectedDisplayIDs.Count;
				int country = a_data.m_selectedCountries[a_cat / a_data.m_selectedDisplayIDs.Count];
				float t = (float)(iDIndex + 1) / (a_data.m_selectedDisplayIDs.Count + 1);

				Team team = SessionManager.Instance.GetTeamByTeamID(country);
				m_image.color = team.color;
				m_image.color = new Color((m_image.color.r + t) / 2f, (m_image.color.g + t) / 2f, (m_image.color.b + t) / 2f, 1f);
				m_tooltip.text = $"{team.name} team's {a_data.m_selectedDisplayIDs[iDIndex]}: {a_data.FormatValue(a_data.m_steps[a_step][a_cat].Value)} {a_data.GetUnitString()}";
			}
			rect.anchorMin = new Vector2(a_xMin, a_yMin);
			rect.anchorMax = new Vector2(a_xMax, a_yMax);
			
		}
	}
}
