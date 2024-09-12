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
		bool m_registered;

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);

			if (!m_registered)
			{
				m_registered = true;
				PolicyLogicEcoGear.Instance.RegisterAPEcoGear(this);
			}

			RefreshContent(a_content);
		}

		public void RefreshContent(Plan a_content, bool a_applyCurrent = true)
		{
			if (m_APWindow.Editing && a_applyCurrent)
			{
				//Apply content already changed before updating
				ApplyContent();
			}

			m_settingsBeforePlan = PolicyLogicEcoGear.Instance.GetEcoGearSettingBeforePlan(a_content);
			PolicyPlanDataEcoGear planData;
			a_content.TryGetPolicyData<PolicyPlanDataEcoGear>(PolicyManager.ECO_GEAR_POLICY_NAME, out planData);
			CountryFleetInfo[] fleets = PolicyLogicFishing.Instance.GetAllFleetInfo();
			string[] gearTypes = PolicyLogicFishing.Instance.GetGearTypes();

			if (m_fleetGearToggles == null || m_fleetGearToggles.Count == 0)
			{
				m_fleetGearToggles = new List<FleetEcoGearToggle>();
				for (int i = 0; i < fleets.Length; i++)
				{
					FleetEcoGearToggle toggle = Instantiate(m_fleetEntryPrefab, m_fleetEntryParent).GetComponent<FleetEcoGearToggle>();
					m_fleetGearToggles.Add(toggle);
				}
			}

			for (int i = 0; i < fleets.Length; i++)
			{
				bool oldValue = false;
				m_settingsBeforePlan.TryGetValue(i, out oldValue);
				if (planData.m_values.TryGetValue(i, out bool value))
				{
					m_fleetGearToggles[i].SetContent(gearTypes[fleets[i].gear_type], fleets[i].country_id, value, oldValue,
						m_APWindow.Editing && (SessionManager.Instance.IsManager(a_content.Country) || fleets[i].country_id == a_content.Country));
				}
				else
				{
					m_fleetGearToggles[i].SetContent(gearTypes[fleets[i].gear_type], fleets[i].country_id, oldValue, oldValue,
						m_APWindow.Editing && (SessionManager.Instance.IsManager(a_content.Country) || fleets[i].country_id == a_content.Country));
				}
			}
		}

		public override void ApplyContent()
		{
			Dictionary<int, bool> result = new Dictionary<int, bool>();
			//Applies all active toggles, not just difference
			for (int i = 0; i < m_fleetGearToggles.Count; i++)
			{
				if(m_fleetGearToggles[i].Interactable)
				{
					result[i] = m_fleetGearToggles[i].Value;
				}
			}
			m_plan.SetPolicyData(new PolicyPlanDataEcoGear(PolicyLogicEcoGear.Instance) { policy_type = PolicyManager.ECO_GEAR_POLICY_NAME, m_values = result });
		}
	}
}
