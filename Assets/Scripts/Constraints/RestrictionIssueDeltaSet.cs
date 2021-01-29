using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

/// <summary>
/// A delta set that specifies the changes in issues.
/// </summary>
public class RestrictionIssueDeltaSet
{
	private HashSet<PlanIssueObject> addedIssues = new HashSet<PlanIssueObject>(IssueObjectEqualityComparer.Instance);
	private List<PlanIssueObject> removedIssues = new List<PlanIssueObject>();

	public void IssueRemoved(PlanIssueObject removed)
	{
		if (!addedIssues.Remove(removed))
		{
			bool result = removedIssues.Contains(removed);
			if (result == false)
			{
				removedIssues.Add(removed);
			}
			else
			{
				UnityEngine.Debug.LogError("Issue is already in the removed issues delta set.");
			}
		}
	}

	public void IssueAdded(PlanIssueObject added)
	{
		if (!removedIssues.Remove(added))
		{
			bool result = addedIssues.Add(added);
			if (!result)
			{
				UnityEngine.Debug.LogError("Issue already in the added delta set.");
			}
		}
	}

	public IEnumerable<PlanIssueObject> GetAddedIssues()
	{
		return addedIssues;
	}

	public IEnumerable<PlanIssueObject> GetRemovedIssues()
	{
		return removedIssues;
	}

	public bool HasAnyChanges()
	{
		return addedIssues.Count > 0 || removedIssues.Count > 0;
	}

	public void SubmitToServer(BatchRequest batch)
	{
		if (HasAnyChanges())
		{
			//form.AddField("added", GetAddedIssues());
			//form.AddField("removed", GetRemovedIssues());

			JObject dataObject = new JObject();
			dataObject.Add("added", JToken.FromObject(GetAddedIssues()));
			dataObject.Add("removed", JToken.FromObject(GetRemovedIssues()));
			batch.AddRequest(Server.SendIssues(), dataObject, BatchRequest.BATCH_GROUP_ISSUES);
		}
	}

	public void AddRemovedIssues(IEnumerable<PlanIssueObject> issues)
	{
		removedIssues.AddRange(issues);
	}

	public PlanIssueObject FindRemovedIssue(PlanIssueObject planIssueData)
	{
		PlanIssueObject removedIssue = removedIssues.Find(obj => IssueObjectEqualityComparer.Instance.Equals(obj, planIssueData));
		return removedIssue;
	}
}