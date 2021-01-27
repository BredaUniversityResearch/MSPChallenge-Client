using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

/// <summary>
/// Manager for holding the restriction area configurations.
/// </summary>
public class RestrictionAreaManager
{
	private static RestrictionAreaManager ms_RestrictionAreaManager = new RestrictionAreaManager();
	public static RestrictionAreaManager instance
	{
		get { return ms_RestrictionAreaManager; }
	}

	private class EntityTypeRestrictionSettings
	{
		private class PerPlanEntry
		{
			public Plan plan;
			public List<RestrictionAreaSetting> settings = new List<RestrictionAreaSetting>();
		};

		private List<PerPlanEntry> perPlanSettings = new List<PerPlanEntry>();

		public void AddPlanSettings(Plan referencePlan, IEnumerable<RestrictionAreaSetting> settings)
		{
			PerPlanEntry entry = FindSettingsForPlan(referencePlan);
			if (entry == null)
			{
				entry = new PerPlanEntry {plan = referencePlan};
				InsertNewPlanEntry(entry);
			}

			foreach (RestrictionAreaSetting setting in settings)
			{
				entry.settings.RemoveAll(obj => obj.teamId == setting.teamId);
				entry.settings.Add(setting);
			}
		}

		public void FindSettingsForPlan(Plan referencePlan, List<RestrictionAreaSetting> result)
		{
			PerPlanEntry entry = FindSettingsForPlan(referencePlan);
			if (entry != null)
			{
				result.AddRange(entry.settings);
			}
		}

		private PerPlanEntry FindSettingsForPlan(Plan referencePlan)
		{
			return perPlanSettings.Find(obj => obj.plan == referencePlan);
		}

		private void InsertNewPlanEntry(PerPlanEntry entry)
		{
			bool inserted = false;
			for (int i = 0; i < perPlanSettings.Count; ++i)
			{
				if (perPlanSettings[i].plan.StartTime > entry.plan.StartTime)
				{
					perPlanSettings.Insert(i, entry);
					inserted = true;
					break;
				}
			}

			if (!inserted)
			{
				perPlanSettings.Add(entry);
			}
		}

		public float GetRestrictionAreaAtPlanTime(Plan referencePlan, int teamId)
		{
			float result = 0.0f;
			for (int i = perPlanSettings.Count - 1; i >= 0; --i)
			{
				PerPlanEntry entry = perPlanSettings[i]; 
				if ((entry.plan.InInfluencingState || referencePlan == entry.plan) && 
					(referencePlan == null || entry.plan.StartTime <= referencePlan.StartTime))
				{
					RestrictionAreaSetting setting = entry.settings.Find(obj => obj.teamId == teamId);
					if (setting != null)
					{
						result = setting.restrictionSize;
						break;
					}
				}
			}
			return result;
		}
	}

	private Dictionary<EntityType, EntityTypeRestrictionSettings> restrictionSettings = new Dictionary<EntityType, EntityTypeRestrictionSettings>();

	public void SetRestrictionAreaSettings(Plan referencePlan, EntityType entityType, IEnumerable<RestrictionAreaSetting> settings)
	{
		EntityTypeRestrictionSettings entityTypeSettings;
		if (!restrictionSettings.TryGetValue(entityType, out entityTypeSettings))
		{
			entityTypeSettings = new EntityTypeRestrictionSettings();
			restrictionSettings.Add(entityType, entityTypeSettings);
		}
		entityTypeSettings.AddPlanSettings(referencePlan, settings);
	}

	public void SetRestrictionAreaSetting(Plan referencePlan, EntityType entityType, RestrictionAreaSetting setting)
	{
		//Thumbs-up
		SetRestrictionAreaSettings(referencePlan, entityType, new RestrictionAreaSetting[] {setting});
	}

	public float GetRestrictionAreaSizeAtPlanTime(Plan referencePlan, EntityType entityType, int teamId)
	{
		float result = 0.0f;
		EntityTypeRestrictionSettings settings;
		if (restrictionSettings.TryGetValue(entityType, out settings))
		{
			result = settings.GetRestrictionAreaAtPlanTime(referencePlan, teamId);
		}
		return result;
	}

	public void SubmitSettingsForPlan(Plan referencePlan, BatchRequest batch)
	{
		List<RestrictionAreaObject> settingsToSubmit = GatherSettingsForPlan(referencePlan);
		JObject dataObject = new JObject();

		//form.AddField("settings", settingsToSubmit);

		dataObject.Add("plan_id", referencePlan.ID);
		dataObject.Add("settings", JToken.FromObject(settingsToSubmit));

		batch.AddRequest(Server.SetPlanRestrictionAreas(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}
	
	private List<RestrictionAreaObject> GatherSettingsForPlan(Plan referencePlan)
	{
		List<RestrictionAreaObject> result = new List<RestrictionAreaObject>(64);

		for (int layerId = 0; layerId < referencePlan.PlanLayers.Count; ++layerId)
		{
			AbstractLayer layer = referencePlan.PlanLayers[layerId].BaseLayer;
			GatherSettingsForPlanLayer(referencePlan, layer, result);
		}

		return result;
	}

	private void GatherSettingsForPlanLayer(Plan referencePlan, AbstractLayer layer, List<RestrictionAreaObject> result)
	{
		List<RestrictionAreaSetting> settings = new List<RestrictionAreaSetting>(16);
		foreach (var entityIdTypePair in layer.EntityTypes)
		{
			EntityTypeRestrictionSettings typeSettings;
			if (restrictionSettings.TryGetValue(entityIdTypePair.Value, out typeSettings))
			{
				typeSettings.FindSettingsForPlan(referencePlan, settings);

				for (int settingId = 0; settingId < settings.Count; ++settingId)
				{
					RestrictionAreaSetting setting = settings[settingId];
					RestrictionAreaObject serverData = new RestrictionAreaObject
					{
						layer_id = layer.ID,
						entity_type_id = entityIdTypePair.Key,
						team_id = setting.teamId,
						restriction_size  = setting.restrictionSize
					};
					result.Add(serverData);
				}

				settings.Clear();
			}
		}
	}

	public void ProcessReceivedRestrictions(Plan targetPlan, RestrictionAreaObject[] planObjectRestrictionSettings)
	{
		for (int i = 0; i < planObjectRestrictionSettings.Length; ++i)
		{
			RestrictionAreaObject restrictionObj = planObjectRestrictionSettings[i];
			AbstractLayer targetLayer = LayerManager.GetLayerByID(restrictionObj.layer_id);
			EntityType targetType = targetLayer.GetEntityTypeByKey(restrictionObj.entity_type_id);
			SetRestrictionAreaSetting(targetPlan, targetType, new RestrictionAreaSetting(restrictionObj.team_id, restrictionObj.restriction_size));
		}
	}
}
