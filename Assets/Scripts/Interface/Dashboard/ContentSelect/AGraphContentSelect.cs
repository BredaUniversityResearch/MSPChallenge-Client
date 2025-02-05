﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace MSP2050.Scripts
{
	public abstract class AGraphContentSelect : MonoBehaviour
	{
		[SerializeField] protected GraphContentSelectToggle[] m_contentToggles;
		[SerializeField] protected string[] m_contentToggleNames = new string[2] { "Type", "Country" };
		[SerializeField] protected GameObject m_detailsWindowPrefab;
		[SerializeField] protected TextMeshProUGUI m_noDataEntry;

		protected ADashboardWidget m_widget;
		protected Action m_onSettingsChanged;
		//Fixed category, toggles for content
		//Fixed category, selectable country (or: all)
		//2 fixed categories, grouped by content (different name)

		public virtual void Initialise(Action a_onSettingsChanged, ADashboardWidget a_widget)
		{
			m_widget = a_widget;
			m_onSettingsChanged = a_onSettingsChanged;
			for (int i = 0; i < m_contentToggles.Length; i++)
			{
				int index = i;
				m_contentToggles[i].m_detailsToggle.onValueChanged.AddListener((b) => ToggleDetails(b, index));
				m_contentToggles[i].m_summaryText.text = m_contentToggleNames[i];
			}
		}

		void ToggleDetails(bool a_value, int a_index)
		{
			if (a_value)
				CreateDetailsWindow(a_index);
			else
				DestroyDetailsWindow(a_index);
		}

		protected abstract void CreateDetailsWindow(int a_index);
		protected abstract void DestroyDetailsWindow(int a_index);
		public abstract GraphDataStepped FetchData(GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue);

		protected void FetchDataInternal(List<KPIValue> a_chosenKPIs, GraphDataStepped a_data, GraphTimeSettings a_timeSettings, bool a_stacked, out float a_maxValue, out float a_minValue)
		{
			a_minValue = 0f;
			a_maxValue = float.NegativeInfinity;

			if (a_timeSettings.m_aggregationFunction != null)
			{
				if (a_data.m_overLapPatternSet && a_data.m_patternNames != null && a_data.m_patternNames.Count > 0 && a_stacked)
				{
					//Aggregated with max per set
					for (int i = 0; i < a_timeSettings.m_months.Count; i++)
					{
						a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
						float stackedV = 0f;
						int indexInSet = 0;
						float maxInSet = 0f;
						for (int j = 0; j < a_chosenKPIs.Count; j++)
						{
							List<float?> values = new List<float?>(a_timeSettings.m_months[i].Count);
							foreach (int month in a_timeSettings.m_months[i])
							{
								values.Add(a_chosenKPIs[j].GetKpiValueForMonth(month));
							}
							float? aggregatedV = a_timeSettings.m_aggregationFunction(values);
							a_data.m_steps[i][j] = aggregatedV;
							if (aggregatedV.HasValue)
							{
								maxInSet = Mathf.Max(maxInSet, aggregatedV.Value);
							}
							indexInSet++;
							if (indexInSet == a_data.m_patternNames.Count)
							{
								indexInSet = 0;
								stackedV += aggregatedV.Value;
								maxInSet = 0f;
							}
						}
						a_maxValue = Mathf.Max(a_maxValue, stackedV);
						a_minValue = Mathf.Min(a_minValue, stackedV);
						
					}
				}
				else
				{
					//Aggregated
					for (int i = 0; i < a_timeSettings.m_months.Count; i++)
					{
						a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
						float stackedV = 0f;
						for (int j = 0; j < a_chosenKPIs.Count; j++)
						{
							List<float?> values = new List<float?>(a_timeSettings.m_months[i].Count);
							foreach (int month in a_timeSettings.m_months[i])
							{
								values.Add(a_chosenKPIs[j].GetKpiValueForMonth(month));
							}
							float? aggregatedV = a_timeSettings.m_aggregationFunction(values);
							a_data.m_steps[i][j] = aggregatedV;
							if (aggregatedV.HasValue)
							{
								if (!a_stacked)
								{
									a_maxValue = Mathf.Max(a_maxValue, aggregatedV.Value);
									a_minValue = Mathf.Min(a_minValue, aggregatedV.Value);
								}
								stackedV += aggregatedV.Value;
							}
						}
						if (a_stacked)
						{
							a_maxValue = Mathf.Max(a_maxValue, stackedV);
							a_minValue = Mathf.Min(a_minValue, stackedV);
						}
					}
				}
			}
			else if(a_data.m_overLapPatternSet && a_data.m_patternNames != null && a_data.m_patternNames.Count > 0 && a_stacked)
			{
				//Non-aggregated, but using max per set
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
					float stackedV = 0f;
					int indexInSet = 0;
					float maxInSet = 0f;
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						a_data.m_steps[i][j] = v;
						if (v.HasValue)
						{
							maxInSet = Mathf.Max(maxInSet, v.Value);
						}
						indexInSet++;
						if(indexInSet == a_data.m_patternNames.Count)
						{
							indexInSet = 0;
							stackedV += v.Value;
							maxInSet = 0f;
						}
					}
					a_maxValue = Mathf.Max(a_maxValue, stackedV);
					a_minValue = Mathf.Min(a_minValue, stackedV);

				}
			}
			else
			{
				//Get data directly
				for (int i = 0; i < a_timeSettings.m_months.Count; i++)
				{
					a_data.m_steps.Add(new float?[a_chosenKPIs.Count]);
					float stackedV = 0f;
					for (int j = 0; j < a_chosenKPIs.Count; j++)
					{
						float? v = a_chosenKPIs[j].GetKpiValueForMonth(a_timeSettings.m_months[i][0]);
						a_data.m_steps[i][j] = v;
						if (v.HasValue)
						{
							if (!a_stacked)
							{
								a_maxValue = Mathf.Max(a_maxValue, v.Value);
								a_minValue = Mathf.Min(a_minValue, v.Value);
							}
							stackedV += v.Value;
						}
					}
					if (a_stacked)
					{
						a_maxValue = Mathf.Max(a_maxValue, stackedV);
						a_minValue = Mathf.Min(a_minValue, stackedV);
					}
				}
			}

			if (a_maxValue == Mathf.NegativeInfinity)
				a_maxValue = 1f;
			if (Mathf.Abs(a_maxValue - a_minValue) < 0.001f)
				a_maxValue = a_minValue + 0.001f;

		}
	}
}
