using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanDetailsTabDescription : LockablePlanDetailsTab
{
	[SerializeField]
	CustomInputField descriptionInputField = null;
	[SerializeField]
    GameObject readOnlyTextContainer = null;
    [SerializeField]
    TextMeshProUGUI readOnlyText = null;

    private string oldDescription;

	public string Description {
		get { return readOnlyText.text; }
        set
        {
            readOnlyText.text = string.IsNullOrEmpty(value) ? "" : value;
        }
	}

	protected override string ContentName => "description";
	protected override PlanDetails.EPlanDetailsTab tabType => PlanDetails.EPlanDetailsTab.Description;

	public override void UpdateTabContent()
	{
		if (!isActiveAndEnabled || lockedPlan != null)
			return;
		Description = planDetails.SelectedPlan.Description;
		emptyContentOverlay.SetActive(DescriptionEmpty);
	}

	protected override void BeginEditing(Plan plan)
	{
		base.BeginEditing(plan);

		emptyContentOverlay.SetActive(false);
		oldDescription = Description;
		descriptionInputField.text = Description;
		descriptionInputField.gameObject.SetActive(true);
		readOnlyTextContainer.SetActive(false);
		descriptionInputField.Select();
	}

	protected override void StopEditing()
	{
		oldDescription = null;
		descriptionInputField.gameObject.SetActive(false);
		readOnlyTextContainer.SetActive(true);
		emptyContentOverlay.SetActive(DescriptionEmpty);

		base.StopEditing();
	}

	protected override void SubmitChangesAndUnlock()
	{
		BatchRequest batch = new BatchRequest();

		lockedPlan.SetDescription(descriptionInputField.text, batch);

		lockedPlan.AttemptUnlock(batch);
		InterfaceCanvas.ShowNetworkingBlocker();
		batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
	}

	protected override void HandleChangesSubmissionSuccess(BatchRequest batch)
	{
		Description = descriptionInputField.text;
		base.HandleChangesSubmissionSuccess(batch);
	}

	public override void CancelChangesAndUnlock()
	{
		base.CancelChangesAndUnlock();

		descriptionInputField.text = oldDescription;
		Description = oldDescription;

		lockedPlan.AttemptUnlock();
		StopEditing();
	}

	private bool DescriptionEmpty => string.IsNullOrEmpty(Description) || Description == " ";
}
