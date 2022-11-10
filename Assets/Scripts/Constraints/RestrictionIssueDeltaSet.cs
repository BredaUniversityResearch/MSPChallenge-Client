using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MSP2050.Scripts
{

	/// <summary>
	/// A delta set that specifies the changes in issues.
	/// </summary>
	public class RestrictionIssueDeltaSet
	{
		private HashSet<PlanIssueObject> addedIssues;
		private List<PlanIssueObject> removedIssues;

		public RestrictionIssueDeltaSet()
		{
			addedIssues = new HashSet<PlanIssueObject>(IssueObjectEqualityComparer.Instance);
			removedIssues = new List<PlanIssueObject>();
		}

		public RestrictionIssueDeltaSet(List<PlanIssueObject> a_existingIssues, Plan a_plan)
		{
			addedIssues = new HashSet<PlanIssueObject>(IssueObjectEqualityComparer.Instance);
			removedIssues = new List<PlanIssueObject>(a_existingIssues);
			
			foreach(PlanLayer planlayer in a_plan.PlanLayers)
			{
				if (planlayer.issues != null)
				{
					foreach (PlanIssueObject issue in planlayer.issues)
					{
						//New issues and removed issues are different objects (and new ones dont have ids), so find them using the IssueObjectEqualityComparer
						PlanIssueObject removedIssue = FindRemovedIssue(issue);
						if (removedIssue != null)
						{
							removedIssues.Remove(removedIssue);
						}
						else
						{
							addedIssues.Add(issue);
						}
					}
				}
			}
		}

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
}