using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlansMonitor : MonoBehaviour
{
	public static PlansMonitor instance;
	public GenericWindow thisGenericWindow;
	public PlansTimeline timeline;
	public PlansList plansList;
	public PlanDetails planDetails;
	public PlansWindowMinMax plansMinMax;
	public MenuBarToggle plansMonitorToggle;
	public ToolbarCounter plansMonitorToolbarCounter;
	public float fadedTransparency;
	public float mouseFadeInDistance = 1f;

	private static PlansList plansListStatic;
	private CanvasGroup canvasGroup;
	private bool faded;
	private Vector3 mousePosOnFade;

	void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		instance = this;
		if (thisGenericWindow == null)
			thisGenericWindow = GetComponent<GenericWindow>();

		plansListStatic = plansList;
    }

	protected void Start()
	{
		instance = this;
        plansMinMax.Minimize();
		this.gameObject.SetActive(false);
	}

	void Update()
	{
		if (faded && Vector3.Distance(mousePosOnFade, Input.mousePosition) > mouseFadeInDistance)
		{
			faded = false;
			canvasGroup.alpha = 1f;
			HighlightManager.instance.RemoveHighlight();
		}
	}

    private void OnDisable()
    {
        if (InterfaceCanvas.Instance.menuBarPlansMonitor.toggle.isOn)
        {
            InterfaceCanvas.Instance.menuBarPlansMonitor.toggle.isOn = false;
        }
    }

    // move all these functions to UIManager
    public static void AddPlan(Plan plan)
	{
		plansListStatic.AddPlanToList(plan);
	}

	public static void SetLockIcon(Plan plan, bool value)
	{
		plansListStatic.SetLockIcon(plan, value);
	}

	public static void SetViewPlanFrameState(Plan plan, bool state)
	{
		plansListStatic.SetViewPlanFrameState(plan, state);
	}

	public static void SetPlanBarToggleState(Plan plan, bool state)
	{
		plansListStatic.SetPlanBarToggleState(plan, state);
	}

	public static void UpdatePlan(Plan plan, bool nameChanged, bool timeChanged, bool stateChanged)
	{
		plansListStatic.UpdatePlan(plan, nameChanged, timeChanged, stateChanged);
	}

	public static void AddPlanLayer(Plan plan, PlanLayer planLayer)
	{
		plansListStatic.AddPlanLayer(plan, planLayer);
	}

	public static void SetPlanUnseenChanges(Plan plan, bool unseenChanges)
	{
		plansListStatic.SetPlanUnseenChanges(plan, unseenChanges);	

	}

	public static void RemovePlanLayer(Plan plan, PlanLayer planLayer)
	{
		plansListStatic.RemovePlanLayer(plan, planLayer);
	}

	public static void SetAllPlanBarInteractable(bool value)
	{
		plansListStatic.SetAllButtonInteractable(value);
	}

	public static void RefreshPlanButtonInteractablity()
	{
		plansListStatic.RefreshPlanBarInteractablityForAllPlans();
		PlanDetails.UpdateButtonInteractability();
	}
		
	public void FadeAndHighlightUntilMouseMove()
	{
		faded = true;
		canvasGroup.alpha = fadedTransparency;
		mousePosOnFade = Input.mousePosition;
	}

	public static void SetUnseenChangesCounter(int value)
	{
		instance.plansMonitorToolbarCounter.SetValue(value);
	}
}