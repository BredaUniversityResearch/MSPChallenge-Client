using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MSP2050.Scripts
{
	public class AP_Shipping : AP_PopoutWindow
	{
		private class DistributionItemEntry
		{
			public AbstractLayer targetLayer;
			public EntityType targetType;
			public DistributionItem item;

			public DistributionItemEntry(AbstractLayer targetLayer, EntityType targetType, DistributionItem item)
			{
				this.targetLayer = targetLayer;
				this.targetType = targetType;
				this.item = item;
			}
		};

		[SerializeField] Distribution m_distributions = null;
		[SerializeField] GameObject m_emptyContentOverlay;
		[SerializeField] TextMeshProUGUI m_emptyContentText;

		private Plan currentDistributionsForPlan = null;
		private Dictionary<EntityType, DistributionGroupShipping> distributionGroups = new Dictionary<EntityType, DistributionGroupShipping>();
		private List<DistributionItemEntry> distributionItemEntries = new List<DistributionItemEntry>();

		public override void OpenToContent(Plan a_content, AP_ContentToggle a_toggle, ActivePlanWindow a_APWindow)
		{
			base.OpenToContent(a_content, a_toggle, a_APWindow);
			UpdateDistributions();
			m_emptyContentOverlay.SetActive(m_distributions.NumberGroups == 0);
			m_emptyContentText.text = a_APWindow.Editing ? "This plan contains no layers for which shipping restrictions can be set" : "No shipping restriction areas have been changed";
			m_distributions.SetInteractability(a_APWindow.Editing);
		}

		public override void ApplyContent()
		{
			if (m_APWindow.Editing)
			{
				foreach (DistributionItemEntry itemEntry in distributionItemEntries)
				{
					if (itemEntry.item.changed)
					{
						RestrictionAreaSetting setting = new RestrictionAreaSetting(itemEntry.item.Country, itemEntry.item.GetDistributionValue());

						RestrictionAreaManager.instance.SetRestrictionAreaSetting(m_APWindow.CurrentPlan, itemEntry.targetType, setting);
					}
				}
			}
		}

		public void UpdateDistributions()
		{
			if (currentDistributionsForPlan != m_plan)
			{
				DestroyDistributions();
			}

			for (int i = 0; i < m_plan.PlanLayers.Count; ++i)
			{
				PlanLayer layer = m_plan.PlanLayers[i];
				foreach (var kvp in layer.BaseLayer.m_entityTypes)
				{
					if (SessionManager.Instance.IsManager(m_plan.Country))
					{
						foreach (Team team in SessionManager.Instance.GetTeams())
						{
							if (!team.IsManager)
							{
								CreateDistributionForTeam(team.ID, kvp, layer);
							}
						}
					}
					else
					{
						CreateDistributionForTeam(m_plan.Country, kvp, layer);
					}
				}
			}
			currentDistributionsForPlan = m_plan;
		}

		private void CreateDistributionForTeam(int a_teamId, KeyValuePair<int, EntityType> a_kvp, PlanLayer a_layer)
		{
			float restrictionSize = RestrictionAreaManager.instance.GetRestrictionAreaSizeAtPlanTime(m_plan, a_kvp.Value, a_teamId);
			SetDistributionSlider(a_layer.BaseLayer, a_kvp.Value, a_teamId, restrictionSize);
		}

		private void DestroyDistributions()
		{
			m_distributions.DestroyAllGroups();
			distributionGroups.Clear();
		}

		private void SetDistributionSlider(AbstractLayer a_baseLayer, EntityType a_entityType, int a_teamId, float a_restrictionSize)
		{
			DistributionGroupShipping group;
			if (!distributionGroups.TryGetValue(a_entityType, out group))
			{
				group = (DistributionGroupShipping)m_distributions.CreateGroup(a_baseLayer.ShortName);
				group.SetTitle(a_baseLayer.ShortName, a_entityType.Name);
				distributionGroups.Add(a_entityType, group);
			}

			DistributionItem item = group.FindDistributionItemByCountryId(a_teamId);
			if (item == null)
			{
				item = group.CreateItem(a_teamId, 1f);
				distributionItemEntries.Add(new DistributionItemEntry(a_baseLayer, a_entityType, item));
			}
			group.UpdateDistributionItem(item, a_restrictionSize);
		}
	}
}
