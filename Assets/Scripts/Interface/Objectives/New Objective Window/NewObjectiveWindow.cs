using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class NewObjectiveWindow : MonoBehaviour
	{
		[SerializeField] GenericWindow thisGenericWindow = null;
		[SerializeField] Button acceptObjectiveButton = null;
		[SerializeField] TextMeshProUGUI errorMessageDisplay = null;

		[Header("Content links")]
		[SerializeField] CustomInputField title = null;
		[SerializeField] CustomInputField description = null;
		[SerializeField] EraDropdown deadline = null;
		[SerializeField] CountryDropdown countryDropdown = null;

		public void Awake()
		{
			acceptObjectiveButton.interactable = false;
			acceptObjectiveButton.onClick.AddListener(CreateNewObjective);
			countryDropdown.gameObject.SetActive(SessionManager.Instance.AreWeManager);
			title.onValueChanged.AddListener((s) => SetObjectiveValid());
			description.onValueChanged.AddListener((s) => SetObjectiveValid());
		}

		public void CreateNewObjective()
		{
			NetworkForm form = new NetworkForm();
			form.AddField("country", SessionManager.Instance.AreWeManager ? countryDropdown.GetSelectedCountryId() : SessionManager.Instance.CurrentUserTeamID);
			form.AddField("title", title.text);
			form.AddField("description", description.text);
			form.AddField("deadline", deadline.GetSelectedMonth());
			ServerCommunication.Instance.DoRequest(Server.SendObjective(), form);
			gameObject.SetActive(false);
		}

		public void OpenToNewObjective()
		{
			gameObject.SetActive(true);
			thisGenericWindow.CenterWindow();
			title.text = "";
			description.text = "";
			deadline.Reset();
			countryDropdown.Reset();
		}

		public void CloneObjective(ObjectiveDetails objectiveDetails)
		{
			gameObject.SetActive(true);
			title.text = objectiveDetails.title;
			description.text = objectiveDetails.description;
			deadline.SetSelectedMonth(objectiveDetails.deadlineMonth);
			countryDropdown.SetSelectedCountryId(objectiveDetails.appliesToCountry);
		}

		private void SetObjectiveValid()
		{
			acceptObjectiveButton.interactable = true;
			if (string.IsNullOrEmpty(title.text))
			{
				errorMessageDisplay.text = "The objective is missing a title";
				acceptObjectiveButton.interactable = false;
			}
			else if (string.IsNullOrEmpty(description.text))
			{
				errorMessageDisplay.text = "The objective is missing a description";
				acceptObjectiveButton.interactable = false;
			}
			errorMessageDisplay.gameObject.SetActive(!acceptObjectiveButton.interactable);
		}
	}
}