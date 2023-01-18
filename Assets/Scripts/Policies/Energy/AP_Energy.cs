using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reactive.Joins;

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

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			//TODO CHECK: this assumes grids have been (re)calculated before it is opened, check that this is the case
			if(a_APWindow.Editing)
			{
				PolicyLogicEnergy.Instance.RecalculateGridsInEditedPlan(a_content);
				SetSliderValuesToEnergyDistribution(a_content, PolicyLogicEnergy.Instance.GetEnergyGridsBeforePlan(a_content, EnergyGrid.GridColor.Either, true, false));
			}
			else
			{
				SetSliderValuesToEnergyDistribution(a_content, PolicyLogicEnergy.Instance.GetEnergyGridsBeforePlan(a_content, EnergyGrid.GridColor.Either, true, true));
				//if (a_content.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var data))
				//{
				//	SetSliderValuesToEnergyDistribution(a_content, data.energyGrids);
				//	//TODO: show removed grids
				//}
				//else
				//{
				//	Debug.LogError("Energy distribution tab visible in AP window for plan without energy policy data");
				//}
			}

			m_emptyContentOverlay.SetActive(m_nextGroupIndex == 0);
			foreach (DistributionGroupEnergy group in m_distributions)
				group.SetInteractability(a_APWindow.Editing);
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

		public void SetSliderValuesToEnergyDistribution(Plan a_plan, List<EnergyGrid> a_energyDistribution)
		{
			m_nextGroupIndex = 0;
			if (a_energyDistribution != null)
			{ 
				foreach (EnergyGrid grid in a_energyDistribution)
				{
					EnergyGrid.GridPlanState state = grid.GetGridPlanStateAtPlan(a_plan);
					if (state == EnergyGrid.GridPlanState.Hidden || (!Main.InEditMode && state == EnergyGrid.GridPlanState.Normal))
						continue;

					if (m_nextGroupIndex < m_distributions.Count)
					{
						m_distributions[m_nextGroupIndex].SetGrid(grid, state, m_toggleGroup);
					}
					else
					{
						DistributionGroupEnergy group = Instantiate(m_energyDistributionGroupPrefab, m_groupParent).GetComponent<DistributionGroupEnergy>();
						m_distributions.Add(group);
						group.SetGrid(grid, state, m_toggleGroup);

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
