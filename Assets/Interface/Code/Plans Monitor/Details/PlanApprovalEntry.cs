using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ColourPalette;

public class PlanApprovalEntry : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI countryNameText;

	[SerializeField]
	Button yesButton, noButton, maybeButton;

	[SerializeField]
	Image yesIcon, noIcon, maybeIcon;

	[SerializeField]
	Image backgroundImage;
	[SerializeField]
	ColourAsset normalTextColour, activeTextColour;

	bool playerCanChangeApproval;
	Team currentTeam;
	public ApprovalButtonCallback approvalButtonCallback;
	public delegate void ApprovalButtonCallback(Team country, EPlanApprovalState newApproval);

	private void Start()
	{
		yesButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Approved));
		noButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Disapproved));
		maybeButton.onClick.AddListener(() => ApprovalButtonPressed(EPlanApprovalState.Maybe));
	}

	public void SetCallback(ApprovalButtonCallback callback)
	{
		approvalButtonCallback = callback;
	}

	public void SetContent(Team country, EPlanApprovalState state)
	{
		currentTeam = country;
		countryNameText.text = country.name;
		yesIcon.color = country.color;
		noIcon.color = country.color;
		maybeIcon.color = country.color;
		if(TeamManager.CurrentTeam.ID == country.ID)
		{
			playerCanChangeApproval = true;
			backgroundImage.enabled = true;
			countryNameText.color = activeTextColour.GetColour();
		}
		else
		{
			playerCanChangeApproval = TeamManager.IsGameMaster;
			backgroundImage.enabled = false;
			countryNameText.color = normalTextColour.GetColour();
		}
		SetApprovalState(state);
	}

	public void SetApprovalState(EPlanApprovalState state)
	{
		yesButton.gameObject.SetActive(playerCanChangeApproval && state != EPlanApprovalState.Approved);
		noButton.gameObject.SetActive(playerCanChangeApproval && state != EPlanApprovalState.Disapproved);
		maybeButton.gameObject.SetActive(playerCanChangeApproval && state != EPlanApprovalState.Maybe);
		yesIcon.gameObject.SetActive(state == EPlanApprovalState.Approved);
		noIcon.gameObject.SetActive(state == EPlanApprovalState.Disapproved);
		maybeIcon.gameObject.SetActive(state == EPlanApprovalState.Maybe);
	}

	void ApprovalButtonPressed(EPlanApprovalState state)
	{
		approvalButtonCallback.Invoke(currentTeam, state);
	}
}

