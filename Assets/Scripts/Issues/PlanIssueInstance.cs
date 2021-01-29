using UnityEngine;

public class PlanIssueInstance: IssueInstance
{
	private WarningLabel label;
	public PlanIssueObject PlanIssueData { get; private set; }
	public string Text { get; private set; }

	public PlanIssueInstance(PlanIssueObject planIssueData, string text)
	{
		label = IssueManager.instance.CreateWarningLabelInstance();
		Text = text;
		PlanIssueData = planIssueData;
		SetupIssueLabel(new Vector3(planIssueData.x, planIssueData.y, 0.0f), text, planIssueData.type);
	}

	public override void Destroy()
	{
		Object.Destroy(label.gameObject);
		label = null;
		PlanIssueData = null;
	}

	private void SetupIssueLabel(Vector3 pos, string text, ERestrictionIssueType issueType)
	{
		label.LabelType(issueType);
		label.boxText.text = text;
		pos.z = label.gameObject.transform.parent.position.z;
		label.gameObject.transform.position = pos;
		label.EnableInspectIssueButton(OnInspectIssue);
	}

	private void OnInspectIssue()
	{
		IssueManager.instance.ShowRelevantPlanLayersForIssue(this);
	}

	public override void SetLabelVisibility(bool visibility)
	{
		label.SetVisible(visibility);
	}

	public override void SetLabelInteractability(bool interactability)
	{
		label.SetInteractability(interactability);
	}

	public override bool IsLabelVisible()
	{
		return label.IsVisible();
	}

	public override void SetLabelScale(float scale)
	{
		label.SetScale(scale);
	}

	public override void CloseIfNotClickedOn()
	{
		label.CloseIfNotClickedOn();
	}
}
