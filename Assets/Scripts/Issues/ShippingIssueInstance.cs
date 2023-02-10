
using UnityEngine;

namespace MSP2050.Scripts
{
	public class ShippingIssueInstance: IssueInstance
	{
		private WarningLabel sourceLabel;
		private WarningLabel destinationLabel;

		SubEntity m_sourceSubentity;
		SubEntity m_destinationSubentity;

		public ShippingIssueInstance()
		{
			sourceLabel = IssueManager.Instance.CreateWarningLabelInstance();
			destinationLabel = IssueManager.Instance.CreateWarningLabelInstance();
			sourceLabel.EnableInspectIssueButton(InspectSource);
			destinationLabel.EnableInspectIssueButton(InspectDestination);
		}

		public void SetIssue(ShippingIssueObject a_issue)
		{
			m_sourceSubentity = LayerManager.Instance.FindSubEntityByPersistentID(a_issue.source_geometry_persistent_id);
			m_destinationSubentity = LayerManager.Instance.FindSubEntityByPersistentID(a_issue.destination_geometry_persistent_id);

			SetupIssueLabel(sourceLabel, m_sourceSubentity, a_issue.message);
			SetupIssueLabel(destinationLabel, m_destinationSubentity, a_issue.message);
			SetLabelVisibility(true);
		}

		private void SetupIssueLabel(WarningLabel targetLabel, SubEntity targetEntity, string text)
		{
			if (targetEntity != null)
			{
				Vector3 pos = targetEntity.BoundingBox.center;
				pos.z = targetLabel.gameObject.transform.parent.position.z;
				targetLabel.gameObject.transform.position = pos;
			}

			targetLabel.LabelType(ERestrictionIssueType.Warning);
			targetLabel.boxText.text = text;
		}

		void InspectSource()
		{
			CameraManager.Instance.cameraPan.StartAutoPan(m_sourceSubentity.BoundingBox.center, 0.35f);
		}

		void InspectDestination()
		{
			CameraManager.Instance.cameraPan.StartAutoPan(m_sourceSubentity.BoundingBox.center, 0.35f);
		}

		public override void Destroy()
		{
			if(sourceLabel != null && sourceLabel.gameObject != null)
				Object.Destroy(sourceLabel.gameObject);
			if (destinationLabel != null && destinationLabel.gameObject != null)
				Object.Destroy(destinationLabel.gameObject);
		}

		public override void SetLabelVisibility(bool visibility)
		{
			sourceLabel.SetVisible(visibility);
			destinationLabel.SetVisible(visibility);
		}

		public override bool IsLabelVisible()
		{
			return sourceLabel.IsVisible() || destinationLabel.IsVisible();
		}

		public override void SetLabelScale(float scale)
		{
			sourceLabel.SetScale(scale);
			destinationLabel.SetScale(scale);
		}

		public override void CloseIfNotClickedOn()
		{
			sourceLabel.CloseIfNotClickedOn();
			destinationLabel.CloseIfNotClickedOn();
		}
	}
}
