using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_Fishing : AP_PopoutWindow
	{
		[SerializeField] Distribution ecologyDistribution;

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			ecologyDistribution.SetSliderValuesToFishingDistribution(SimulationLogicMEL.Instance.GetFishingDistributionForPreviousPlan(planDetails.SelectedPlan), planDetails.SelectedPlan.fishingDistributionDelta);
			emptyContentOverlay.SetActive(ecologyDistribution.NumberGroups == 0);
			ecologyDistribution.SetInteractability(a_APWindow.Editing);
		}

		public override void ApplyContent()
		{
			if (m_APWindow.Editing)
			{ 
			
				ecologyDistribution.SetFishingToSliderValues(m_plan);
			}
		}
	}
}
