using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

public static class ConstraintManager
{
	public enum EConstraintType { Exclusion, Inclusion, Type_Unavailable }

	private enum EConstraintSatisfyRule
	{
		All, //All checked geometry must satisfy the constraint, and we will emit an issue for all that does not.
		Any	//If any checked geometry satisfies the constraint we will not emit an issue, only if no geometry satisfies the rule an issue will be created.
	};

	/// <summary>
	/// Constraint source, this is the object you will test against the target 
	/// ("restriction_start_layer_id" and "restriction_start_layer_type" in server)
	/// </summary>
	private struct ConstraintSource
	{
		public AbstractLayer layer;
		public EntityType entityType;
	}

	private struct ConstraintSourceEqualityComparer : IEqualityComparer<ConstraintSource>
	{
		public static readonly ConstraintSourceEqualityComparer instance = new ConstraintSourceEqualityComparer();

		public bool Equals(ConstraintSource lhs, ConstraintSource rhs)
		{
			return lhs.layer == rhs.layer && lhs.entityType == rhs.entityType;
		}

		public int GetHashCode(ConstraintSource obj)
		{
			int result = 0;
			if (obj.layer != null)
			{
				result = obj.layer.GetHashCode();
			}
			if (obj.entityType != null)
			{
				result ^= obj.entityType.GetHashCode();
			}
			return result;
		}
	};

	private class RestrictionQueryCache
	{
		private Dictionary<HashCode, List<SubEntity>> cachedTargets = new Dictionary<HashCode, List<SubEntity>>(HashCodeEqualityComparer.Instance); 

		public void AddCachedEntry(HashCode entryHash, List<SubEntity> entries)
		{
			cachedTargets.Add(entryHash, entries);
		}

		public List<SubEntity> FindCachedEntry(HashCode hashCode)
		{
			List<SubEntity> result;
			cachedTargets.TryGetValue(hashCode, out result);
			return result;
		}

		/// <summary>
		/// Make a key based on the layer and type
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public HashCode GetEntryHash(AbstractLayer layer, EntityType type)
		{
			if (type == null)
			{
				return new HashCode(layer.FileName + "complete_layer");
			}

			return new HashCode(layer.FileName + layer.GetEntityTypeKey(type));
		}

		public HashCode GetEntryHash(PlanLayer planLayer, EntityType type)
		{
			StringBuilder sb = new StringBuilder(128);
			sb.Append("PLAN_LAYER");
			sb.Append(planLayer.ID);
			sb.Append((type == null) ? "complete_layer" : type.Name);

			return new HashCode(sb.ToString());
		}
	}

	// used to store error messages with their IDs to avoid passing around long strings
	private static Dictionary<int, string> restrictionIdToMessage = new Dictionary<int, string>();

	/// <summary>
	/// Separate dictionary for inclusions and exclusions
	/// </summary>
	private static Dictionary<ConstraintSource, List<ConstraintTarget>> inclusions = new Dictionary<ConstraintSource, List<ConstraintTarget>>(ConstraintSourceEqualityComparer.instance);
	private static Dictionary<ConstraintSource, List<ConstraintTarget>> exclusions = new Dictionary<ConstraintSource, List<ConstraintTarget>>(ConstraintSourceEqualityComparer.instance);
	private static Dictionary<ConstraintSource, ConstraintTarget> typeUnavailableConstraint = new Dictionary<ConstraintSource, ConstraintTarget>();
	private static Dictionary<int, KeyValuePair<ConstraintSource, ConstraintTarget>> restrictionIdToConstraint = new Dictionary<int, KeyValuePair<ConstraintSource, ConstraintTarget>>();

	public static float ConstraintPointCollisionSize
	{
		get;
		private set;
	}

	/// <summary>
	/// Adds a possible constraint, only done when loading in constraints
	/// </summary>
	/// <param name="layerA">restriction_start_layer_id</param>
	/// <param name="typeA">restriction_start_layer_type</param>
	/// <param name="layerB">restriction_end_layer_id</param>
	/// <param name="typeB">restriction_end_layer_type</param>
	/// <param name="message">restriction_message</param>
	/// <param name="issueSeverity">restriction_type</param>
	/// <param name="constraintType">restriction_sort</param>
	/// <param name="value">restriction_value</param>
	private static void AddConstraint(AbstractLayer layerA, EntityType typeA, AbstractLayer layerB, EntityType typeB, string message, ERestrictionIssueType issueSeverity, EConstraintType constraintType, float value, int restrictionId)
	{
		ConstraintSource source = new ConstraintSource
		{
			layer = layerA,
			entityType = typeA
		};

		ConstraintTarget target = new ConstraintTarget(restrictionId, layerB, typeB, issueSeverity, message, value);

		restrictionIdToConstraint.Add(restrictionId, new KeyValuePair<ConstraintSource, ConstraintTarget>(source, target));

		switch (constraintType)
		{
		case EConstraintType.Inclusion:
			AddInclusion(source, target);
			break;
		case EConstraintType.Exclusion:
			AddExclusion(source, target);
			break;
		case EConstraintType.Type_Unavailable:
			AddTypeUnavailable(source, target);
			break;
		default:
			Debug.LogError("Unknown constraint type " + constraintType);
			break;
		}
	}

	/// <summary>
	/// Add an inclusion to the dictionary
	/// </summary>
	private static void AddInclusion(ConstraintSource source, ConstraintTarget target)
	{
		if (inclusions.ContainsKey(source))
			inclusions[source].Add(target);
		else
			inclusions.Add(source, new List<ConstraintTarget>() { target });
	}

	/// <summary>
	/// Add an exclusion to the dictionary
	/// </summary>
	private static void AddExclusion(ConstraintSource source, ConstraintTarget target)
	{
		if (exclusions.ContainsKey(source))
			exclusions[source].Add(target);
		else
			exclusions.Add(source, new List<ConstraintTarget>() { target });
	}

	private static void AddTypeUnavailable(ConstraintSource source, ConstraintTarget target)
	{
		typeUnavailableConstraint[source] = target;
	}

	private static ConstraintTarget FindTypeUnavailableConstraint(AbstractLayer targetLayer, EntityType targetEntityType)
	{
		ConstraintSource source = new ConstraintSource {entityType = targetEntityType, layer = targetLayer};

		ConstraintTarget target;
		typeUnavailableConstraint.TryGetValue(source, out target);
		return target;
	}

	/// <summary>
	/// Check constraints against any future plans
	/// </summary>
	public static RestrictionIssueDeltaSet CheckConstraints(Plan plan, List<PlanIssueObject> existingIssues, bool notifyUserOfExternalIssues)
	{
		RestrictionQueryCache cache = new RestrictionQueryCache();
		MultiLayerRestrictionIssueCollection issueCollection = new MultiLayerRestrictionIssueCollection();
		RestrictionIssueDeltaSet deltaSet = new RestrictionIssueDeltaSet();

		if (existingIssues != null)
		{
			deltaSet.AddRemovedIssues(existingIssues);
		}

		// Replace all the issues 
		IssueManager.instance.RemoveIssuesForPlan(plan, existingIssues == null? deltaSet : null);

		// check against all future plan layers and past baselayers
		foreach (PlanLayer planLayer in plan.PlanLayers)
		{
			CheckRestrictionsForLayer(cache, plan, planLayer, true, issueCollection);

			//For now don't check for future restrictions. This currently puts issues in plans that are in approved, which is undesired. Scheduled for a rework.
			//CheckRestrictionsForFuture(cache, planLayer, plan, issueCollection);
		}

		CheckTypeUnavailableConstraints(plan, plan.StartTime, issueCollection);

		//Send the issues to the issue manager.
		IssueManager.instance.ImportNewIssues(issueCollection, deltaSet);
		IssueManager.instance.SetIssueVisibilityForPlan(plan, true);

		if (notifyUserOfExternalIssues)
		{
			NotifyUserOfExternalIssues(plan, issueCollection);
		}

		return deltaSet;
	}

	public static void CheckTypeUnavailableConstraints(Plan plan, int implementationDate, MultiLayerRestrictionIssueCollection issueCollection)
	{
		foreach (PlanLayer layer in plan.PlanLayers)
		{
			bool checkEntityTypes = false;
			foreach (EntityType entityType in layer.BaseLayer.EntityTypes.Values)
			{
				if (entityType.availabilityDate > implementationDate)
				{
					checkEntityTypes = true;
					break;
				}
			}

			if (checkEntityTypes)
			{
				foreach (Entity newEntity in layer.GetNewGeometry())
				{
					foreach (EntityType type in newEntity.EntityTypes)
					{
						if (type.availabilityDate > implementationDate)
						{
							Vector3 issueLocation = newEntity.GetSubEntity(0).BoundingBox.center;
							ConstraintTarget issueConstraint = FindTypeUnavailableConstraint(layer.BaseLayer, type);
							if (issueConstraint != null)
							{
								issueCollection.AddIssue(plan, layer, issueLocation, issueConstraint);
							}
						}
					}
				}
			}
		}
	}

	private static void NotifyUserOfExternalIssues(Plan currentPlan, MultiLayerRestrictionIssueCollection issueCollection)
	{
		List<Plan> externalPlans = new List<Plan>();
		foreach (var kvp in issueCollection.GetIssues())
		{
			if (kvp.Key.Plan != currentPlan && !externalPlans.Contains(kvp.Key.Plan))
			{
				externalPlans.Add(kvp.Key.Plan);
			}
		}

		if (externalPlans.Count > 0)
		{
			StringBuilder notificationText = new StringBuilder(256);
			notificationText.Append("The accepted changes have created issues in the following plans:\n\n");
			for (int i = 0; i < externalPlans.Count; ++i)
			{
				notificationText.Append("<color=#").Append(Util.ColorToHex(TeamManager.GetTeamByTeamID(externalPlans[i].Country).color)).Append(">");
				notificationText.Append(" - ").Append(externalPlans[i].Name).Append("\n");
				notificationText.Append("</color>");
			}
			DialogBoxManager.instance.NotificationWindow("Issues in other plans", notificationText.ToString(), () => { });
		}
	}

	private class FuturePlanEntityEntry
	{
		public SubEntity subEntity;
		public int checkUntilMonth;
	};

	/// <summary>
	/// Check restrictions against future plans, including layers and types
	/// </summary>
	private static void CheckRestrictionsForFutureLayerAndType(RestrictionQueryCache cache, PlanLayer checkingThisPlanLayer, EntityType checkingThisEntityType, Plan checkingThisPlan, MultiLayerRestrictionIssueCollection resultIssueCollection)
	{
		//get all the future geometry of this plan and add it to the source subentities
		List<FuturePlanEntityEntry> entityEntries = BuildFuturePlanEntityList(cache, checkingThisPlan, checkingThisPlanLayer, checkingThisEntityType);

		//Future checks are the other way around. We check the target against te source, so we need to find all inclusion constraints that have the layer and type as targets.
		List<KeyValuePair<ConstraintSource, ConstraintTarget>> constraintsToCheck = FindConstraintsForTarget(inclusions, checkingThisPlanLayer.BaseLayer, checkingThisEntityType); 
		CheckFutureConstraints(checkingThisPlan, resultIssueCollection, entityEntries, constraintsToCheck, EConstraintType.Inclusion);

		List<KeyValuePair<ConstraintSource, ConstraintTarget>> exclusionConstraintsToCheck = FindConstraintsForTarget(exclusions, checkingThisPlanLayer.BaseLayer, checkingThisEntityType);
		CheckFutureConstraints(checkingThisPlan, resultIssueCollection, entityEntries, exclusionConstraintsToCheck, EConstraintType.Exclusion);
	}

	private static void CheckFutureConstraints(Plan checkingThisPlan, MultiLayerRestrictionIssueCollection resultIssueCollection, List<FuturePlanEntityEntry> entityEntries, List<KeyValuePair<ConstraintSource, ConstraintTarget>> constraintsToCheck, EConstraintType constraintType)
	{
		for (int i = 0; i < constraintsToCheck.Count; i++)
		{
			KeyValuePair<ConstraintSource, ConstraintTarget> target = constraintsToCheck[i];

			//Get all the plan layers for the constraint sources
			List<PlanLayer> relevantFuturePlanLayers = PlanManager.GetPlanLayersForBaseLayerFrom(target.Key.layer, checkingThisPlan.StartTime, true);
			if (relevantFuturePlanLayers.Count == 0)
			{
				continue;
			}

			for (int entryId = 0; entryId < entityEntries.Count; ++entryId)
			{
				FuturePlanEntityEntry entry = entityEntries[entryId];
				ConstraintChecks.DoCheck check;
				EConstraintSatisfyRule satisfyRule;
				if (constraintType == EConstraintType.Inclusion) 
				{
					check = ConstraintChecks.PickCorrectInclusionCheckType(target.Key.layer, target.Value.layer);
					satisfyRule = EConstraintSatisfyRule.All;
				}
				else if (constraintType == EConstraintType.Exclusion)
				{
					check = ConstraintChecks.PickCorrectExclusionCheckType(target.Key.layer, target.Value.layer);
					satisfyRule = EConstraintSatisfyRule.Any;
				}
				else
				{
					Debug.LogError("Invalid constraint type for constraint check " + constraintType);
					return;
				}

				CheckFutureConstraintForPlanEntity(entry, checkingThisPlan, target.Value, check, satisfyRule, relevantFuturePlanLayers, resultIssueCollection);
			}
		}
	}

	private static List<KeyValuePair<ConstraintSource, ConstraintTarget>> FindConstraintsForTarget(Dictionary<ConstraintSource, List<ConstraintTarget>> constraintCollection, AbstractLayer targetBaseLayer, EntityType targetEntityType)
	{
		List<KeyValuePair<ConstraintSource, ConstraintTarget>> result = new List<KeyValuePair<ConstraintSource, ConstraintTarget>>(32);
		foreach (var kvp in constraintCollection)
		{
			ConstraintTarget target = kvp.Value.Find(obj => obj.entityType == targetEntityType && obj.layer == targetBaseLayer);
			if (target != null)
			{
				result.Add(new KeyValuePair<ConstraintSource, ConstraintTarget>(kvp.Key, target));
			}
		}
		return result;
	}

	private static List<FuturePlanEntityEntry> BuildFuturePlanEntityList(RestrictionQueryCache cache, Plan plan, PlanLayer checkingThisPlanLayer, EntityType checkingThisEntityType)
	{
		List<SubEntity> sourceObjects = GetAllSubentitiesOfType(cache, checkingThisPlanLayer, checkingThisEntityType);

		List<FuturePlanEntityEntry> result = new List<FuturePlanEntityEntry>(sourceObjects.Count);
		//For each subentity try to figure out the time of the first plan that changes it.
		for (int subEntityId = 0; subEntityId < sourceObjects.Count; ++subEntityId)
		{
			SubEntity subEntity = sourceObjects[subEntityId];
			int changeTime = Main.MspGlobalData.session_end_month;
			Plan firstPlanChangingGeometry = PlanManager.FindFirstPlanChangingGeometry(plan.StartTime, subEntity.Entity.PersistentID, checkingThisPlanLayer.BaseLayer);
			if (firstPlanChangingGeometry != null)
			{
				changeTime = firstPlanChangingGeometry.StartTime;
			}

			FuturePlanEntityEntry entry = new FuturePlanEntityEntry()
			{
				subEntity = subEntity,
				checkUntilMonth = changeTime,
			};
			result.Add(entry);
		}
		return result;
	}

	/// <summary>
	/// Check restrictions against future plans
	/// </summary>
	/// <param name="cache"></param>
	/// <param name="checkingThisPlanLayer"></param>
	/// <param name="plan"></param>
	private static void CheckRestrictionsForFuture(RestrictionQueryCache cache, PlanLayer checkingThisPlanLayer, Plan plan, MultiLayerRestrictionIssueCollection resultIssueCollection)
	{
		// check if the type is set to null (This will check all types)
		CheckRestrictionsForFutureLayerAndType(cache, checkingThisPlanLayer, null, plan, resultIssueCollection);
		foreach (var kvp in checkingThisPlanLayer.BaseLayer.EntityTypes)
		{
			//check each type individually
			CheckRestrictionsForFutureLayerAndType(cache, checkingThisPlanLayer, kvp.Value, plan, resultIssueCollection);
		}
	}

	private static void CheckRestrictionsForLayerAndType(RestrictionQueryCache cache, Plan checkingThisPlan, PlanLayer planLayer, EntityType checkingThisEntityType, bool checkOnlyPlanEntities, MultiLayerRestrictionIssueCollection resultIssueCollection)
	{
		ConstraintSource tmpSource = new ConstraintSource
		{
			layer = planLayer.BaseLayer,
			entityType = checkingThisEntityType
		};

		List<SubEntity> sourceObjects;
		if (checkOnlyPlanEntities)
		{
			sourceObjects = GetAllSubentitiesOfType(cache, planLayer, checkingThisEntityType);
		}
		else
		{
			sourceObjects = GetAllSubentitiesOfType(cache, planLayer.BaseLayer, checkingThisEntityType, checkingThisPlan);
		}

		if (inclusions.ContainsKey(tmpSource))
		{
			List<ConstraintTarget> targets = inclusions[tmpSource];
			foreach (ConstraintTarget target in targets)
			{
				// do the check
				CheckConstraint(cache, sourceObjects, target, ConstraintChecks.PickCorrectInclusionCheckType(planLayer.BaseLayer, target.layer), EConstraintSatisfyRule.All, planLayer, checkingThisPlan, resultIssueCollection);
			}
		}

		if (exclusions.ContainsKey(tmpSource))
		{
			List<ConstraintTarget> targets = exclusions[tmpSource];
			foreach (ConstraintTarget target in targets)
			{
				CheckConstraint(cache, sourceObjects, target, ConstraintChecks.PickCorrectExclusionCheckType(planLayer.BaseLayer, target.layer), EConstraintSatisfyRule.Any, planLayer, checkingThisPlan, resultIssueCollection);
			}
		}
	}

	private static void CheckRestrictionsForLayer(RestrictionQueryCache cache, Plan checkingThisPlan, PlanLayer planLayer, bool checkOnlyPlanEntities, MultiLayerRestrictionIssueCollection resultIssueCollection)
	{
		CheckRestrictionsForLayerAndType(cache, checkingThisPlan, planLayer, null, checkOnlyPlanEntities, resultIssueCollection);
		foreach (var kvp in planLayer.BaseLayer.EntityTypes)
		{
			CheckRestrictionsForLayerAndType(cache, checkingThisPlan, planLayer, kvp.Value, checkOnlyPlanEntities, resultIssueCollection);
		}
	}

	private static void CheckConstraint(RestrictionQueryCache cache, List<SubEntity> sourceObjects, ConstraintTarget target, ConstraintChecks.DoCheck check, EConstraintSatisfyRule satisfactionRule, PlanLayer planLayer, Plan checkingThisPlan, MultiLayerRestrictionIssueCollection resultIssueCollection)
	{
		if (check == null)
		{
			Debug.LogError("Got NULL constraint check for layers " + planLayer.BaseLayer.FileName + " and " + target.layer.FileName);
			return;
		}

		// get all the target objects 
		List<SubEntity> targetObjects = GetAllSubentitiesOfType(cache, target.layer, target.entityType, checkingThisPlan);

		foreach (SubEntity a in sourceObjects)
		{
			bool anySatisfies = false;
			foreach (SubEntity b in targetObjects)
			{
				if (a != b)
				{
					// do the check. Returns true if there is an error.
					Vector3 issueLocation;
					if (check.Invoke(a, b, target, planLayer, checkingThisPlan, out issueLocation))
					{
						if (satisfactionRule == EConstraintSatisfyRule.All)
						{
							resultIssueCollection.AddIssue(checkingThisPlan, planLayer, issueLocation, target);
						}
					}
					else if (satisfactionRule == EConstraintSatisfyRule.Any)
					{
						anySatisfies = true;
						break;
					}
				}
			}

			if (satisfactionRule == EConstraintSatisfyRule.Any && anySatisfies == false)
			{
				resultIssueCollection.AddIssue(checkingThisPlan, planLayer, a.BoundingBox.center, target);
			}
		}
	}

	private static void CheckFutureConstraintForPlanEntity(FuturePlanEntityEntry entityEntry, Plan checkingThisPlan, ConstraintTarget target, ConstraintChecks.DoCheck check, EConstraintSatisfyRule satisfyRule, IList<PlanLayer> relevantFutureLayers, MultiLayerRestrictionIssueCollection resultIssueCollection)
	{
		for (int i = 0; i < relevantFutureLayers.Count; ++i)
		{
			PlanLayer futureLayer = relevantFutureLayers[i];
			if (futureLayer.Plan.StartTime > entityEntry.checkUntilMonth)
			{
				//Plan layer is outside of our checking time.
				continue;
			}

			for (int newGeometryId = 0; newGeometryId < futureLayer.GetNewGeometryCount(); ++newGeometryId)
			{
				bool anySatisfies = false;

				Entity newGeometry = futureLayer.GetNewGeometryByIndex(newGeometryId);
				if (target.entityType == null || newGeometry.EntityTypes.Contains(target.entityType))
				{
					for (int subEntityId = 0; subEntityId < newGeometry.GetSubEntityCount(); ++subEntityId)
					{
						SubEntity targetSubEntity = newGeometry.GetSubEntity(subEntityId);
						Vector3 issueLocation;
						if (check.Invoke(entityEntry.subEntity, targetSubEntity, target, futureLayer, checkingThisPlan, out issueLocation))
						{
							if (satisfyRule == EConstraintSatisfyRule.All)
							{
								resultIssueCollection.AddIssue(checkingThisPlan, futureLayer, issueLocation, target);
							}
						}
						else if (satisfyRule == EConstraintSatisfyRule.Any)
						{
							anySatisfies = true;
							break;
						}
					}
				}

				if (satisfyRule == EConstraintSatisfyRule.Any && anySatisfies == false)
				{
					resultIssueCollection.AddIssue(checkingThisPlan, futureLayer, newGeometry.GetSubEntity(0).BoundingBox.center, target);
				}
			}
		}
	}

	private static List<SubEntity> GetAllSubentitiesOfType(RestrictionQueryCache cache, PlanLayer planLayer, EntityType type)
	{
		HashCode cacheKey = cache.GetEntryHash(planLayer, type);
		List<SubEntity> sourceObjects = cache.FindCachedEntry(cacheKey);
		if (sourceObjects != null)
		{
			return sourceObjects;
		}

		sourceObjects = new List<SubEntity>();
		int newGeometryCount = planLayer.GetNewGeometryCount();
		for (int i = 0; i < newGeometryCount; ++i)
		{
			Entity newGeometry = planLayer.GetNewGeometryByIndex(i);
			if (type == null || newGeometry.EntityTypes.Contains(type))
			{
				int subEntityCount = newGeometry.GetSubEntityCount();
				for (int subEntityId = 0; subEntityId < subEntityCount; ++subEntityId)
				{
					sourceObjects.Add(newGeometry.GetSubEntity(subEntityId));
				}
			}
		}

		cache.AddCachedEntry(cacheKey, sourceObjects);

		return sourceObjects;
	}

	private static List<SubEntity> GetAllSubentitiesOfType(RestrictionQueryCache cache, AbstractLayer layer, EntityType type, Plan planToCheckFor)
	{
		// If we already got all the entities of this layer-type then just return them
		// make sure to clear this cached targets befre and/or after checking for constraints
		HashCode cacheKey = cache.GetEntryHash(layer, type);
		List<SubEntity> sourceObjects = cache.FindCachedEntry(cacheKey);
		if (sourceObjects != null)
		{
			return sourceObjects;
		}

		if (type == null)
		{
			// base objects of entire layer
			sourceObjects = GetSubentitiesAtTime(layer, planToCheckFor.StartTime, planToCheckFor);
		}
		else
		{
			// base objects of given type
			sourceObjects = GetSubentitiesAtTime(layer, type, planToCheckFor.StartTime, planToCheckFor);
		}

		cache.AddCachedEntry(cacheKey, sourceObjects);

		return sourceObjects;
	}

	private static List<SubEntity> GetSubentitiesAtTime(AbstractLayer layer, EntityType type, int month, Plan planToCheckFor)
	{
		LayerState state = layer.GetLayerStateAtTime(month, planToCheckFor);
		List<SubEntity> result = new List<SubEntity>(state.baseGeometry.Capacity);
		int entityTypeKey = layer.GetEntityTypeKey(type);

		for (int i = 0; i < state.baseGeometry.Count; ++i)
		{
			Entity ent = state.baseGeometry[i];
			if (ent.GetEntityTypeKeys().Contains(entityTypeKey))
			{
				for (int subEntityId = 0; subEntityId < ent.GetSubEntityCount(); ++subEntityId)
				{
					result.Add(ent.GetSubEntity(subEntityId));
				}
			}
		}

		return result;
	}

	private static List<SubEntity> GetSubentitiesAtTime(AbstractLayer layer, int month, Plan treatAsInfluencing = null)
	{
		LayerState state = layer.GetLayerStateAtTime(month, treatAsInfluencing);
		List<SubEntity> result = new List<SubEntity>(state.baseGeometry.Capacity);

		for (int i = 0; i < state.baseGeometry.Count; ++i)
		{
			Entity ent = state.baseGeometry[i];
			for (int subEntityId = 0; subEntityId < ent.GetSubEntityCount(); ++subEntityId)
			{
				result.Add(ent.GetSubEntity(subEntityId));
			}
		}

		return result;
	}

	public static void LoadRestrictions()
	{
		NetworkForm form = new NetworkForm();
		ServerCommunication.DoRequest<RestrictionConfigObject>(Server.GetRestrictions(), form, HandleLoadRestrictionsCallback);
	}

	private static void HandleLoadRestrictionsCallback(RestrictionConfigObject restrictionConfig)
	{
		restrictionIdToMessage.Clear();
		ConstraintPointCollisionSize = restrictionConfig.restriction_point_size;

		foreach (RestrictionObject restriction in restrictionConfig.restrictions)
		{
			// start layer
			AbstractLayer startLayer = LayerManager.GetLoadedLayer(restriction.start_layer);

			// value (so far only used for raster layer)
			float value = restriction.value;

			// end layer
			AbstractLayer endLayer = LayerManager.GetLoadedLayer(restriction.end_layer);

			if (startLayer != null && endLayer != null)
			{
				EntityType startEntityType = null;
				EntityType endEntityType = null;

				// set start entity type, otherwise its null, which means all entity types
				if (!string.IsNullOrEmpty(restriction.start_type))
				{
					int startEntityTypeId = Util.ParseToInt(restriction.start_type);
					startEntityType = startLayer.GetEntityTypeByKey(startEntityTypeId);
				}

				// set end entity type, otherwise its null, which means all entity types
				if (!string.IsNullOrEmpty(restriction.end_type))
				{
					int endEntityTypeId = Util.ParseToInt(restriction.end_type);
					endEntityType = endLayer.GetEntityTypeByKey(endEntityTypeId);
				}

				AddConstraint(startLayer, startEntityType, endLayer, endEntityType, restriction.message, restriction.type, restriction.sort, value, restriction.id);
				//constraints are only one way, uncomment this if I want both ways//AddExclusion(endLayer, endEntityType, startLayer, startEntityType, restriction.message);

				// add messages to dictionaries 
				restrictionIdToMessage[restriction.id] = restriction.message;
			}
		}
	}

	public static string GetRestrictionMessage(int restrictionId)
	{
		string text;
		if (restrictionIdToMessage.ContainsKey(restrictionId))
		{
			text = restrictionIdToMessage[restrictionId];
		}
		else
		{
			text = "Failed to load message of ID " + restrictionId;
		}
		return text;
	}

	public static KeyValuePair<AbstractLayer, AbstractLayer> GetRestrictionLayersForRestrictionId(int restrictionId)
	{
		KeyValuePair<ConstraintSource, ConstraintTarget> constraint;
		if (restrictionIdToConstraint.TryGetValue(restrictionId, out constraint))
		{
			return new KeyValuePair<AbstractLayer, AbstractLayer>(constraint.Key.layer, constraint.Value.layer);
		}
		else
		{
			return new KeyValuePair<AbstractLayer, AbstractLayer>(null, null);
		}
	}
}