using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Objective : MonoBehaviour
{
    [Header("Info")]
	[SerializeField]
	protected TextMeshProUGUI title;
	[SerializeField]
    protected TextMeshProUGUI summary;
	public int TeamId
	{
		get;
		private set;
	}

	public void CopyObjectiveDataFrom(Objective other)
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
	
	public virtual void SetObjectiveDetails(ObjectiveDetails details)
	{
		title.text = details.title;
		summary.text = details.description;
		TeamId = details.appliesToCountry;
	}
}
