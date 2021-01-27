using System.Collections.Generic;
using UnityEngine;

public class MultiLayerRestrictionIssueCollection
{
	private Dictionary<PlanLayer, List<PlanIssueObject>> issuesPerLayer = new Dictionary<PlanLayer, List<PlanIssueObject>>(8, PlanLayerIdEqualityComparer.Instance);

	public void AddIssue(Plan offendingPlan, PlanLayer targetLayer, Vector3 pos, ConstraintTarget constraint)
	{
		List<PlanIssueObject> issueList;
		if (!issuesPerLayer.TryGetValue(targetLayer, out issueList))
		{
			issueList = new List<PlanIssueObject>(16);
			issuesPerLayer.Add(targetLayer, issueList);
		}

		PlanIssueObject planIssueObject = new PlanIssueObject(constraint.issueType, pos.x, pos.y, offendingPlan.ID, targetLayer.ID, constraint.constraintId);
		issueList.Add(planIssueObject);
	}

	public IEnumerable<KeyValuePair<PlanLayer, List<PlanIssueObject>>> GetIssues()
	{
		return issuesPerLayer;
	}

	public bool HasIssues()
	{
		return issuesPerLayer.Count > 0;
	}
}