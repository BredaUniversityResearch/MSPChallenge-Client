using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class MonitorObjective : Objective
{
	private ObjectivesMonitor owningMonitor = null;
	private ObjectiveDetails objectiveDetails = null;
	public ObjectiveDetails ObjectiveDetails
	{
		get
		{
			return objectiveDetails;
		}
	}

	[Header("Rect")]
	public RectTransform thisRect;
	public RectTransform parentRect;

	[Header("Bar UI")]
	public Toggle toggle;
	[SerializeField]
	private Toggle completedToggle = null; //GM / AM
	[SerializeField]
	private TextMeshProUGUI completedLabel = null;		//All other clients that don't have permission to change the completed state.

	public Image foldIcon, country;
	public TextMeshProUGUI date;
    
	private bool pulsePending;

	private void Awake()
	{
		parentRect = (RectTransform)transform.parent.transform;

		bool isManager = TeamManager.CurrentTeam.IsManager;
		if (isManager)
		{
			completedToggle.onValueChanged.AddListener(OnCompletedChanged);
		}
		completedToggle.gameObject.SetActive(isManager);
		//completedLabel.gameObject.SetActive(!isManager);
	}

	public void SetOwner(ObjectivesMonitor owner)
	{
		owningMonitor = owner;
	}

	public override void SetObjectiveDetails(ObjectiveDetails details)
	{
		base.SetObjectiveDetails(details);
		objectiveDetails = details;
		completedToggle.isOn = details.completed;
        completedLabel.text = details.completed ? "Completed" : "In progress";

        date.text = Util.MonthToYearText(details.deadlineMonth);
		Team team = TeamManager.FindTeamByID(details.appliesToCountry);
		country.color = (team != null)? team.color : Color.white;
	}

	private void OnCompletedChanged(bool newCompletedState)
	{
		if (newCompletedState != objectiveDetails.completed)
		{
			objectiveDetails.completed = newCompletedState;
			NetworkForm form = new NetworkForm();
			form.AddField("objective_id", objectiveDetails.objectiveId);
			form.AddField("completed", newCompletedState ? 1 : 0);
			ServerCommunication.DoRequest(Server.SetObjectiveCompleted(), form);
		}

		owningMonitor.OnObjectiveUIStateChanged();
	}

	//Used as a callback from the Unity Editor.
	public void CloneObjectiveAsNew()
	{
		InterfaceCanvas.Instance.newObjectiveWindow.gameObject.SetActive(true);
		InterfaceCanvas.Instance.newObjectiveWindow.CloneObjective(objectiveDetails);
	}

	//Used as a callback from the Unity Editor.
	public void FoldIcon(bool dir)
	{
		RectTransform rectTrans = (RectTransform)foldIcon.transform;
		float rotation = (dir) ? 0f : 90f;
		rectTrans.DORotate(new Vector3(rectTrans.rotation.x, rectTrans.rotation.y, rotation), 0.1f);
	}

	//Used as a callback from the Unity Editor.
	public void DeleteObjective()
	{
		DialogBoxManager.instance.ConfirmationWindow("Confirm Action", "Are you sure you want to delete this objective?", () => { }, () => { DeleteObjectiveInternal(objectiveDetails.objectiveId); });
	}

	private void DeleteObjectiveInternal(int objectiveId)
	{
		NetworkForm form = new NetworkForm();
		form.AddField("id", objectiveId);
		ServerCommunication.DoRequest(Server.DeleteObjective(), form);

		//Remove the objective from the UI immediately.
		owningMonitor.RemoveObjectiveFromUI(this);
	}

	public void FocusObjective()
	{
		// Open the objectives monitor and display the menu bar button as active
		ObjectivesMonitor objectivesMonitor = InterfaceCanvas.Instance.objectivesMonitor;

		if (!InterfaceCanvas.Instance.objectivesMonitor.gameObject.activeSelf)
		{
			MenuBarToggle menuBarObjectivesMonitor = InterfaceCanvas.Instance.menuBarObjectivesMonitor;

			objectivesMonitor.gameObject.SetActive(true);
			menuBarObjectivesMonitor.toggle.isOn = true;
		}

		// Show objectives of this color
		//if (!allCountries.gameObject.activeSelf)
		//{
		//	objectivesMonitor.filterToggles[TeamId].toggle.isOn = true;
		//}

		// Tween to position and pulse
		RectTransform monitorRect = InterfaceCanvas.Instance.objectivesMonitor.thisRect;
		GameObject raycastBlocker = InterfaceCanvas.Instance.objectivesMonitor.raycastBlocker;

		LayoutRebuilder.ForceRebuildLayoutImmediate(monitorRect);
		Canvas.ForceUpdateCanvases();

		if (thisRect.anchoredPosition.y != -parentRect.anchoredPosition.y)
		{
			raycastBlocker.SetActive(true);

			Sequence seq = DOTween.Sequence();

			seq.Append(parentRect.DOAnchorPos(new Vector2(parentRect.anchoredPosition.x, -thisRect.anchoredPosition.y), 1f));
			seq.AppendCallback(() => toggle.isOn = true);
			seq.AppendCallback(() => PulseHeader());
			seq.AppendCallback(() => raycastBlocker.SetActive(false));

			pulsePending = true;
		}
		else
		{
			pulsePending = true;
			PulseHeader();
		}
	}

	private void PulseHeader(float duration = 1f)
	{
		if (pulsePending)
		{
			InterfaceCanvas.Instance.objectivesMonitor.raycastBlocker.SetActive(true);
			Color defaultCol = toggle.targetGraphic.color;
			toggle.targetGraphic.color = Color.white;
			toggle.targetGraphic.DOBlendableColor(defaultCol, duration)
				.OnComplete(() => InterfaceCanvas.Instance.objectivesMonitor.raycastBlocker.SetActive(false));
			pulsePending = false;
		}
	}
}