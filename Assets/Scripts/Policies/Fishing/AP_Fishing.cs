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
		[SerializeField] GameObject emptyContentOverlay;

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			if (a_content.TryGetPolicyData<PolicyPlanDataFishing>(PolicyManager.FISHING_POLICY_NAME, out var fishingData))
			{
				ecologyDistribution.SetSliderValuesToFishingDistribution(PolicyLogicFishing.Instance.GetFishingDistributionForPreviousPlan(a_content), fishingData.fishingDistributionDelta);
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
			if (m_APWindow.Editing)
			{ 
			
				ecologyDistribution.SetFishingToSliderValues(m_plan);
			}
		}
	}
}
