using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlansGroupBar : MonoBehaviour
{

    public TextMeshProUGUI title;
    public GameObject plansContainerOuter;
    public Transform plansContainerInner;
    public Button foldButton;
    public RectTransform foldButtonRect;
	public AddTooltip tooltip;

    void Awake()
    {
        foldButton.onClick.AddListener(ToggleContent);
    }
    
    public void ToggleContent()
    {
		plansContainerOuter.SetActive(!plansContainerOuter.activeSelf);

        Vector3 rot = foldButtonRect.eulerAngles;
        foldButtonRect.eulerAngles = (rot.z == 0) ? new Vector3(rot.x, rot.y, 90f) : new Vector3(rot.x, rot.y, 0f);
    }

    public void AddPlan(PlanBar plan)
    {
        plan.transform.SetParent(plansContainerInner, false);
    }
}