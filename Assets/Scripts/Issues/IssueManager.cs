using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class IssueManager : MonoBehaviour
	{
		private static IssueManager m_instance = null;

		public static IssueManager Instance
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = FindObjectOfType<IssueManager>();
					if (m_instance == null)
					{
						Debug.LogWarning("Could not find singleton instance of IssueManager in the current scene.");
					}
				}
				return m_instance;
			}
		}

		[SerializeField] WarningLabel warningLabel = null;
		[SerializeField] GraphicRaycaster issueParentCanvasRaycaster = null;
		[SerializeField] Transform issueParentTransform = null;
		[SerializeField] int maxShippingIssues = 30;

		List<PlanIssueInstance> m_issueInstances = new List<PlanIssueInstance>();
		Dictionary<int, ShippingIssueInstance> m_shippingIssueInstances = new Dictionary<int, ShippingIssueInstance>(); 

		private void OnDestroy()
		{
			DestroyAllShippingIssues();
			m_instance = null;
		}

		public WarningLabel CreateWarningLabelInstance()
		{
			float scale = GetIssueLabelScale();
			WarningLabel labelInstance = Instantiate(warningLabel, issueParentTransform);
			labelInstance.transform.localScale = new Vector3(scale, scale, 0);
			return labelInstance;
		}

		public void HidePlanIssueInstances()
		{
			foreach(PlanIssueInstance issue in m_issueInstances)
			{
				issue.SetLabelInteractability(false);
			}
		}

		public void SetIssueInstancesToPlan(Plan a_plan)
		{
			int nextIssueIndex = 0;
			if(a_plan.PlanLayers != null)
			{
				foreach(PlanLayer planlayer in a_plan.PlanLayers)
				{
					if(planlayer.issues != null)
					{
						foreach(PlanIssueObject issue in planlayer.issues)
						{
							if(nextIssueIndex < m_issueInstances.Count)
							{
								m_issueInstances[nextIssueIndex].SetIssue(issue);
							}
							else
							{
								PlanIssueInstance newInstance = new PlanIssueInstance();
								newInstance.SetIssue(issue);
								m_issueInstances.Add(newInstance);
							}
							nextIssueIndex++;
						}
					}
				}
			}
			for(; nextIssueIndex < m_issueInstances.Count; nextIssueIndex++)
			{
				m_issueInstances[nextIssueIndex].SetLabelVisibility(false);
			}
			RescaleIssues();
		}

		private static float GetIssueLabelScale()
		{
			return VisualizationUtil.Instance.DisplayScale / 120.0f * InterfaceCanvas.Instance.canvas.scaleFactor;
		}

		public void RescaleIssues()
		{
			RescaleIssueList(m_issueInstances);
			RescaleIssueList(m_shippingIssueInstances.Values);
		}

		private void RescaleIssueList<ISSUE_TYPE>(IEnumerable<ISSUE_TYPE> list)
			where ISSUE_TYPE : IssueInstance
		{
			float scale = GetIssueLabelScale();
			foreach (ISSUE_TYPE issue in list)
			{
				if (issue.IsLabelVisible())
				{
					issue.SetLabelScale(scale);
				}
			}
		}

		public bool IssueVisibility
		{
			set { issueParentTransform.gameObject.SetActive(value); }
			get { return issueParentTransform.gameObject.activeSelf; }
		}

		protected void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				foreach (PlanIssueInstance issue in m_issueInstances)
				{
					if (issue.IsLabelVisible())
					{
						issue.CloseIfNotClickedOn();
					}
				}

				foreach(var kvp in m_shippingIssueInstances)
				{
					kvp.Value.CloseIfNotClickedOn();
				}
			}
		}

		public void ShowRelevantPlanLayersForIssue(PlanIssueObject planIssueData)
		{
			KeyValuePair<AbstractLayer, AbstractLayer> layerData = ConstraintManager.Instance.GetRestrictionLayersForRestrictionId(planIssueData.restriction_id);

			if (layerData.Key != null)
			{
				ToggleRelevantPlanIssueLayer(PlanManager.Instance.planViewing, layerData.Key);
			}
			if (layerData.Value != null)
			{
				ToggleRelevantPlanIssueLayer(PlanManager.Instance.planViewing, layerData.Value);
			}
		}

		private void ToggleRelevantPlanIssueLayer(Plan currentPlan, AbstractLayer layer)
		{
			if (!LayerManager.Instance.LayerIsVisible(layer))
			{
				LayerManager.Instance.ShowLayer(layer);
			}
			else
			{
				//if (layer.Toggleable && currentPlan != null)
				//{
				//	//Only toggle layers off if they aren't in the current plan.
				//	if (currentPlan.PlanLayers.Find(obj => obj.BaseLayer == layer) == null)
				//	{
				//		LayerManager.Instance.HideLayer(layer);
				//	}
				//}
			}
		}

		private void CreateNewShippingIssue(ShippingIssueObject issueObject)
		{
			if (m_shippingIssueInstances.Count < maxShippingIssues)
			{
				ShippingIssueInstance issueInstance = new ShippingIssueInstance();
				issueInstance.SetIssue(issueObject);
				m_shippingIssueInstances.Add(issueObject.warning_id, issueInstance);
			}
		}

		private void DestroyAllShippingIssues()
		{
			foreach(ShippingIssueInstance issueInstance in m_shippingIssueInstances.Values)
			{
				issueInstance.Destroy();
			}
			m_shippingIssueInstances.Clear();
		}

		public void UpdateShippingIssues(List<ShippingIssueObject> shippingIssues)
		{
			for (int i = 0; i < shippingIssues.Count; ++i)
			{
				ShippingIssueObject issue = shippingIssues[i];
				ShippingIssueInstance existingInstance;
				if (m_shippingIssueInstances.TryGetValue(issue.warning_id, out existingInstance))
				{
					if (!issue.active)
					{
						existingInstance.Destroy();
						m_shippingIssueInstances.Remove(issue.warning_id);
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
	}
}
