using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace MSP2050.Scripts
{
	/// <summary>
	/// The IssueObject is the JSON representation of an issue which is sent back and forth to the API server.
	/// </summary>
	public class PlanIssueObject
	{
		//This issue_id cannot be trusted. New issues will always have issue_id -1 until they have been received by the update tick.
		//To check if issues are the same please use the IsSameIssueAs function.
		public int issue_database_id { get; set; }

		[JsonConverter(typeof(StringEnumConverter))] //Serialize the names instead of the enum values please.
		public ERestrictionIssueType type { get; set; }
		public bool active { get; set; }
		public float x { get; set; }
		public float y { get; set; }
		//public int source_plan_id { get; set; } //The plan that caused this issue.
		public int base_layer_id { get; set; } //The base layer this issue applies to.
		public int restriction_id { get; set; }

		public PlanIssueObject()
		{
		}

		public PlanIssueObject(ERestrictionIssueType restrictionType, float x, float y, int baseLayerId, int restrictionId)
		{
			issue_database_id = -1;
			active = true;
			type = restrictionType;
			this.x = (float)Math.Round(x, 2);
			this.y = (float)Math.Round(y, 2);
			base_layer_id = baseLayerId;
			restriction_id = restrictionId;
		}

		public bool IsSameIssueAs(PlanIssueObject other)
		{
			//Assumes the 2 issues are in the same plan
			if (issue_database_id != -1 && other.issue_database_id != -1 && issue_database_id == other.issue_database_id)
			{
				return true;
			}
			//Idk why but inserting a const float with a value of 0.001f becomes 0 so I'm just adding the literal values in here.
			return Mathf.Abs(x - other.x) < 0.01f &&
			       Mathf.Abs(y - other.y) < 0.01f &&
				   base_layer_id == other.base_layer_id &&
			       restriction_id == other.restriction_id &&
			       type == other.type;
		}

		public int GetIssueHash()
		{
			if (base_layer_id >= 1 << 16)
			{
				UnityEngine.Debug.LogWarning("GetIssueHash has issues. planLayerId >= 1<<16 {0}" + base_layer_id);
			}
			if (base_layer_id >= 1 << 16)
			{
				UnityEngine.Debug.LogWarning("GetIssueHash has issues. sourcePlanId >= 1<<16 {0}" + base_layer_id);
			}

			int planAndPlanLayer = base_layer_id << 16; //Assuming planLayerId < 65k
			int restrictionAndType = restriction_id | (int)type << 16;

			return x.GetHashCode() ^ y.GetHashCode() ^ planAndPlanLayer ^ restrictionAndType;
		}
	}

	public class IssueObjectEqualityComparer : IEqualityComparer<PlanIssueObject>
	{
		public static readonly IssueObjectEqualityComparer Instance = new IssueObjectEqualityComparer();

		public bool Equals(PlanIssueObject x, PlanIssueObject y)
		{
			return x.IsSameIssueAs(y);
		}

		public int GetHashCode(PlanIssueObject obj)
		{
			return obj.GetIssueHash();
		}
	}
}