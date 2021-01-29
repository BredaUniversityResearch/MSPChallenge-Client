using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlanWizardToggles : MonoBehaviour
{

	public int selected;
	public GameObject[] contents;
	public Image[] accents;

	void Start()
	{
		StartCoroutine("CenterWindow");
	}

	public void SetAccent(Color col)
	{
		for (int i = 0; i < accents.Length; i++)
		{
			accents[i].color = col;
		}
	}

	// Deprecated after moving distributions to the plan details
	public void Select(int choice)
	{
		selected = choice;

		StartCoroutine("CenterWindow");
	}

	// Resizes the window
	IEnumerator CenterWindow()
	{
		Canvas.ForceUpdateCanvases();
		InterfaceCanvas.Instance.planWizard.thisGenericWindow.CenterWindow();

		yield return new WaitForEndOfFrame();

		Canvas.ForceUpdateCanvases();
	}
}