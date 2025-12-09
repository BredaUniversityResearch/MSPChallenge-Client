using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class MonitorObjective : MonoBehaviour
	{
		[Header("Info")]
		[SerializeField] TextMeshProUGUI title;
		[SerializeField] TextMeshProUGUI summary;
		[SerializeField] TextMeshProUGUI date;
		[SerializeField] Image countryImage;
		[SerializeField] Toggle completedToggle = null; //GM / AM
		[SerializeField] Button cloneButton;
		[SerializeField] Button deleteButton;

		public int TeamId
		{
			get;
			private set;
		}

		public ObjectiveDetails ObjectiveDetails
		{
			get;
			private set;
		}
    
		private ObjectivesMonitor objectivesMonitor = null;

		private void Awake()
		{
			bool isManager = SessionManager.Instance.CurrentTeam.IsManager;
			if (isManager)
			{
				completedToggle.onValueChanged.AddListener(OnCompletedChanged);
			}
			completedToggle.gameObject.SetActive(isManager);

			deleteButton.onClick.AddListener(DeleteObjective);
			cloneButton.onClick.AddListener(CloneObjectiveAsNew);
		}

		public void SetObjectiveDetails(ObjectiveDetails details, ObjectivesMonitor owner)
		{
			objectivesMonitor = owner;
			title.text = details.title;
			summary.text = details.description;
			TeamId = details.appliesToCountry;

			ObjectiveDetails = details;
			completedToggle.isOn = details.completed;

			date.text = Util.MonthToYearText(details.deadlineMonth);
			Team team = SessionManager.Instance.FindTeamByID(details.appliesToCountry);
			countryImage.color = (team != null)? team.color : Color.white;
		}

		private void OnCompletedChanged(bool newCompletedState)
		{
			if (newCompletedState != ObjectiveDetails.completed)
			{
				ObjectiveDetails.completed = newCompletedState;
				NetworkForm form = new NetworkForm();
				form.AddField("objective_id", ObjectiveDetails.objectiveId);
				form.AddField("completed", newCompletedState ? 1 : 0);
				ServerCommunication.Instance.DoRequestForm(Server.SetObjectiveCompleted(), form);
			}

			objectivesMonitor.OnObjectiveUIStateChanged();
		}

		public void CloneObjectiveAsNew()
		{
			objectivesMonitor.CopyObjective(ObjectiveDetails);
		}

		public void DeleteObjective()
		{
			DialogBoxManager.instance.ConfirmationWindow("Confirm Action", "Are you sure you want to delete this objective?", () => { }, (UnityEngine.Events.UnityAction)(() => { DeleteObjectiveInternal((int)this.ObjectiveDetails.objectiveId); }));
		}

		private void DeleteObjectiveInternal(int objectiveId)
		{
			NetworkForm form = new NetworkForm();
			form.AddField("id", objectiveId);
			ServerCommunication.Instance.DoRequestForm(Server.DeleteObjective(), form);

			//Remove the objective from the UI immediately.
			objectivesMonitor.RemoveObjectiveFromUI(this);
		}

		public void CopyObjectiveDataFrom(MonitorObjective other)
		{
			if (title != null && other.title != null)
			{
				title.text = other.title.text;
			}
			if (summary != null && other.summary != null)
			{
				summary.text = other.summary.text;
			}
			TeamId = other.TeamId;
		}
	}
}