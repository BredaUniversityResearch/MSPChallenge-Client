using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace MSP2050.Scripts
{
	public class AP_PolicySelect: AP_PopoutWindow
	{
		[SerializeField] GameObject m_policyPrefab;
		[SerializeField] Transform m_contentContainer;
		[SerializeField] Button m_confirmButton;
		[SerializeField] Button m_cancelButton;

		bool m_initialised;
		bool m_changed;
		bool m_ignoreCallback;
		HashSet<string> m_originalPolicies;
		HashSet<string> m_currentPolicies;
		Dictionary<string, AP_PolicySelectEntry> m_policyEntries;

		void Initialise()
		{
			m_initialised = true;

			m_confirmButton.onClick.AddListener(OnAccept);
			m_cancelButton.onClick.AddListener(TryClose);

			m_policyEntries = new Dictionary<string, AP_PolicySelectEntry>();

			foreach(var kvp in PolicyManager.Instance.PolicyLogic)
			{
				AP_PolicySelectEntry entry = Instantiate(m_policyPrefab, m_contentContainer).GetComponent<AP_PolicySelectEntry>();
				entry.Initialise(kvp.Value, OnLayerToggleChanged);
				m_policyEntries.Add(kvp.Key, entry);
			}
		}

		private void OnDisable()
		{
			m_originalPolicies = null;
			m_currentPolicies = null;
		}

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			if (!m_initialised)
				Initialise();

			base.OpenToContent(a_content, a_toggle, a_APWindow);

			m_changed = false;
			m_ignoreCallback = true;
			foreach (var kvp in m_policyEntries)
			{
				kvp.Value.SetValue(false);
			}

			m_originalPolicies = new HashSet<string>();
			foreach (var kvp in a_content.m_policies)
			{
				m_originalPolicies.Add(kvp.Key);
				if(PolicyManager.Instance.TryGetLogic(kvp.Key, out var logic) && logic.ShowPolicyToggled(kvp.Value))
					m_policyEntries[kvp.Key].SetValue(true);
			}
			m_currentPolicies = new HashSet<string>(m_originalPolicies);
			m_ignoreCallback = false;
		}

		void OnLayerToggleChanged(APolicyLogic a_policy, bool a_value)
		{
			if (m_ignoreCallback)
				return;

			m_changed = true;
			if (a_value)
			{
				m_currentPolicies.Add(a_policy.m_definition.m_name);
			}
			else
			{
				m_currentPolicies.Remove(a_policy.m_definition.m_name);
			}
		}

		void OnAccept()
		{
			HashSet<string> added = m_currentPolicies;
			added.ExceptWith(m_originalPolicies);
			foreach(string addedPolicy in added)
			{
				if(PolicyManager.Instance.TryGetLogic(addedPolicy, out var logic))
				{
					logic.SetPolicyToggled(m_plan, true);
				}
			}
			m_originalPolicies.ExceptWith(m_currentPolicies);
			foreach (string removedPolicy in m_originalPolicies)
			{
				if (PolicyManager.Instance.TryGetLogic(removedPolicy, out var logic))
				{
					logic.SetPolicyToggled(m_plan, false);
				}
			}
			m_contentToggle.ForceClose();
			m_APWindow.RefreshContent();
		}

		public override bool MayClose()
		{
			if (m_changed)
			{
				DialogBoxManager.instance.ConfirmationWindow("Discard policy changes?", "Are you sure you want to discard any changes made to what policies are in the plan?", null, m_contentToggle.ForceClose);
				return false;
			}
			return true;
		}

	}
}
