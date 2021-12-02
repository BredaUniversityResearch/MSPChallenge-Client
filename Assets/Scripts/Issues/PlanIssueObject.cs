using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

/// <summary>
/// The IssueObject is the JSON representation of an issue which is sent back and forth to the API server.
/// 
/// super short variable names to get less characters when serialising
/// ^ IMO this is a terrible motivation for creating unreadable code. 
/// </summary>
public class PlanIssueObject
{
	//21-11-2017 PdG: I've aliased these values with their old names so we can at least have some readability in the code.
	//24-11-2017 PdG: And they're gone...

	//This issue_id cannot be trusted. New issues will always have issue_id -1 until they have been received by the update tick.
	//To check if issues are the same please use the IsSameIssueAs function.
	public int issue_database_id { get; set; }

	[JsonConverter(typeof(StringEnumConverter))] //Serialize the names instead of the enum values please.
	public ERestrictionIssueType type { get; set; }
	public bool active { get; set; }
	public float x { get; set; }
	public float y { get; set; }
	public int source_plan_id { get; set; } //The plan that caused this issue.
	public int plan_layer_id { get; set; } //The plan layer this issue applies to.
	public int restriction_id { get; set; }

	public PlanIssueObject()
	{
	}

	public PlanIssueObject(ERestrictionIssueType restrictionType, float x, float y, int sourcePlanId, int planLayerId, int restrictionId)
	{
		issue_database_id = -1;
		active = true;
		type = restrictionType;
		this.x = (float)Math.Round(x, 2);
		this.y = (float)Math.Round(y, 2);
		source_plan_id = sourcePlanId;
		plan_layer_id = planLayerId;
		restriction_id = restrictionId;
	}

	public bool IsSameIssueAs(PlanIssueObject other)
	{
		if (issue_database_id != -1 && other.issue_database_id != -1 && issue_database_id == other.issue_database_id)
		{
			return true;
		}

		//Idk why but inserting a const float with a value of 0.001f becomes 0 so I'm just adding the literal values in here.
		return Mathf.Abs(x - other.x) < 0.01f &&
			   Mathf.Abs(y - other.y) < 0.01f &&
			   source_plan_id == other.source_plan_id &&
			   plan_layer_id == other.plan_layer_id &&
			   restriction_id == other.restriction_id &&
			   type == other.type;
	}

	public int GetIssueHash()
	{
		if (plan_layer_id >= 1 << 16)
		{
			UnityEngine.Debug.LogWarning("GetIssueHash has issues. planLayerId >= 1<<16 {0}" + plan_layer_id);
		}
		if (source_plan_id >= 1 << 16)
		{
			UnityEngine.Debug.LogWarning("GetIssueHash has issues. sourcePlanId >= 1<<16 {0}" + plan_layer_id);
		}

		int planAndPlanLayer = source_plan_id | plan_layer_id << 16; //Assuming planLayerId < 65k
		int restrictionAndType = restriction_id | (int)type << 16;

		return x.GetHashCode() ^ y.GetHashCode() ^ planAndPlanLayer ^ restrictionAndType;
	}

	public Plan GetSourcePlan()
	{
        //Maybe we should cache this...
        //return PlanManager.GetPlanAtIndex(source_plan_id);
        //This used to get the plan by index... while using an ID. Seemed incorrect. -Kevin 4/6/18
        return PlanManager.GetPlanWithID(source_plan_id);
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
