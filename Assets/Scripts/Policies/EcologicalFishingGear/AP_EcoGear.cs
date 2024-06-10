using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_EcoGear : AP_PopoutWindow
	{
		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			//If plan has policy data: display data

			//Otherwise: get previous data for country, display that

			//TODO: Get state for country at plan time
			//TODO: get overrides of current plan
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			if (a_content.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var fishingData))
			{
				ecologyDistribution.SetSliderValuesToFishingDistribution(SimulationLogicMEL.Instance.GetFishingDistributionForPreviousPlan(a_content), fishingData.fishingDistributionDelta);
				emptyContentOverlay.SetActive(ecologyDistribution.NumberGroups == 0);
			}
			else
			{
				Debug.LogError("Cannot get slider values for plan without fishing policy");
			}
			ecologyDistribution.SetInteractability(a_APWindow.Editing);
		}

		public override void ApplyContent()
		{
			//TODO: apply all toggle states, not just difference
			if (m_APWindow.Editing)
			{

				ecologyDistribution.SetFishingToSliderValues(m_plan);
			}
		}
	}
}
