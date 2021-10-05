using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlansGroupBar : MonoBehaviour
{

	public TextMeshProUGUI title;
	public GameObject plansContainerOuter;
	public RectTransform plansContainerOuterRect;
	public Transform plansContainerInner;
	public Button foldButton;
	public RectTransform foldButtonRect;
	public AddTooltip tooltip;

	void Awake()
	{
		foldButton.onClick.AddListener(ToggleContent);
		plansContainerOuterRect = plansContainerOuter.GetComponent<RectTransform>();
		foldButtonRect.gameObject.SetActive(plansContainerOuterRect.rect.height != 0);
	}

	public void ToggleContent()
	{
		// Ignore empty content
		if (plansContainerOuterRect.rect.height == 0)
			return;

		plansContainerOuter.SetActive(!plansContainerOuter.activeSelf);

		Vector3 rot = foldButtonRect.eulerAngles;
		foldButtonRect.eulerAngles = (rot.z == 0) ? new Vector3(rot.x, rot.y, 90f) : new Vector3(rot.x, rot.y, 0f);
	}

	public void AddPlan(PlanBar plan)
	{
		plan.transform.SetParent(plansContainerInner, false);
		foldButtonRect.gameObject.SetActive(plansContainerOuterRect.rect.height != 0);
	}
}