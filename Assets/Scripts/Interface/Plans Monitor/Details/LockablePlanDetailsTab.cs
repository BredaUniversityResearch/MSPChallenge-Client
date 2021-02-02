using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LockablePlanDetailsTab : PlanDetailsTab
{
	protected Plan lockedPlan;
	public Plan LockedPlan => lockedPlan;

	public override void UpdateTabAvailability()
	{
		base.UpdateTabAvailability();
		if (isActive)
		{
			SetEditContentButtonEnabled(planDetails.CanStartEditingContent);
		}
	}

	/// <summary>
	/// Should always be called when the plan to be edited has already been locked.
	/// </summary>
	protected virtual void BeginEditing(Plan plan)
	{
		lockedPlan = plan;
		PlanManager.ShowPlan(plan);
		Main.EditingPlanDetailsContent = true;
		SetAcceptChangesButtonEnabled(true);
		PlansMonitor.RefreshPlanButtonInteractablity();
		InterfaceCanvas.Instance.activePlanWindow.UpdateEditButtonActivity();
	}

	/// <summary>
	/// Should always be called when the plan to be edited has already been unlocked.
	/// </summary>
	protected virtual void StopEditing()
	{
		lockedPlan = null;
		SetAcceptChangesButtonEnabled(false);
		Main.EditingPlanDetailsContent = false;
		PlansMonitor.RefreshPlanButtonInteractablity();
		PlanDetails.UpdateTabContent();
		InterfaceCanvas.Instance.activePlanWindow.UpdateEditButtonActivity();
	}

	protected virtual void SubmitChangesAndUnlock()
	{ }

	public virtual void CancelChangesAndUnlock()
	{
		SetConfirmCancelChangesInteractable(false);
	}

	protected virtual void HandleChangesSubmissionSuccess(BatchRequest batch)
	{
		StopEditing();
		InterfaceCanvas.HideNetworkingBlocker();
		SetConfirmCancelChangesInteractable(false);
	}

	protected void HandleChangesSubmissionFailure(BatchRequest batch)
	{
		InterfaceCanvas.HideNetworkingBlocker();
		DialogBoxManager.instance.NotificationWindow("Submitting data failed", "There was an error when submitting the plan's changes to the server. Please try again or see the error log for more information.", null);
	}

	protected virtual string ContentName { get; }

	public void SetAcceptChangesButtonEnabled(bool enabled)
	{
		planDetails.changesConfirmCancelBox.SetActive(enabled);
		if (enabled)
		{
			SetConfirmCancelChangesInteractable(true);
			planDetails.changesConfirmCancelText.text = $"Confirm {ContentName} changes?";

			planDetails.changesConfirmButton.onClick.RemoveAllListeners();
			planDetails.changesConfirmButton.onClick.AddListener(SubmitChangesAndUnlock);

			planDetails.changesCancelButton.onClick.RemoveAllListeners();
			planDetails.changesCancelButton.onClick.AddListener(() =>
			{
				UnityEngine.Events.UnityAction lb = () => { };
				UnityEngine.Events.UnityAction rb = () =>
				{
					CancelChangesAndUnlock();
				};
				DialogBoxManager.instance.ConfirmationWindow("Cancel changes", "Are you sure you want to cancel changes and revert to the previous values?", lb, rb);
			});
		}
	}

	protected void SetConfirmCancelChangesInteractable(bool value)
	{
		planDetails.changesConfirmButton.interactable = value;
		planDetails.changesCancelButton.interactable = value;
	}

	protected void SetEditContentButtonEnabled(bool enabled)
	{
		planDetails.editTabContentBox.SetActive(enabled);
		if (enabled)
		{
			planDetails.editTabContentText.text = $"Edit the plan's {ContentName}";

			planDetails.editTabContentButton.onClick.RemoveAllListeners();
			planDetails.editTabContentButton.onClick.AddListener(() =>
				{
					if (planDetails.SelectedPlan == null)
						return;

					Main.PreventPlanAndTabChange = true;
					PlansMonitor.RefreshPlanButtonInteractablity();

					planDetails.SelectedPlan.AttemptLock(
						(plan) =>
						{
							Main.PreventPlanAndTabChange = false;
							BeginEditing(plan);
						},
						(plan) => {
							Main.PreventPlanAndTabChange = false;
							PlansMonitor.RefreshPlanButtonInteractablity();
						});
				});
		}
	}
}

