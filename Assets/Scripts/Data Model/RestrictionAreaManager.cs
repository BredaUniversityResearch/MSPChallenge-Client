using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{
	/// <summary>
	/// Manager for holding the restriction area configurations.
	/// </summary>
	public class RestrictionAreaManager
	{
		private static RestrictionAreaManager LocalInstance = new RestrictionAreaManager();
		public static RestrictionAreaManager Instance => LocalInstance;

		private class EntityTypeRestrictionSettings
		{
			private class PerPlanEntry
			{
				public Plan m_plan;
				public List<RestrictionAreaSetting> m_settings = new List<RestrictionAreaSetting>();
			};

			private List<PerPlanEntry> m_perPlanSettings = new List<PerPlanEntry>();

			public void AddPlanSettings(Plan a_referencePlan, IEnumerable<RestrictionAreaSetting> a_settings)
			{
				PerPlanEntry entry = FindSettingsForPlan(a_referencePlan);
				if (entry == null)
				{
					entry = new PerPlanEntry {m_plan = a_referencePlan};
					InsertNewPlanEntry(entry);
				}

				foreach (RestrictionAreaSetting setting in a_settings)
				{
					entry.m_settings.RemoveAll(a_obj => a_obj.teamId == setting.teamId);
					entry.m_settings.Add(setting);
				}
			}

			public void FindSettingsForPlan(Plan a_referencePlan, List<RestrictionAreaSetting> a_result)
			{
				PerPlanEntry entry = FindSettingsForPlan(a_referencePlan);
				if (entry != null)
				{
					a_result.AddRange(entry.m_settings);
				}
			}

			private PerPlanEntry FindSettingsForPlan(Plan a_referencePlan)
			{
				return m_perPlanSettings.Find(a_obj => a_obj.m_plan == a_referencePlan);
			}

			private void InsertNewPlanEntry(PerPlanEntry a_entry)
			{
				bool inserted = false;
				for (int i = 0; i < m_perPlanSettings.Count; ++i)
				{
					if (m_perPlanSettings[i].m_plan.StartTime <= a_entry.m_plan.StartTime)
						continue;
					m_perPlanSettings.Insert(i, a_entry);
					inserted = true;
					break;
				}

				if (!inserted)
				{
					m_perPlanSettings.Add(a_entry);
				}
			}

			public float GetRestrictionAreaAtPlanTime(Plan a_referencePlan, int a_teamId)
			{
				float result = 0.0f;
				for (int i = m_perPlanSettings.Count - 1; i >= 0; --i)
				{
					PerPlanEntry entry = m_perPlanSettings[i];
					if ((!entry.m_plan.InInfluencingState && a_referencePlan != entry.m_plan) ||
						(a_referencePlan != null && entry.m_plan.StartTime > a_referencePlan.StartTime))
						continue;
					RestrictionAreaSetting setting = entry.m_settings.Find(a_obj => a_obj.teamId == a_teamId);
					if (setting == null)
						continue;
					result = setting.restrictionSize;
					break;
				}
				return result;
			}
		}

		private Dictionary<EntityType, EntityTypeRestrictionSettings> m_restrictionSettings = new Dictionary<EntityType, EntityTypeRestrictionSettings>();

		private void SetRestrictionAreaSettings(Plan a_referencePlan, EntityType a_entityType, IEnumerable<RestrictionAreaSetting> a_settings)
		{
			EntityTypeRestrictionSettings entityTypeSettings;
			if (!m_restrictionSettings.TryGetValue(a_entityType, out entityTypeSettings))
			{
				entityTypeSettings = new EntityTypeRestrictionSettings();
				m_restrictionSettings.Add(a_entityType, entityTypeSettings);
			}
			entityTypeSettings.AddPlanSettings(a_referencePlan, a_settings);
		}

		public void SetRestrictionAreaSetting(Plan a_referencePlan, EntityType a_entityType, RestrictionAreaSetting a_setting)
		{
			//Thumbs-up
			SetRestrictionAreaSettings(a_referencePlan, a_entityType, new RestrictionAreaSetting[] {a_setting});
		}

		public float GetRestrictionAreaSizeAtPlanTime(Plan a_referencePlan, EntityType a_entityType, int a_teamId)
		{
			float result = 0.0f;
			if (m_restrictionSettings.TryGetValue(a_entityType, out EntityTypeRestrictionSettings settings))
			{
				result = settings.GetRestrictionAreaAtPlanTime(a_referencePlan, a_teamId);
			}
			return result;
		}

		public void SubmitSettingsForPlan(Plan a_referencePlan, BatchRequest a_batch)
		{
			List<RestrictionAreaObject> settingsToSubmit = GatherSettingsForPlan(a_referencePlan);
			JObject dataObject = new JObject {
				{
					"plan_id", a_referencePlan.GetDataBaseOrBatchIDReference()
				},
				{
					"settings", JToken.FromObject(settingsToSubmit)
				}
			};

			a_batch.AddRequest(Server.SetPlanRestrictionAreas(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
		}
	
		public List<RestrictionAreaObject> GatherSettingsForPlan(Plan a_referencePlan)
		{
			List<RestrictionAreaObject> result = new List<RestrictionAreaObject>(64);

			for (int layerId = 0; layerId < a_referencePlan.PlanLayers.Count; ++layerId)
			{
				AbstractLayer layer = a_referencePlan.PlanLayers[layerId].BaseLayer;
				GatherSettingsForPlanLayer(a_referencePlan, layer, result);
			}

			return result;
		}

		private void GatherSettingsForPlanLayer(Plan a_referencePlan, AbstractLayer a_layer, List<RestrictionAreaObject> a_result)
		{
			List<RestrictionAreaSetting> settings = new List<RestrictionAreaSetting>(16);
			foreach (var entityIdTypePair in a_layer.m_entityTypes)
			{
				EntityTypeRestrictionSettings typeSettings;
				if (!m_restrictionSettings.TryGetValue(entityIdTypePair.Value, out typeSettings))
					continue;
				typeSettings.FindSettingsForPlan(a_referencePlan, settings);

				for (int settingId = 0; settingId < settings.Count; ++settingId)
				{
					RestrictionAreaSetting setting = settings[settingId];
					RestrictionAreaObject serverData = new RestrictionAreaObject
					{
						layer_id = a_layer.m_id,
						entity_type_id = entityIdTypePair.Key,
						team_id = setting.teamId,
						restriction_size  = setting.restrictionSize
					};
					a_result.Add(serverData);
				}

				settings.Clear();
			}
		}

		public void SetRestrictionsToObject(Plan a_targetPlan, IEnumerable<RestrictionAreaObject> a_planObjectRestrictionSettings)
		{
			if (a_planObjectRestrictionSettings == null)
				return;
			foreach (RestrictionAreaObject restrictionObj in a_planObjectRestrictionSettings)
			{
				AbstractLayer targetLayer = LayerManager.Instance.GetLayerByID(restrictionObj.layer_id);
				EntityType targetType = targetLayer.GetEntityTypeByKey(restrictionObj.entity_type_id);
				SetRestrictionAreaSetting(a_targetPlan, targetType, new RestrictionAreaSetting(restrictionObj.team_id, restrictionObj.restriction_size));
			}
		}
	}
}
