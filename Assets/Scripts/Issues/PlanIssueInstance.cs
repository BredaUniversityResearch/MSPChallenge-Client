using UnityEngine;

namespace MSP2050.Scripts
{
	public class PlanIssueInstance: IssueInstance
	{
		WarningLabel m_label;
		PlanIssueObject m_planIssueData;

		public PlanIssueInstance()
		{
			m_label = IssueManager.Instance.CreateWarningLabelInstance();
			m_label.EnableInspectIssueButton(OnInspectIssue);
		}

		public void SetIssue(PlanIssueObject a_planIssueData)
		{
			m_planIssueData = a_planIssueData;
			m_label.LabelType(a_planIssueData.type);
			m_label.boxText.text = ConstraintManager.Instance.GetRestrictionMessage(a_planIssueData.RestrictionID);
			m_label.gameObject.transform.position = new Vector3(a_planIssueData.x, a_planIssueData.y, m_label.gameObject.transform.parent.position.z);
			SetLabelVisibility(true);
		}

		public override void Destroy()
		{
			Object.Destroy(m_label.gameObject);
			m_label = null;
			m_planIssueData = null;
		}

		private void OnInspectIssue()
		{
			IssueManager.Instance.ShowRelevantPlanLayersForIssue(m_planIssueData);
		}

		public override void SetLabelVisibility(bool visibility)
		{
			m_label.SetVisible(visibility);
		}

		public override bool IsLabelVisible()
		{
			return m_label.IsVisible();
		}

		public override void SetLabelScale(float scale)
		{
			m_label.SetScale(scale);
		}

		public override void CloseIfNotClickedOn()
		{
			m_label.CloseIfNotClickedOn();
		}
	}
}
