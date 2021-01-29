
using UnityEngine;

public class ShippingIssueInstance: IssueInstance
{
	private WarningLabel sourceLabel;
	private WarningLabel destinationLabel;

	public ShippingIssueInstance(ShippingIssueObject issueData)
	{
		sourceLabel = IssueManager.instance.CreateWarningLabelInstance();
		destinationLabel = IssueManager.instance.CreateWarningLabelInstance();

		SubEntity sourceEntity = LayerManager.FindSubEntityByPersistentID(issueData.source_geometry_persistent_id);
		SubEntity destinationEntity = LayerManager.FindSubEntityByPersistentID(issueData.destination_geometry_persistent_id);

		SetupIssueLabel(sourceLabel, sourceEntity, issueData.message, CreateZoomToEntityCallback(destinationEntity, sourceLabel, destinationLabel));
		SetupIssueLabel(destinationLabel, destinationEntity, issueData.message, CreateZoomToEntityCallback(sourceEntity, destinationLabel, sourceLabel));
	}

	private void SetupIssueLabel(WarningLabel targetLabel, SubEntity targetEntity, string text, WarningLabel.InspectIssueCallback onInspectIssue)
	{
		if (targetEntity != null)
		{
			Vector3 pos = targetEntity.BoundingBox.center;
			pos.z = targetLabel.gameObject.transform.parent.position.z;
			targetLabel.gameObject.transform.position = pos;
		}

		targetLabel.LabelType(ERestrictionIssueType.Warning);
		targetLabel.boxText.text = text;
		targetLabel.EnableInspectIssueButton(onInspectIssue);
	}

	public override void Destroy()
	{
		if(sourceLabel != null && sourceLabel.gameObject != null)
			Object.Destroy(sourceLabel.gameObject);
		if (destinationLabel != null && destinationLabel.gameObject != null)
			Object.Destroy(destinationLabel.gameObject);
	}

	private WarningLabel.InspectIssueCallback CreateZoomToEntityCallback(SubEntity targetEntity, WarningLabel sourceLabel, WarningLabel destinationLabel)
	{
		WarningLabel.InspectIssueCallback result = null;
		if (targetEntity != null)
		{
			result = () =>
			{
				sourceLabel.SetLabelOpenState(false);
				CameraManager.Instance.cameraPan.StartAutoPan(targetEntity.BoundingBox.center, 0.35f);
				destinationLabel.SetLabelOpenState(true);
			};
		}
		return result;
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

	public override void SetLabelInteractability(bool interactability)
	{
		sourceLabel.SetInteractability(interactability);
		destinationLabel.SetInteractability(interactability);
	}
}
