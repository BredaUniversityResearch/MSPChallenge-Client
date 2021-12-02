using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlanDetailsTabButton : MonoBehaviour {

	public PlanDetails.EPlanDetailsTab tab;

	void Awake()
	{
		GetComponent<Button>().onClick.AddListener(() =>
		{
			PlanDetails.instance.TabSelect(tab);
		});
	}
}
