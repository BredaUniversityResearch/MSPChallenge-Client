using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_Energy : AP_PopoutWindow
	{
		[SerializeField] Distribution m_energyDistribution;
		[SerializeField] GameObject m_emptyContentOverlay;

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			//TODO: this assumes grids have been (re)calculated before it is opened, check that this is the case
			m_energyDistribution.SetSliderValuesToEnergyDistribution(a_content, PolicyLogicEnergy.Instance.GetEnergyGridsBeforePlan(a_content, EnergyGrid.GridColor.Either, true, true));
			m_emptyContentOverlay.SetActive(m_energyDistribution.NumberGroups == 0);
			m_energyDistribution.SetInteractability(a_APWindow.Editing);
		}

		public override void ApplyContent()
		{
			if(m_APWindow.Editing)
				m_energyDistribution.SetGridsToSliderValues(m_plan);
		}
	}
}
