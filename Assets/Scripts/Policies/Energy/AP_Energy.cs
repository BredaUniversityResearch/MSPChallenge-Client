﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_Energy : AP_PopoutWindow
	{
		[SerializeField] GameObject m_energyDistributionGroupPrefab;
		[SerializeField] Transform m_groupParent;
		[SerializeField] GameObject m_emptyContentOverlay;
		[SerializeField] ToggleGroup m_toggleGroup;

		List<DistributionGroupEnergy> m_distributions = new List<DistributionGroupEnergy>();
		int m_nextGroupIndex;
		bool m_registered;

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			if(!m_registered)
			{
				m_registered = true;
				PolicyLogicEnergy.Instance.RegisterAPEnergy(this);
			}
			
			RefreshContent(a_content);		
		}

		public void RefreshContent(Plan a_content)
		{
			if (m_APWindow.Editing)
			{
				PolicyLogicEnergy.Instance.RecalculateGridsInEditedPlan(a_content);
			}
			Dictionary<int, GridEnergyDistribution> previousDistributions;
			List<EnergyGrid> gridsAtPlanTime = PolicyLogicEnergy.Instance.GetEnergyGridsBeforePlan(a_content, EnergyGrid.GridColor.Either, out previousDistributions, true, true);
			SetSliderValuesToEnergyDistribution(a_content, gridsAtPlanTime, previousDistributions);

			m_emptyContentOverlay.SetActive(m_nextGroupIndex == 0);
			foreach (DistributionGroupEnergy group in m_distributions)
				group.SetInteractability(m_APWindow.Editing);
			m_toggleGroup.SetAllTogglesOff();
		}

		public override void ApplyContent()
		{
			if (m_APWindow.Editing)
			{
				foreach (DistributionGroupEnergy group in m_distributions)
					group.ApplySliderValues(m_plan);
			}
		}

		public void SetSliderValuesToEnergyDistribution(Plan a_plan, List<EnergyGrid> a_energyDistribution, Dictionary<int, GridEnergyDistribution> a_previousDistributions)
		{
			m_nextGroupIndex = 0;
			if (a_energyDistribution != null)
			{ 
				foreach (EnergyGrid grid in a_energyDistribution)
				{
					EnergyGrid.GridPlanState state = grid.GetGridPlanStateAtPlan(a_plan);
					if (state == EnergyGrid.GridPlanState.Hidden || (!Main.InEditMode && state == EnergyGrid.GridPlanState.Normal))
						continue;

					GridEnergyDistribution oldDistribution = null;
					a_previousDistributions.TryGetValue(grid.m_persistentID, out oldDistribution);

					if (m_nextGroupIndex < m_distributions.Count)
					{
						m_distributions[m_nextGroupIndex].SetGrid(grid, state, m_toggleGroup, oldDistribution);
					}
					else
					{
						DistributionGroupEnergy group = Instantiate(m_energyDistributionGroupPrefab, m_groupParent).GetComponent<DistributionGroupEnergy>();
						m_distributions.Add(group);
						group.SetGrid(grid, state, m_toggleGroup, oldDistribution);

					}
					m_nextGroupIndex++;
				}
			}

			for (int i = m_nextGroupIndex; i < m_distributions.Count; i++)
			{
				m_distributions[i].gameObject.SetActive(false);    
			}
		}
	}
}
