using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ConstraintManager : MonoBehaviour
	{
		public enum EConstraintType { Exclusion, Inclusion, Type_Unavailable }

		private enum EConstraintSatisfyRule
		{
			All, //All checked geometry must satisfy the constraint, and we will emit an issue for all that does not.
			Any //If any checked geometry satisfies the constraint we will not emit an issue, only if no geometry satisfies the rule an issue will be created.
		};

		/// <summary>
		/// Constraint source, this is the object you will test against the target 
		/// ("restriction_start_layer_id" and "restriction_start_layer_type" in server)
		/// </summary>
		private struct ConstraintSource
		{
			public AbstractLayer m_layer;
			public EntityType m_entityType;
		}

		private struct ConstraintSourceEqualityComparer : IEqualityComparer<ConstraintSource>
		{
			public static readonly ConstraintSourceEqualityComparer Instance = new ConstraintSourceEqualityComparer();

			public bool Equals(ConstraintSource a_lhs, ConstraintSource a_rhs)
			{
				return a_lhs.m_layer == a_rhs.m_layer && a_lhs.m_entityType == a_rhs.m_entityType;
			}

			public int GetHashCode(ConstraintSource a_obj)
			{
				int result = 0;
				if (a_obj.m_layer != null)
				{
					result = a_obj.m_layer.GetHashCode();
				}
				if (a_obj.m_entityType != null)
				{
					result ^= a_obj.m_entityType.GetHashCode();
				}
				return result;
			}
		};

		private class RestrictionQueryCache
		{
			private Dictionary<HashCode, List<SubEntity>> cachedTargets = new Dictionary<HashCode, List<SubEntity>>(HashCodeEqualityComparer.Instance);

			public void AddCachedEntry(HashCode a_entryHash, List<SubEntity> a_entries)
			{
				cachedTargets.Add(a_entryHash, a_entries);
			}

			public List<SubEntity> FindCachedEntry(HashCode a_hashCode)
			{
				List<SubEntity> result;
				cachedTargets.TryGetValue(a_hashCode, out result);
				return result;
			}

			/// <summary>
			/// Make a key based on the layer and type
			/// </summary>
			/// <param name="a_layer"></param>
			/// <param name="a_type"></param>
			/// <returns></returns>
			public HashCode GetEntryHash(AbstractLayer a_layer, EntityType a_type)
			{
				if (a_type == null)
				{
					return new HashCode(a_layer.FileName + "complete_layer");
				}

				return new HashCode(a_layer.FileName + a_layer.GetEntityTypeKey(a_type));
			}

			public HashCode GetEntryHash(PlanLayer a_planLayer, EntityType a_type)
			{
				StringBuilder sb = new StringBuilder(128);
				sb.Append("PLAN_LAYER");
				sb.Append(a_planLayer.BaseLayer.m_id);
				sb.Append((a_type == null) ? "complete_layer" : a_type.Name);

				return new HashCode(sb.ToString());
			}
		}

		private static ConstraintManager singleton;
		public static ConstraintManager Instance
		{
			get
			{
				if (singleton == null)
					singleton = FindObjectOfType<ConstraintManager>();
				return singleton;
			}
		}

		// used to store error messages with their IDs to avoid passing around long strings
		private Dictionary<int, string> restrictionIdToMessage = new Dictionary<int, string>();

		/// <summary>
		/// Separate dictionary for inclusions and exclusions
		/// </summary>
		private Dictionary<ConstraintSource, List<ConstraintTarget>> inclusions = new Dictionary<ConstraintSource, List<ConstraintTarget>>(ConstraintSourceEqualityComparer.Instance);
		private Dictionary<ConstraintSource, List<ConstraintTarget>> exclusions = new Dictionary<ConstraintSource, List<ConstraintTarget>>(ConstraintSourceEqualityComparer.Instance);
		private Dictionary<ConstraintSource, ConstraintTarget> typeUnavailableConstraint = new Dictionary<ConstraintSource, ConstraintTarget>();
		private Dictionary<int, KeyValuePair<ConstraintSource, ConstraintTarget>> restrictionIdToConstraint = new Dictionary<int, KeyValuePair<ConstraintSource, ConstraintTarget>>();

		public float ConstraintPointCollisionSize
		{
			get;
			private set;
		}

		private void Start()
		{
			if (singleton != null && singleton != this)
				Destroy(this);
			else
				singleton = this;
		}

		private void OnDestroy()
		{
			singleton = null;
		}

		/// <summary>
		/// Adds a possible constraint, only done when loading in constraints
		/// </summary>
		/// <param name="a_layerA">restriction_start_layer_id</param>
		/// <param name="a_typeA">restriction_start_layer_type</param>
		/// <param name="a_layerB">restriction_end_layer_id</param>
		/// <param name="a_typeB">restriction_end_layer_type</param>
		/// <param name="a_message">restriction_message</param>
		/// <param name="a_issueSeverity">restriction_type</param>
		/// <param name="a_constraintType">restriction_sort</param>
		/// <param name="a_value">restriction_value</param>
		private void AddConstraint(AbstractLayer a_layerA, EntityType a_typeA, AbstractLayer a_layerB, EntityType a_typeB, string a_message, ERestrictionIssueType a_issueSeverity, EConstraintType a_constraintType, float a_value, int a_restrictionId)
		{
			ConstraintSource source = new ConstraintSource
			{
				m_layer = a_layerA,
				m_entityType = a_typeA
			};

			ConstraintTarget target = new ConstraintTarget(a_restrictionId, a_layerB, a_typeB, a_issueSeverity, a_message, a_value);

			restrictionIdToConstraint.Add(a_restrictionId, new KeyValuePair<ConstraintSource, ConstraintTarget>(source, target));

			switch (a_constraintType)
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
					Debug.LogError("Unknown constraint type " + a_constraintType);
					break;
			}
		}

		/// <summary>
		/// Add an inclusion to the dictionary
		/// </summary>
		private void AddInclusion(ConstraintSource a_source, ConstraintTarget a_target)
		{
			if (inclusions.ContainsKey(a_source))
				inclusions[a_source].Add(a_target);
			else
				inclusions.Add(a_source, new List<ConstraintTarget>() { a_target });
		}

		/// <summary>
		/// Add an exclusion to the dictionary
		/// </summary>
		private void AddExclusion(ConstraintSource a_source, ConstraintTarget a_target)
		{
			if (exclusions.ContainsKey(a_source))
				exclusions[a_source].Add(a_target);
			else
				exclusions.Add(a_source, new List<ConstraintTarget>() { a_target });
		}

		private void AddTypeUnavailable(ConstraintSource a_source, ConstraintTarget a_target)
		{
			typeUnavailableConstraint[a_source] = a_target;
		}

		private ConstraintTarget FindTypeUnavailableConstraint(AbstractLayer a_targetLayer, EntityType a_targetEntityType)
		{
			ConstraintSource source = new ConstraintSource { m_entityType = a_targetEntityType, m_layer = a_targetLayer };

			typeUnavailableConstraint.TryGetValue(source, out ConstraintTarget target);
			return target;
		}

		public void CheckConstraints(Plan a_plan, out List<string> a_unavailableTypeNames)
		{
			RestrictionQueryCache cache = new RestrictionQueryCache();

			foreach (PlanLayer planLayer in a_plan.PlanLayers)
			{
				//Clear current issues
				if (planLayer.issues != null)
				{
					if (planLayer.issues.Count > 0)
						planLayer.issues.Clear();
				}
				else
					planLayer.issues = new HashSet<PlanIssueObject>(new IssueObjectEqualityComparer());

				CheckRestrictionsForLayer(cache, a_plan, planLayer, true);
			}

			a_unavailableTypeNames = CheckTypeUnavailableConstraints(a_plan, a_plan.StartTime);
		}

		public void CheckConstraintsForLayer(Plan a_plan, PlanLayer a_planLayer)
		{
			RestrictionQueryCache cache = new RestrictionQueryCache();

			if (a_planLayer.issues != null)
			{
				if (a_planLayer.issues.Count > 0)
					a_planLayer.issues.Clear();
			}
			else
				a_planLayer.issues = new HashSet<PlanIssueObject>(new IssueObjectEqualityComparer());

			CheckRestrictionsForLayer(cache, a_plan, a_planLayer, true);
		}

		private List<string> CheckTypeUnavailableConstraints(Plan a_plan, int a_implementationDate)
		{
			List<string> result = new List<string>();
			foreach (PlanLayer layer in a_plan.PlanLayers)
			{
				bool checkEntityTypes = false;
				foreach (EntityType entityType in layer.BaseLayer.m_entityTypes.Values)
				{
					if (entityType.availabilityDate <= a_implementationDate)
						continue;
					checkEntityTypes = true;
					break;
				}

				if (!checkEntityTypes)
					continue;
				foreach (Entity newEntity in layer.GetNewGeometry())
				{
					foreach (EntityType type in newEntity.EntityTypes)
					{
						if (type.availabilityDate <= a_implementationDate)
							continue;
						Vector3 issueLocation = newEntity.GetSubEntity(0).m_boundingBox.center;
						ConstraintTarget issueConstraint = FindTypeUnavailableConstraint(layer.BaseLayer, type);
						if (issueConstraint == null)
							continue;
						layer.issues.Add(new PlanIssueObject(issueConstraint.issueType, issueLocation.x, issueLocation.y, layer.BaseLayer.m_id, issueConstraint.constraintId));
						result.Add(type.Name);
					}
				}
			}
			return result;
		}

		private List<KeyValuePair<ConstraintSource, ConstraintTarget>> FindConstraintsForTarget(Dictionary<ConstraintSource, List<ConstraintTarget>> a_constraintCollection, AbstractLayer a_targetBaseLayer, EntityType a_targetEntityType)
		{
			List<KeyValuePair<ConstraintSource, ConstraintTarget>> result = new List<KeyValuePair<ConstraintSource, ConstraintTarget>>(32);
			foreach (var kvp in a_constraintCollection)
			{
				ConstraintTarget target = kvp.Value.Find(a_obj => a_obj.entityType == a_targetEntityType && a_obj.layer == a_targetBaseLayer);
				if (target != null)
				{
					result.Add(new KeyValuePair<ConstraintSource, ConstraintTarget>(kvp.Key, target));
				}
			}
			return result;
		}

		private void CheckRestrictionsForLayerAndType(RestrictionQueryCache a_cache, Plan a_checkingThisPlan, PlanLayer a_planLayer, EntityType a_checkingThisEntityType, bool a_checkOnlyPlanEntities)
		{
			ConstraintSource tmpSource = new ConstraintSource
			{
				m_layer = a_planLayer.BaseLayer,
				m_entityType = a_checkingThisEntityType
			};

			List<SubEntity> sourceObjects;
			if (a_checkOnlyPlanEntities)
			{
				sourceObjects = GetAllSubentitiesOfType(a_cache, a_planLayer, a_checkingThisEntityType);
			}
			else
			{
				sourceObjects = GetAllSubentitiesOfType(a_cache, a_planLayer.BaseLayer, a_checkingThisEntityType, a_checkingThisPlan);
			}

			if (inclusions.ContainsKey(tmpSource))
			{
				List<ConstraintTarget> targets = inclusions[tmpSource];
				foreach (ConstraintTarget target in targets)
				{
					// do the check
					CheckConstraint(a_cache, sourceObjects, target, ConstraintChecks.PickCorrectInclusionCheckType(a_planLayer.BaseLayer, target.layer), EConstraintSatisfyRule.All, a_planLayer, a_checkingThisPlan);
				}
			}

			if (!exclusions.ContainsKey(tmpSource))
				return;
			{
				List<ConstraintTarget> targets = exclusions[tmpSource];
				foreach (ConstraintTarget target in targets)
				{
					CheckConstraint(a_cache, sourceObjects, target, ConstraintChecks.PickCorrectExclusionCheckType(a_planLayer.BaseLayer, target.layer), EConstraintSatisfyRule.Any, a_planLayer, a_checkingThisPlan);
				}
			}
		}

		private void CheckRestrictionsForLayer(RestrictionQueryCache a_cache, Plan a_checkingThisPlan, PlanLayer a_planLayer, bool a_checkOnlyPlanEntities)
		{
			CheckRestrictionsForLayerAndType(a_cache, a_checkingThisPlan, a_planLayer, null, a_checkOnlyPlanEntities);
			foreach (var kvp in a_planLayer.BaseLayer.m_entityTypes)
			{
				CheckRestrictionsForLayerAndType(a_cache, a_checkingThisPlan, a_planLayer, kvp.Value, a_checkOnlyPlanEntities);
			}
		}

		private void CheckConstraint(RestrictionQueryCache a_cache, List<SubEntity> a_sourceObjects, ConstraintTarget a_target, ConstraintChecks.DoCheck a_check, EConstraintSatisfyRule a_satisfactionRule, PlanLayer a_planLayer, Plan a_checkingThisPlan)
		{
			if (a_check == null)
			{
				Debug.LogError("Got NULL constraint check for layers " + a_planLayer.BaseLayer.FileName + " and " + a_target.layer.FileName);
				return;
			}

			// get all the target objects 
			List<SubEntity> targetObjects = GetAllSubentitiesOfType(a_cache, a_target.layer, a_target.entityType, a_checkingThisPlan);

			foreach (SubEntity a in a_sourceObjects)
			{
				bool anySatisfies = false;
				foreach (SubEntity b in targetObjects)
				{
					if (a == b)
						continue;
					if(a == null || b == null)
					{
						Debug.LogError($"Encountered null geometry when doing an overlap check for layers {a_planLayer.BaseLayer.FileName} and {a_target.layer.FileName}");
					}

					// do the check. Returns true if there is an error.
					if (a_check.Invoke(a, b, a_target, a_planLayer, a_checkingThisPlan, out var issueLocation))
					{
						if (a_satisfactionRule == EConstraintSatisfyRule.All)
						{
							a_planLayer.issues.Add(new PlanIssueObject(a_target.issueType, issueLocation.x, issueLocation.y, a_planLayer.BaseLayer.m_id, a_target.constraintId));
						}
					}
					else if (a_satisfactionRule == EConstraintSatisfyRule.Any)
					{
						anySatisfies = true;
						break;
					}
				}

				if (a_satisfactionRule == EConstraintSatisfyRule.Any && anySatisfies == false)
				{
					a_planLayer.issues.Add(new PlanIssueObject(a_target.issueType, a.m_boundingBox.center.x, a.m_boundingBox.center.y, a_planLayer.BaseLayer.m_id, a_target.constraintId));
				}
			}
		}

		private List<SubEntity> GetAllSubentitiesOfType(RestrictionQueryCache a_cache, PlanLayer a_planLayer, EntityType a_type)
		{
			HashCode cacheKey = a_cache.GetEntryHash(a_planLayer, a_type);
			List<SubEntity> sourceObjects = a_cache.FindCachedEntry(cacheKey);
			if (sourceObjects != null)
			{
				return sourceObjects;
			}

			sourceObjects = new List<SubEntity>();
			int newGeometryCount = a_planLayer.GetNewGeometryCount();
			for (int i = 0; i < newGeometryCount; ++i)
			{
				Entity newGeometry = a_planLayer.GetNewGeometryByIndex(i);
				if (a_type == null || newGeometry.EntityTypes.Contains(a_type))
				{
					int subEntityCount = newGeometry.GetSubEntityCount();
					for (int subEntityId = 0; subEntityId < subEntityCount; ++subEntityId)
					{
						sourceObjects.Add(newGeometry.GetSubEntity(subEntityId));
					}
				}
			}

			a_cache.AddCachedEntry(cacheKey, sourceObjects);

			return sourceObjects;
		}

		private List<SubEntity> GetAllSubentitiesOfType(RestrictionQueryCache a_cache, AbstractLayer a_layer, EntityType a_type, Plan a_planToCheckFor)
		{
			// If we already got all the entities of this layer-type then just return them
			// make sure to clear this cached targets befre and/or after checking for constraints
			HashCode cacheKey = a_cache.GetEntryHash(a_layer, a_type);
			List<SubEntity> sourceObjects = a_cache.FindCachedEntry(cacheKey);
			if (sourceObjects != null)
			{
				return sourceObjects;
			}

			if (a_type == null)
			{
				// base objects of entire layer
				sourceObjects = GetSubentitiesAtTime(a_layer, a_planToCheckFor.StartTime, a_planToCheckFor);
			}
			else
			{
				// base objects of given type
				sourceObjects = GetSubentitiesAtTime(a_layer, a_type, a_planToCheckFor.StartTime, a_planToCheckFor);
			}

			a_cache.AddCachedEntry(cacheKey, sourceObjects);

			return sourceObjects;
		}

		private List<SubEntity> GetSubentitiesAtTime(AbstractLayer a_layer, EntityType a_type, int a_month, Plan a_planToCheckFor)
		{
			LayerState state = a_layer.GetLayerStateAtTime(a_month, a_planToCheckFor);
			List<SubEntity> result = new List<SubEntity>(state.baseGeometry.Capacity);
			int entityTypeKey = a_layer.GetEntityTypeKey(a_type);

			for (int i = 0; i < state.baseGeometry.Count; ++i)
			{
				Entity ent = state.baseGeometry[i];
				if (!ent.GetEntityTypeKeys().Contains(entityTypeKey))
					continue;
				for (int subEntityId = 0; subEntityId < ent.GetSubEntityCount(); ++subEntityId)
				{
					result.Add(ent.GetSubEntity(subEntityId));
				}
			}

			return result;
		}

		private List<SubEntity> GetSubentitiesAtTime(AbstractLayer a_layer, int a_month, Plan a_treatAsInfluencing = null)
		{
			LayerState state = a_layer.GetLayerStateAtTime(a_month, a_treatAsInfluencing);
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

		public void LoadRestrictions()
		{
			NetworkForm form = new NetworkForm();
			ServerCommunication.Instance.DoRequest<RestrictionConfigObject>(Server.GetRestrictions(), form, HandleLoadRestrictionsCallback);
		}

		private void HandleLoadRestrictionsCallback(RestrictionConfigObject a_restrictionConfig)
		{
			restrictionIdToMessage.Clear();
			ConstraintPointCollisionSize = a_restrictionConfig.restriction_point_size;

			foreach (RestrictionObject restriction in a_restrictionConfig.restrictions)
			{
				// start layer
				AbstractLayer startLayer = LayerManager.Instance.GetLoadedLayer(restriction.start_layer);

				// value (so far only used for raster layer)
				float value = restriction.value;

				// end layer
				AbstractLayer endLayer = LayerManager.Instance.GetLoadedLayer(restriction.end_layer);

				if (startLayer == null || endLayer == null)
					continue;
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

		public string GetRestrictionMessage(int a_restrictionId)
		{
			string text;
			if (restrictionIdToMessage.ContainsKey(a_restrictionId))
			{
				text = restrictionIdToMessage[a_restrictionId];
			}
			else
			{
				text = "Failed to load message of ID " + a_restrictionId;
			}
			return text;
		}

		public KeyValuePair<AbstractLayer, AbstractLayer> GetRestrictionLayersForRestrictionId(int a_restrictionId)
		{
			KeyValuePair<ConstraintSource, ConstraintTarget> constraint;
			if (restrictionIdToConstraint.TryGetValue(a_restrictionId, out constraint))
			{
				return new KeyValuePair<AbstractLayer, AbstractLayer>(constraint.Key.m_layer, constraint.Value.layer);
			}
			return new KeyValuePair<AbstractLayer, AbstractLayer>(null, null);
		}
	}
}
