using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanLayerBar : MonoBehaviour/*, IPointerEnterHandler, IPointerExitHandler*/
{
	public TextMeshProUGUI title, issueIndicator;
	//public Image editIcon, textMask;
	//public Button barButton/*, oculusButton*/;
	//public GameObject buttonCoverGroup;

	//[HideInInspector]
	//public PlanLayer planLayerRepresenting;

	//public void SetBarAccent(Color col)
	//{
	//	ColorBlock block = barButton.colors;
	//	block.normalColor = new Color(col.r, col.g, col.b, 0.5f);
	//	barButton.colors = block;
	//}

	//public void BarButton() {
	//    if (Main.InEditMode)
	//            return; 

	//    if (!buttonCoverGroup.activeSelf) 
	//        toggle.isOn = !toggle.isOn;
	//    else {
	//        PlanBar plan = GetComponentInParent<PlanBar>();
	//        //plan.ClearLayerEdits();
	//        plan.EditLayer(this, true);
	//    }
	//}

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

	//public void OnPointerEnter(PointerEventData eventData)
	//{
	//	//Call show
	//	if (Main.CurrentlyEditingPlan != null && Main.CurrentlyEditingPlan.ID == planLayerRepresenting.Plan.ID)
	//		buttonCoverGroup.SetActive(true);

	//}

	//public void OnPointerExit(PointerEventData eventData)
	//{
	//	//Call hide
	//	buttonCoverGroup.SetActive(false);
	//}
}