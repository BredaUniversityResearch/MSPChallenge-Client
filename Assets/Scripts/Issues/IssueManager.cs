using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class IssueManager : MonoBehaviour
{
	private static IssueManager ms_instance = null;

	public static IssueManager instance
	{
		get
		{
			if (ms_instance == null)
			{
				ms_instance = FindObjectOfType<IssueManager>();
				if (ms_instance == null)
				{
					Debug.LogWarning("Could not find singleton instance of IssueManager in the current scene.");
				}
			}
			return ms_instance;
		}
	}

	public delegate void OnIssueChanged(PlanLayer changedIssueLayer);

	private event OnIssueChanged issueChangedEvent;

	[SerializeField] 
	private WarningLabel warningLabel = null;

	[SerializeField]
	private GraphicRaycaster issueParentCanvasRaycaster = null;

	[SerializeField] 
	private Transform issueParentTransform = null;

	[SerializeField]
	private int maxShippingIssues = 30;

	private Dictionary<PlanLayer, List<PlanIssueInstance>> planIssuesByLayer = new Dictionary<PlanLayer, List<PlanIssueInstance>>();
	private Dictionary<int, ShippingIssueInstance> shippingIssueInstances = new Dictionary<int, ShippingIssueInstance>(); 

	private void OnDestroy()
	{
		DestroyAllShippingIssues();
		ms_instance = null;
	}

	public void InitialiseIssuesForPlanLayer(PlanLayer planLayer)
	{
		if (planIssuesByLayer.ContainsKey(planLayer))
		{
			RemoveIssuesForPlanLayer(planLayer, null);
		}
	}

	public WarningLabel CreateWarningLabelInstance()
	{
		float scale = GetIssueLabelScale();

		WarningLabel labelInstance = Instantiate(warningLabel);
		labelInstance.transform.SetParent(issueParentTransform, false);
		labelInstance.transform.localScale = new Vector3(scale, scale, 0);

		return labelInstance;
	}

	public ERestrictionIssueType GetMaximumSeverity(Plan plan)
	{
		ERestrictionIssueType maxSeverity = ERestrictionIssueType.None;
		for (int layerId = 0; layerId < plan.PlanLayers.Count; ++layerId)
		{
			PlanLayer layer = plan.PlanLayers[layerId];
			ERestrictionIssueType layerSeverity = GetMaximumSeverity(layer);
			if (layerSeverity < maxSeverity)
			{
				maxSeverity = layerSeverity;
			}
		}
		return maxSeverity;
	}

	/// <summary>
	/// Returns the maximum severity of the issues in the given plan layer.
	/// </summary>
	/// <param name="planLayer"></param>
	/// <returns></returns>
	public ERestrictionIssueType GetMaximumSeverity(PlanLayer planLayer)
	{
		List<PlanIssueInstance> issuesForPlan;
		if (!planIssuesByLayer.TryGetValue(planLayer, out issuesForPlan))
			return ERestrictionIssueType.None;

		ERestrictionIssueType result = ERestrictionIssueType.None;
		foreach (PlanIssueInstance issue in issuesForPlan)
		{
			if (issue.PlanIssueData.type < result)
			{
				result = issue.PlanIssueData.type;
			}
		}

		return result;
	}

	public IEnumerable<PlanIssueInstance> FindIssuesForPlan(Plan plan)
	{
		List<PlanIssueInstance> result = new List<PlanIssueInstance>(32);

		if (plan != null)
		{
			for (int i = 0; i < plan.PlanLayers.Count; ++i)
			{
				List<PlanIssueInstance> planLayerIssues;
				if (planIssuesByLayer.TryGetValue(plan.PlanLayers[i], out planLayerIssues))
				{
					result.AddRange(planLayerIssues);
				}
			}
		}
		return result;
	}

	public List<PlanIssueObject> FindIssueDataForPlan(Plan plan)
	{
		List<PlanIssueObject> result = new List<PlanIssueObject>(32);
		if (plan != null)
		{
			for (int i = 0; i < plan.PlanLayers.Count; ++i)
			{
				List<PlanIssueInstance> planLayerIssues;
				if (planIssuesByLayer.TryGetValue(plan.PlanLayers[i], out planLayerIssues))
				{
					for (int j = 0; j < planLayerIssues.Count; ++j)
					{
						result.Add(planLayerIssues[j].PlanIssueData);
					}
				}
			}
		}

		return result;
	}

	private bool HasError(PlanLayer planLayer)
	{
		return GetMaximumSeverity(planLayer) <= ERestrictionIssueType.Error;
	}

	public bool HasError(Plan plan)
	{
		foreach (PlanLayer planLayer in plan.PlanLayers)
		{
			if (HasError(planLayer))
			{
				return true;
			}
		}
		return false;
	}

	public void DeleteIssuesForPlanLayer(PlanLayer planLayer)
	{
		RemoveIssuesForPlanLayer(planLayer, null);
		planIssuesByLayer.Remove(planLayer);
	}

	private PlanIssueInstance AddPlanIssue(PlanLayer targetPlanLayer, PlanIssueObject planIssueData, RestrictionIssueDeltaSet deltaSet = null)
	{
		string restrictionText = ConstraintManager.GetRestrictionMessage(planIssueData.restriction_id);
		PlanIssueInstance planIssueInstance = FindIssueByData(targetPlanLayer, planIssueData);
		if (planIssueInstance == null)
		{
			PlanIssueObject issueData = planIssueData;
			if (deltaSet != null)
			{
				//So... If we add an issue that is a removed issue in the delta set, use the one that is in the delta set.
				//This will ensure that we use the proper field values (e.g. database_id) instead of nuking those.
				PlanIssueObject removedIssue = deltaSet.FindRemovedIssue(planIssueData);
				if (removedIssue != null)
				{
					issueData = removedIssue;
				}
			}

			planIssueInstance = new PlanIssueInstance(issueData, restrictionText);
			planIssueInstance.SetLabelVisibility(false);
			GetOrCreateIssueInstanceListForPlanLayer(targetPlanLayer).Add(planIssueInstance);

			OnIssueLayerChanged(targetPlanLayer);
			if (deltaSet != null)
			{
				deltaSet.IssueAdded(planIssueInstance.PlanIssueData);
			}
		}

		return planIssueInstance;
	}

	private List<PlanIssueInstance> GetOrCreateIssueInstanceListForPlanLayer(PlanLayer planLayer)
	{
		List<PlanIssueInstance> result;
		if (!planIssuesByLayer.TryGetValue(planLayer, out result))
		{
			result = new List<PlanIssueInstance>();
			planIssuesByLayer.Add(planLayer, result);
		}
		return result;
	}

	private PlanIssueInstance FindIssueByData(PlanLayer planLayer, PlanIssueObject planIssueData)
	{
		PlanIssueInstance result = null;
		List<PlanIssueInstance> issuesForPlan;
		if (planIssuesByLayer.TryGetValue(planLayer, out issuesForPlan))
		{
			for (int i = 0; i < issuesForPlan.Count; ++i)
			{
				PlanIssueObject rhs = issuesForPlan[i].PlanIssueData;
				if (rhs.IsSameIssueAs(planIssueData))
				{
					result = issuesForPlan[i];
					break;
				}
			}
		}
		return result;
	}

	private static float GetIssueLabelScale()
	{
		return VisualizationUtil.DisplayScale / 120.0f * InterfaceCanvas.Instance.canvas.scaleFactor;
	}

	public void RescaleIssues()
	{
		foreach (var kvp in planIssuesByLayer)
		{
			RescaleIssueList(kvp.Value);
		}
		RescaleIssueList(shippingIssueInstances.Values);
	}

	private void RescaleIssueList<ISSUE_TYPE>(IEnumerable<ISSUE_TYPE> list)
		where ISSUE_TYPE : IssueInstance
	{
		float scale = GetIssueLabelScale();
		// do a check if the plan has any planlayers active
		foreach (ISSUE_TYPE issue in list)
		{
			if (issue.IsLabelVisible())
			{
				issue.SetLabelScale(scale);
			}
		}
	}

	public void SetIssueVisibility(bool visible)
	{
		issueParentTransform.gameObject.SetActive(visible);
	}

	public bool GetIssueVisibility()
	{
		return issueParentTransform.gameObject.activeSelf;
	}

	public void HideIssuesForPlan(PlanLayer planLayer)
	{
		SetIssueVisibilityForPlanLayer(planLayer, false);
	}

	public void ShowIssuesForPlan(PlanLayer planLayer)
	{
		SetIssueVisibilityForPlanLayer(planLayer, true);
		RescaleIssues();
	}

	public void SetIssueVisibilityForPlan(Plan plan, bool visible)
	{
		for (int i = 0; i < plan.PlanLayers.Count; ++i)
		{
			SetIssueVisibilityForPlanLayer(plan.PlanLayers[i], visible);
		}

		if (visible)
		{
			RescaleIssues();
		}
	}

	private void SetIssueVisibilityForPlanLayer(PlanLayer planLayer, bool visible)
	{
		List<PlanIssueInstance> issuesForPlan;
		planIssuesByLayer.TryGetValue(planLayer, out issuesForPlan);
		if (issuesForPlan != null)
		{
			for (int i = 0; i < issuesForPlan.Count; i++)
			{
				issuesForPlan[i].SetLabelVisibility(visible);
			}
		}
	}

	public void RemoveIssuesForPlan(Plan plan, RestrictionIssueDeltaSet deltaSet)
	{
		for (int i = 0; i < plan.PlanLayers.Count; ++i)
		{
			RemoveIssuesForPlanLayer(plan.PlanLayers[i], deltaSet);
		}

		RemoveIssuesForPlanInRemotePlans(plan, deltaSet);
	}

	private void DestroyIssuesForPlan(IList<PlanIssueInstance> issueInstances, RestrictionIssueDeltaSet deltaSet)
	{
		for (int i = 0; i < issueInstances.Count; i++)
		{
			PlanIssueInstance planIssueInstance = issueInstances[i];

			if (deltaSet != null)
			{
				deltaSet.IssueRemoved(planIssueInstance.PlanIssueData);
			}

			planIssueInstance.Destroy();
		}
	}

	private void RemoveIssueForPlanIssueObject(PlanLayer targetPlanLayer, PlanIssueObject planIssue)
	{
		List<PlanIssueInstance> issuesForPlan;
		planIssuesByLayer.TryGetValue(targetPlanLayer, out issuesForPlan);
		if (issuesForPlan != null)
		{
			for (int i = issuesForPlan.Count - 1; i >= 0; --i)
			{
				PlanIssueInstance planIssueInstance = issuesForPlan[i];
				if (planIssueInstance.PlanIssueData.issue_database_id == planIssue.issue_database_id)
				{
					planIssueInstance.Destroy();
					issuesForPlan.RemoveAt(i);
				}
			}
		}
		OnIssueLayerChanged(targetPlanLayer);
	}

	private void RemoveIssuesForPlanLayer(PlanLayer planLayer, RestrictionIssueDeltaSet deltaSet)
	{
		List<PlanIssueInstance> issuesForPlan;
		planIssuesByLayer.TryGetValue(planLayer, out issuesForPlan);
		if (issuesForPlan != null)
		{
			DestroyIssuesForPlan(issuesForPlan, deltaSet);
			issuesForPlan.Clear();
		}
		OnIssueLayerChanged(planLayer);
	}

	private void RemoveIssuesForPlanInRemotePlans(Plan sourcePlan, RestrictionIssueDeltaSet deltaSet)
	{
		foreach (var kvp in planIssuesByLayer)
		{
			if (kvp.Key.Plan == sourcePlan)
			{
				continue;
			}

			for (int issueIndex = kvp.Value.Count - 1; issueIndex >= 0; --issueIndex)
			{
				PlanIssueInstance planIssue = kvp.Value[issueIndex];
				if (planIssue.PlanIssueData.source_plan_id == sourcePlan.ID)
				{
					if (deltaSet != null)
					{
						deltaSet.IssueRemoved(planIssue.PlanIssueData);
					}
					kvp.Value.RemoveAt(issueIndex);
				}
			}
		}
	}

	protected void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			foreach (var kvp in planIssuesByLayer)
			{
				for (int i = 0; i < kvp.Value.Count; i++)
				{
					if (kvp.Value[i].IsLabelVisible())
					{
						kvp.Value[i].CloseIfNotClickedOn();
					}
				}
			}

			foreach(var kvp in shippingIssueInstances)
			{
				kvp.Value.CloseIfNotClickedOn();
			}
		}
	}

	public void ImportNewIssues(MultiLayerRestrictionIssueCollection issueCollection, RestrictionIssueDeltaSet deltaSet)
	{
		foreach (var issueLayer in issueCollection.GetIssues())
		{
			for (int i = 0; i < issueLayer.Value.Count; ++i)
			{
				PlanIssueObject planIssue = issueLayer.Value[i];
				AddPlanIssue(issueLayer.Key, planIssue, deltaSet);
			}
		}
	}

	public void OnIssuesReceived(WarningObject warningUpdateData)
	{
		foreach (PlanIssueObject planIssue in warningUpdateData.plan_issues)
		{
			OnPlanIssueReceivedFromServer(planIssue);
		}

		if (warningUpdateData.shipping_issues.Count > 0)
		{
			UpdateShippingIssues(warningUpdateData.shipping_issues);
		}
	}

	private void OnPlanIssueReceivedFromServer(PlanIssueObject planIssue)
	{
		PlanLayer targetPlanLayer = PlanManager.GetPlanLayer(planIssue.plan_layer_id);
		//This can happen if someone has an error on a plan layer and deletes that layer from their plan.
		//The plan layer will only not exist on re-connect so this should be safe to ignore then.
		if (targetPlanLayer != null)
		{
			if (planIssue.active)
			{
				PlanIssueInstance planIssueInstance = AddPlanIssue(targetPlanLayer, planIssue);
				//Overwrite the ID we received back from te server to ensure it's up to date. This might be a redundant operation, but it might also not be a redundant operation when the issue is newly created.
				planIssueInstance.PlanIssueData.issue_database_id = planIssue.issue_database_id;
			}
			else
			{
				RemoveIssueForPlanIssueObject(targetPlanLayer, planIssue);
			}
		}
	}

	public void SubscribeToIssueChangedEvent(OnIssueChanged callbackDelegate)
	{
		issueChangedEvent += callbackDelegate;
	}

	public void UnsubscribeFromIssueChangedEvent(OnIssueChanged callbackDelegate)
	{
		issueChangedEvent -= callbackDelegate;
	}

	private void OnIssueLayerChanged(PlanLayer targetPlanLayer)
	{
		if (issueChangedEvent != null)
		{
			issueChangedEvent(targetPlanLayer);
		}
	}

	public void ShowRelevantPlanLayersForIssue(PlanIssueInstance planIssueData)
	{
		KeyValuePair<AbstractLayer, AbstractLayer> layerData = ConstraintManager.GetRestrictionLayersForRestrictionId(planIssueData.PlanIssueData.restriction_id);

		if (layerData.Key != null)
		{
			ToggleRelevantPlanIssueLayer(PlanManager.planViewing, layerData.Key);
		}
		if (layerData.Value != null)
		{
			ToggleRelevantPlanIssueLayer(PlanManager.planViewing, layerData.Value);
		}
	}

	private void ToggleRelevantPlanIssueLayer(Plan currentPlan, AbstractLayer layer)
	{
		if (!LayerManager.LayerIsVisible(layer))
		{
			LayerManager.ShowLayer(layer);
		}
		else
		{
			if (layer.Toggleable && currentPlan != null)
			{
				//Only toggle layers off if they aren't in the current plan.
				if (currentPlan.PlanLayers.Find(obj => obj.BaseLayer == layer) == null)
				{
					LayerManager.HideLayer(layer);
				}
			}
		}
	}

	private void CreateNewShippingIssue(ShippingIssueObject issueObject)
	{
		if (shippingIssueInstances.Count < maxShippingIssues)
		{
			ShippingIssueInstance issueInstance = new ShippingIssueInstance(issueObject);
			shippingIssueInstances.Add(issueObject.warning_id, issueInstance);
		}
	}

	private void DestroyAllShippingIssues()
	{
		foreach(ShippingIssueInstance issueInstance in shippingIssueInstances.Values)
		{
			issueInstance.Destroy();
		}
		shippingIssueInstances.Clear();
	}

	private void UpdateShippingIssues(List<ShippingIssueObject> shippingIssues)
	{
		for (int i = 0; i < shippingIssues.Count; ++i)
		{
			ShippingIssueObject issue = shippingIssues[i];
			ShippingIssueInstance existingInstance;
			if (shippingIssueInstances.TryGetValue(issue.warning_id, out existingInstance))
			{
				if (!issue.active)
				{
					existingInstance.Destroy();
					shippingIssueInstances.Remove(issue.warning_id);
				}
			}
			else if (issue.active)
			{
				CreateNewShippingIssue(issue);
			}
		}
	}

	public void SetIssueInteractability(bool interactability)
	{
		if (issueParentCanvasRaycaster != null)
		{
			issueParentCanvasRaycaster.enabled = interactability;
		}
		else
		{
			Debug.LogError("Parent canvas raycaster not assigned in the IssueManager");
		}
	}

	public void SetIssuesForPlan(Plan plan, List<PlanIssueObject> issuesBackup)
	{
		RemoveIssuesForPlan(plan, null);
		foreach (PlanIssueObject planIssue in issuesBackup)
		{
			PlanLayer layer = PlanManager.GetPlanLayer(planIssue.plan_layer_id);
			AddPlanIssue(layer, planIssue);
		}
	}
}
