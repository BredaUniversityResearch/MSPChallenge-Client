using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Interface.Notifications;
using TMPro;

public class PlanBar : MonoBehaviour
{
    public Image lockIcon, countryIcon;
	public GameObject changeIndicator;
	public GameObject actionRequiredIcon;
	//public Sprite viewSprite, editSprite;
	public TextMeshProUGUI title, date, issueIndicator;
	public Button foldButton, viewButton;
	public Toggle barToggle;
	public Transform foldButtonRect;
	public GameObject layersContainer;
	public GameObject viewFrame;
	public List<PlanLayerBar> planLayers;

	private Plan planRepresenting;
	private bool ignoreBarCallback;

	public void Initialise(Plan planRepresenting)
	{
		this.planRepresenting = planRepresenting;
		viewButton.onClick.AddListener(() =>
		{
			if (!Main.InEditMode && !Main.EditingPlanDetailsContent)
			{
				//if (planRepresenting.State == Plan.PlanState.DESIGN)
				//{
				//	PlanManager.RequestPlanLockForEditing(planRepresenting);
				//	PlanDetails.SelectPlan(planRepresenting);

				//}
				//else if (planRepresenting.InInfluencingState)
				//{
					PlanManager.ShowPlan(planRepresenting);
				//}
			}
		});

		barToggle.onValueChanged.AddListener((b) =>
		{
			if (!ignoreBarCallback)
			{
				if (PlanDetails.IsOpen)
					PlanManager.SetPlanUnseenChanges(planRepresenting, false);
				if (b)
					PlanDetails.SelectPlan(planRepresenting);
				else
					PlanDetails.SelectPlan(null);
			}
		});

		foldButton.onClick.AddListener(() =>
		{
			//ToggleContent(false);
			SetDropDown(!layersContainer.activeSelf);
		});
	}

	//public void ToggleContent(bool aToggled)
	//{
	//	if (aToggled)
	//	{
	//		SetDropDown(!layersContainer.activeSelf);
	//	}
	//	else
	//	{
	//		if (PlanDetails.GetSelectedPlan() == null)
	//		{
	//			SetDropDown(true);
	//		}
	//		else if (PlanDetails.GetSelectedPlan().ID == planRepresenting.ID)
	//		{
	//			SetDropDown(!layersContainer.activeSelf);
	//		}
	//	}
	//}

	public void SetViewEditButtonState(bool? edit)
	{
		viewButton.gameObject.SetActive(edit.HasValue);
		//if (edit.HasValue)
		//{
		//	viewEditButtonImage.sprite = edit.Value ? editSprite : viewSprite;
		//	TooltipManager.UpdateText(viewEditButtonImage.gameObject, edit.Value ? editTooltip : viewTooltip);
		//}
	}

	public void SetViewEditButtonInteractable(bool value)
	{
		//Don't allow the edit button to be interactable if we are in simulation and this is a plan that is still in design.
		if (GameState.CurrentState == GameState.PlanningState.Simulation)
		{
			if (!planRepresenting.InInfluencingState)
			{
				value = false;
			}
		}
		//Non GM players cant interact with plans during setup
		else if(GameState.CurrentState == GameState.PlanningState.Setup && !TeamManager.IsGameMaster)
		{
			value = false;
		}
		viewButton.interactable = value;
	}

	private void SetDropDown(bool aDown)
	{
		layersContainer.SetActive(aDown);

		//Rotates the little triangle that indicates a dropdown list
		Vector3 rot = foldButtonRect.eulerAngles;
		foldButtonRect.eulerAngles = aDown ? new Vector3(rot.x, rot.y, 0f) : new Vector3(rot.x, rot.y, 90f);
	}

	public void ToggleChangeIndicator(bool show)
	{
		changeIndicator.SetActive(show);
	}

	public void UpdateActionRequired()
	{
		bool actionRequired = false;
		if (planRepresenting.State == Plan.PlanState.APPROVAL)
		{
			EPlanApprovalState approvalState;
			if (planRepresenting.countryApproval.TryGetValue(TeamManager.CurrentUserTeamID, out approvalState))
			{
				if (approvalState == EPlanApprovalState.Maybe)
				{
					actionRequired = true;

				}
			}
		}

		if (actionRequired)
		{
			PlayerNotifications.AddApprovalActionRequiredNotification(planRepresenting);
		}
		else
		{
			PlayerNotifications.RemoveApprovalActionRequiredNotification(planRepresenting);
		}

		SetActionRequired(actionRequired);
	}

	private void SetActionRequired(bool actionIsRequired)
	{
		actionRequiredIcon.SetActive(actionIsRequired);
	}

	public void AddLayer(PlanLayerBar layer)
	{
		layer.transform.SetParent(layersContainer.transform, false);
		planLayers.Add(layer);
	}

	public void RemoveLayer(PlanLayerBar layer)
	{
		planLayers.Remove(layer);
	}

	public void SetIssue(ERestrictionIssueType issue)
	{
		switch (issue)
		{
		case ERestrictionIssueType.None:
		case ERestrictionIssueType.Info:
			issueIndicator.gameObject.SetActive(false);
			break;
		case ERestrictionIssueType.Warning:
			issueIndicator.gameObject.SetActive(true);
			issueIndicator.color = new Color(1f, 250f / 255, 49f / 255f);
			break;
		case ERestrictionIssueType.Error:
			issueIndicator.gameObject.SetActive(true);
			issueIndicator.color = new Color(1f, 84f / 255, 84f / 255f);
			break;
		}
	}

	public void SetViewFrameActivity(bool active)
	{
		viewFrame.SetActive(active);
	}

	public void SetPlanBarToggleValue(bool value)
	{
		ignoreBarCallback = true;
		barToggle.isOn = value;
		ignoreBarCallback = false;
	}

	public void SetPlanBarToggleInteractability(bool value)
	{
		barToggle.interactable = value;
	}
}