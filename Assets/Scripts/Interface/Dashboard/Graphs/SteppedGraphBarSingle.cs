﻿using System.Collections;
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
			if (a_data.m_patternNames != null && a_data.m_patternNames.Count > 0)
			{
				int patternIndex = a_cat % a_data.m_patternNames.Count;
				if (patternIndex == 0)
				{
					m_image.color = DashboardManager.Instance.ColourList.GetColour(a_data.m_absoluteCategoryIndices[a_cat] / a_data.m_patternNames.Count);
				}
				else
				{
					m_image.sprite = DashboardManager.Instance.ColourList.GetPattern(patternIndex);
					m_image.color = Color.black;
					m_image.type = Image.Type.Tiled;
					m_image.pixelsPerUnitMultiplier = 2f;
				}
				m_tooltip.text = $"{a_data.m_selectedDisplayIDs[a_cat/ a_data.m_patternNames.Count]} {a_data.m_patternNames[patternIndex]}: {a_data.FormatValue(a_data.m_steps[a_step][a_cat].Value)} {a_data.GetUnitString()}";				

			}
			else if (a_data.m_valueCountries == null)
			{
				m_image.color = DashboardManager.Instance.ColourList.GetColour(a_data.m_absoluteCategoryIndices[a_cat]);
				m_tooltip.text = $"{a_data.m_selectedDisplayIDs[a_cat]}: {a_data.FormatValue(a_data.m_steps[a_step][a_cat].Value)} {a_data.GetUnitString()}";
			}
			else
			{
				SetCountryColour(a_data, a_step, a_cat);
			}
			rect.anchorMin = new Vector2(a_xMin, a_yMin);
			rect.anchorMax = new Vector2(a_xMax, a_yMax);
			
		}

		void SetCountryColour(GraphDataStepped a_data, int a_step, int a_cat)
		{
			int iDIndex = a_cat % a_data.m_selectedDisplayIDs.Count;
			float t = (float)(iDIndex + 1) / (a_data.m_selectedDisplayIDs.Count + 1);

			Team team = SessionManager.Instance.FindTeamByID(a_data.m_valueCountries[a_cat]);
			if (team == null)
			{
				m_image.color = new Color(t, t, t, 1f);
				m_tooltip.text = $"All team's {a_data.m_selectedDisplayIDs[iDIndex]}: {a_data.FormatValue(a_data.m_steps[a_step][a_cat].Value)} {a_data.GetUnitString()}";
			}
			else
			{
				m_image.color = team.color;
				m_image.color = new Color((m_image.color.r + t) / 2f, (m_image.color.g + t) / 2f, (m_image.color.b + t) / 2f, 1f);
				m_tooltip.text = $"{team.name} team's {a_data.m_selectedDisplayIDs[iDIndex]}: {a_data.FormatValue(a_data.m_steps[a_step][a_cat].Value)} {a_data.GetUnitString()}";
			}
		}
	}
}
