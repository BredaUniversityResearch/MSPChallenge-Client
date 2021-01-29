using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

class PlanIssuesEntry: MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI issueText = null;
	[SerializeField] private Button viewIssueButton = null;

	public void SetText(string text)
	{
		issueText.text = text;
	}

	public void DisableViewOnMapButton()
	{
		viewIssueButton.gameObject.SetActive(false);
	}

	public void SetViewOnMapClickedAction(UnityAction callbackAction)
	{
		viewIssueButton.onClick.AddListener(callbackAction);
	}
}

