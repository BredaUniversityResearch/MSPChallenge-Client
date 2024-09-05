using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_EcoGear : AP_PopoutWindow
	{
		[SerializeField] GameObject m_fleetEntryPrefab;
		[SerializeField] Transform m_fleetEntryParent;

		List<FleetEcoGearToggle> m_fleetGearToggles;

		Dictionary<int, bool> m_settingsBeforePlan;
		int m_activeEntries;

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			m_settingsBeforePlan = PolicyLogicEcoGear.Instance.GetEcoGearSettingBeforePlan(a_content);
			PolicyPlanDataEcoGear planData;
			a_content.TryGetPolicyData<PolicyPlanDataEcoGear>(PolicyManager.ECO_GEAR_POLICY_NAME, out planData);
			CountryFleetInfo[] fleets = PolicyLogicFishing.Instance.GetAllFleetInfo();
			string[] gearTypes = PolicyLogicFishing.Instance.GetGearTypes();

			m_activeEntries = 0;
			for(int i = 0; i< fleets.Length; i++)
			{
				//TODO: get and use value
				if(SessionManager.Instance.IsManager(a_content.Country) || fleets[i].country_id == a_content.Country)
				{
					if(m_activeEntries <= m_fleetGearToggles.Count)
					{
						FleetEcoGearToggle toggle = Instantiate(m_fleetEntryPrefab, m_fleetEntryParent).GetComponent<FleetEcoGearToggle>();
						//toggle.SetContent(gearTypes[fleets[i].gear_type], )
					}
				}
			}

			//TODO: Get state for country at plan time
			//TODO: get overrides of current plan
			RefreshContent(a_content);
		}

		public void RefreshContent(Plan a_content)
		{
			if (m_APWindow.Editing)
			{
				//TODO
			}
			//TODO
		}

		public override void ApplyContent()
		{
			//TODO: apply all toggle states, not just difference
		}
	}
}
