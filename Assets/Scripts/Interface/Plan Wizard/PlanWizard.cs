using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using Newtonsoft.Json.Linq;

public class PlanWizard : MonoBehaviour
{
    public enum UpdateReach { NoWhere = 0, MinYear = 1, Year = 2, MinMonth = 3, Month = 4 }
    public enum ErrorCode { None, NoName, NoSelection, InvalidDate }

    public GenericWindow thisGenericWindow;

    private static PlanWizard singleton;

    public static PlanWizard instance
    {
        get
        {
            if (singleton == null)
                singleton = (PlanWizard)FindObjectOfType(typeof(PlanWizard));
            return singleton;
        }
    }

    [Header("Plan Name")]
    public CustomInputField planName;

    [Header("Plan Layers")]
    public Transform planLayerLocation;
    public Transform planSelectionLocation;
    public GameObject planCategoryPrefab, planSubCategoryPrefab, planLayerPrefab;

    [Header("Toggles")]
    public Toggle shippingToggle;
    public Toggle energyToggle;
    public Toggle ecologyToggle;

    [Header("Covers")]
    public GameObject layersCover;
    public GameObject activitiesCover;

    [Header("Timeline")]
    public GameObject startPlanArea;
    public GameObject timeSelectArea;
    public Toggle startPlanToggle;
    public CustomDropdown monthDropdown, yearDropdown;
    public TextMeshProUGUI constructionStartText, constructionDurationText;
    private int finishTime, minTimeSelectable = 10000;  //In game time (0-479)
    private int finishMonth, minMonthSelectable;//0-11
    private int finishYear, minYearSelectable;  //0-39
    private int maxConstructionTime;//months of construction time
    private bool ignoreTimeUICallback, dropDownsFilled;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;
    public GameObject feedbackParent;

    [Header("Buttons")]
    public Button closeButton;
    public Button acceptButton;

    [Header("Plan Window Height")]
    public RectTransform planLayersWindow;
    int planLayersChildCount;
    public RectTransform planSelectionWindow;
    //public int planSelectionChildCount;
    public float planWizardPlansHeight = 250f;

    List<PlanLayerBase> planLayers, planSelections;
    bool settingUpPlan;
    private Dictionary<AbstractLayer, PlanLayer_Layer> planLayerLayers;
    private PlanLayer_Layer cablePlanLayerGreenLeft, cablePlanLayerGreyLeft, cablePlanLayerGreenRight, cablePlanLayerGreyRight;
    private Plan editingPlan;
    private bool isEnergyPlan;

    protected void Awake()
    {
        if (thisGenericWindow == null)
            thisGenericWindow = GetComponent<GenericWindow>();

        planLayers = new List<PlanLayerBase>();
        planSelections = new List<PlanLayerBase>();
        planLayerLayers = new Dictionary<AbstractLayer, PlanLayer_Layer>();

        CheckForErrors();
        createCategories();
        SetupButtonCallbacks();

        //Set up toggle callbacks
        energyToggle.onValueChanged.AddListener((b) =>
        {
            if (b)
                isEnergyPlan = true;
            else
            {
                int energyLayersGreen = amountOfEnergyLayersInPlan(out var energyLayersGrey);
                isEnergyPlan = energyLayersGrey + energyLayersGreen > 0;
            }
        });
		closeButton.onClick.AddListener(() => CloseWindow());
        monthDropdown.onValueChanged.AddListener(MonthDropdownChanged);
        yearDropdown.onValueChanged.AddListener(YearDropdownChanged);
		startPlanToggle.onValueChanged.AddListener((value) =>
		{
			if (!settingUpPlan)
				timeSelectArea.SetActive(!value);
		});
		SetupActivityToggles();
	}

	public void OnEnable()
    {
        transform.SetAsLastSibling();
        editingPlan = null;
        CheckForErrors();
        this.thisGenericWindow.CreateModalBackground();
    }

    private void createCategories()
    {
        foreach (var kvp in LayerManager.GetCategorySubcategories())
        {
            // Create category
            string category = kvp.Key;

            bool isCategoryCreated = false;
            // Create subcategories
            foreach (string subcategory in kvp.Value)
            {
                var loadedLayers = LayerManager.GetLoadedLayers(category, subcategory);
                bool isSubCategoryCreated = false;

                foreach (AbstractLayer layer in loadedLayers)
                {
                    if ((layer.Selectable && layer.Editable && layer.GetType() != typeof(RasterLayer)) && !layer.FileName.StartsWith("_PLAYAREA"))
                    {
                        if (!isCategoryCreated)
                        {
                            isCategoryCreated = true;
                            //CreateCategoryPlan(category);
                            CreateCategoryPlan(LayerManager.MakeCategoryDisplayString(category));
                        }
                        if (!isSubCategoryCreated)
                        {
                            isSubCategoryCreated = true;
                            CreateSubCategoryPlan(LayerManager.MakeCategoryDisplayString(subcategory), LayerInterface.GetIconStatic(subcategory));
                        }

                        CreateLayerPlan(layer);
                    }
                }
            }
        }

        CheckForErrors();
    }

    public void CloseWindow(bool openPlansMonitor = false)
    {
        if(openPlansMonitor)
            InterfaceCanvas.Instance.menuBarPlansMonitor.toggle.isOn = true;

		gameObject.SetActive(false);
    }

    private void OnDisable()
    {
		if (editingPlan != null)
		{
			editingPlan.AttemptUnlock();
			editingPlan = null;
		}

		InterfaceCanvas.Instance.menuBarPlanWizard.toggle.isOn = false;
        thisGenericWindow.DestroyModalWindow();
    }

    private void createPlan()
    {
        List<AbstractLayer> layersInThisPlan = new List<AbstractLayer>();
        foreach (KeyValuePair<AbstractLayer, PlanLayer_Layer> kvp in planLayerLayers)
        {
            if (kvp.Value.toggle.isOn)
            {
                layersInThisPlan.Add(kvp.Key);
            }
        }

        string type = string.Format("{0},{1},{2}", isEnergyPlan ? 1 : 0, ecologyToggle.isOn ? 1 : 0, shippingToggle.isOn ? 1 : 0);

        Plan.SendPlan(planName.text, layersInThisPlan, startPlanToggle.isOn ? -1 : finishTime, type, energyToggle.isOn);
        CloseWindow(true);
    }

	private int GetNewPlanStartDate()
	{
		return startPlanToggle.isOn? -1 : finishTime;
	}

	private void OnEditPlanAcceptClicked()
	{
		string notificationText = null;
		BatchRequest batch = new BatchRequest();
		MultiLayerRestrictionIssueCollection resultIssues = new MultiLayerRestrictionIssueCollection();

		bool timeMovedToPast = GetNewPlanStartDate() < editingPlan.StartTime;
		if (timeMovedToPast)
		{
			ConstraintManager.CheckTypeUnavailableConstraints(editingPlan, GetNewPlanStartDate(), resultIssues);
			//TODO: undo these issue changes if change submission fails

			if (resultIssues.HasIssues())
			{
				notificationText = "This plan contains entity types that are not yet available at the new implementation time. Do you want to submit your changes?";
			}
		}

		if (notificationText != null)
		{
			UnityAction onSubmitAction = () => {
				ApplyAndSubmitPlanIssues(resultIssues, batch);
				SubmitEditPlanChanges(batch);
			};

			DialogBoxManager.instance.ConfirmationWindow("Confirm", notificationText, null, onSubmitAction, "Cancel", "Submit");
		}
		else
		{
			SubmitEditPlanChanges(batch);
		}
	}

	private void ApplyAndSubmitPlanIssues(MultiLayerRestrictionIssueCollection issues, BatchRequest batch)
	{
		RestrictionIssueDeltaSet deltaSet = new RestrictionIssueDeltaSet();
		IssueManager.instance.ImportNewIssues(issues, deltaSet);
		deltaSet.SubmitToServer(batch);
	}

	private void SubmitEditPlanChanges(BatchRequest batch)
    {
		bool needsEnergyError = false;

        //Rename Plan
        editingPlan.RenamePlan(planName.text, batch);

        // Change date plan
        bool timeChanged = GetNewPlanStartDate() != editingPlan.StartTime;
		if (timeChanged)
		{
			editingPlan.ChangePlanDate(GetNewPlanStartDate(), batch);
		}

		//Only time change and renaming are allowed for DELETED plans
        if (editingPlan.State != Plan.PlanState.DELETED)
        {
            // Check which of the selected layers are contained within the plan
            List<AbstractLayer> layersInThisPlan = new List<AbstractLayer>();
            foreach (PlanLayer planLayer in editingPlan.PlanLayers)
                if (planLayerLayers.ContainsKey(planLayer.BaseLayer))
                    layersInThisPlan.Add(planLayer.BaseLayer);

            List<AbstractLayer> selectedLayers = new List<AbstractLayer>();

            // Adding of layers
            foreach (KeyValuePair<AbstractLayer, PlanLayer_Layer> kvp in planLayerLayers)
            {
                if (kvp.Value.toggle.isOn)
                {
                    selectedLayers.Add(kvp.Key); // this is all of the selected items
                    if (!layersInThisPlan.Contains(kvp.Key))// Check if there are any new Layers
                    {
                        Debug.Log("Adding layer");
                        editingPlan.AddNewPlanLayer(kvp.Key, batch);//then add them to the plan
                    }
                }
            }

            // Removing of layers
            List<AbstractLayer> layersToRemove = layersInThisPlan.Except(selectedLayers).ToList(); // all layers that are not selected but are in the plan

            bool seperatelyRemoveGreenCables = LayerManager.energyCableLayerGreen != null && !layersToRemove.Contains(LayerManager.energyCableLayerGreen);
            bool seperatelyRemoveGreyCables = LayerManager.energyCableLayerGrey != null && !layersToRemove.Contains(LayerManager.energyCableLayerGrey);
            Dictionary<int, List<EnergyLineStringSubEntity>> network = null;
            if (seperatelyRemoveGreenCables)
                network = LayerManager.energyCableLayerGreen.GetNodeConnectionsForPlan(editingPlan);
            if (seperatelyRemoveGreyCables)
                network = LayerManager.energyCableLayerGrey.GetNodeConnectionsForPlan(editingPlan, network);
            bool energyLayersRemoved = false;
            foreach (AbstractLayer layer in layersToRemove)
            {
                //Remove dependant stuff for energy layers
                if (layer.IsEnergyLayer())
                {
                    energyLayersRemoved = true;
                    if (seperatelyRemoveGreenCables || seperatelyRemoveGreyCables)
					{
						PlanLayer currentPlanLayer = editingPlan.GetPlanLayerForLayer(layer);
                        for (int i = 0; i < currentPlanLayer.GetNewGeometryCount(); ++i)
						{
							Entity t = currentPlanLayer.GetNewGeometryByIndex(i);
                            SubEntity subEnt = t.GetSubEntity(0);
                            if (network.ContainsKey(subEnt.GetDatabaseID()))
                                foreach (EnergyLineStringSubEntity cable in network[subEnt.GetDatabaseID()])
                                    cable.SubmitDelete(batch);//Connections will be removed up to 4 times
                        }
                    }
                }
                //Removes planlayer from plan and all geom on it
                //Removes all connections, sockets, sources and output for geom on the layer
                editingPlan.RemovePlanLayer(editingPlan.GetPlanLayerForLayer(layer), batch);
            }
            if (energyLayersRemoved)
                needsEnergyError = true;

            bool typeChanged = false;
            if (isEnergyPlan && !editingPlan.energyPlan)//Enabled
            {
                needsEnergyError = true;
                typeChanged = true;
            }
            else if (!isEnergyPlan && editingPlan.energyPlan)//Disabled
            {
                typeChanged = true;
                JObject dataObject = new JObject();
				dataObject.Add("plan", editingPlan.ID);
				batch.AddRequest(Server.DeleteEnergyFromPlan(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
            Plan.SetEnergyDistribution(editingPlan.ID, energyToggle.isOn, batch);

            if (ecologyToggle.isOn && !editingPlan.ecologyPlan) //Enabled
            {
                typeChanged = true;
                //Creates initial fishing distribution based on previous plan and sends it to the server
                editingPlan.fishingDistributionDelta = new FishingDistributionDelta();
            }
            else if (!ecologyToggle.isOn && editingPlan.ecologyPlan) //Disabled
            {
                typeChanged = true;
                JObject dataObject = new JObject();
				dataObject.Add("plan", editingPlan.ID);
				batch.AddRequest(Server.DeleteFishingFromPlan(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
			}
            if (shippingToggle.isOn && !editingPlan.shippingPlan)//Enabled
            {
                typeChanged = true;
            }
            if (!shippingToggle.isOn && editingPlan.shippingPlan)//Disabled
            {
                typeChanged = true;
            }
            if (typeChanged)
            {
                string type = string.Format("{0},{1},{2}", isEnergyPlan ? 1 : 0, ecologyToggle.isOn ? 1 : 0, shippingToggle.isOn ? 1 : 0);
                Plan.SetPlanType(editingPlan.ID, type, batch);
            }

            HashSet<int> countriesAffectedByRemovedGrids = new HashSet<int>();
            if (isEnergyPlan && editingPlan.energyPlan)
                foreach (EnergyGrid grid in PlanManager.GetEnergyGridsBeforePlan(editingPlan, EnergyGrid.GridColor.Either))
                    if (editingPlan.removedGrids.Contains(grid.persistentID))
                        foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.energyDistribution.distribution)
                            if (!countriesAffectedByRemovedGrids.Contains(countryAmount.Key))
                                countriesAffectedByRemovedGrids.Add(countryAmount.Key);

			Dictionary<int, EPlanApprovalState> newApproval = editingPlan.CalculateRequiredApproval(countriesAffectedByRemovedGrids);
            editingPlan.SubmitRequiredApproval(batch, newApproval);
        }

        if (isEnergyPlan && (needsEnergyError || timeChanged))
        {
            editingPlan.AddSystemMessage("Removal of energy layers, making a plan an energy plan or changing the time of an energy plan necessitate the recalculation of energy grids.");
            editingPlan.SubmitEnergyError(true, true, batch);
        }

		editingPlan.AttemptUnlock(batch);
		InterfaceCanvas.ShowNetworkingBlocker();
		batch.ExecuteBatch(HandleChangesSubmissionSuccess, HandleChangesSubmissionFailure);
	}

	private void HandleChangesSubmissionSuccess(BatchRequest batch)
	{
		editingPlan = null;
		CloseWindow(true);
		InterfaceCanvas.HideNetworkingBlocker();
	}

	private void HandleChangesSubmissionFailure(BatchRequest batch)
	{
		InterfaceCanvas.HideNetworkingBlocker();
		DialogBoxManager.instance.NotificationWindow("Submitting data failed", "There was an error when submitting the plan's changes to the server. Please try again or see the error log for more information.", null);
	}

	private void SetupButtonCallbacks()
    {
        acceptButton.onClick.RemoveAllListeners();
        // This doesnt know which phase of the plan wizard its in tho :(
		acceptButton.onClick.AddListener(() =>
		{
			if (editingPlan == null)
			{
				createPlan();
			}
			else
			{
				OnEditPlanAcceptClicked();
			}
		});
    }

	public void SetToPlan(Plan plan)
    {
        settingUpPlan = true;
        if (plan != null)
        {
            editingPlan = plan;
            planName.text = plan.Name;
            maxConstructionTime = 0;

            //Disable all
            foreach (KeyValuePair<AbstractLayer, PlanLayer_Layer> kvp in planLayerLayers)
            {
                kvp.Value.toggle.isOn = false;
            }
            isEnergyPlan = false;

            //Enable layers in plan
            foreach (PlanLayer planLayer in plan.PlanLayers)
            {
                if (planLayer.BaseLayer.AssemblyTime > maxConstructionTime)
                    maxConstructionTime = planLayer.BaseLayer.AssemblyTime;

                planLayerLayers[planLayer.BaseLayer].toggle.isOn = true;
                if (planLayer.BaseLayer.IsEnergyLayer())
                {
                    isEnergyPlan = true;
                }
            }

            UpdateMinAndSetTime(plan.StartTime);
            if (TeamManager.AreWeGameMaster && !GameState.GameStarted)
            {
                startPlanArea.SetActive(true);
                startPlanToggle.isOn = plan.StartTime < 0;
                timeSelectArea.SetActive(!startPlanToggle.isOn);
            }
            else
            {
                timeSelectArea.SetActive(true);
                startPlanArea.SetActive(false);
                startPlanToggle.isOn = false;
            }

            //Set the cable layer toggles to right interactability
            int energyLayersGreen = amountOfEnergyLayersInPlan(out var energyLayersGrey);
            if (cablePlanLayerGreenLeft != null)
                cablePlanLayerGreenLeft.toggle.interactable = energyLayersGreen <= 1;
            if (cablePlanLayerGreyLeft != null)
                cablePlanLayerGreyLeft.toggle.interactable = energyLayersGrey <= 1;

            //Set toggles for plan types
            if (!isEnergyPlan)
            {
                isEnergyPlan = plan.energyPlan;
            }

            energyToggle.isOn = plan.altersEnergyDistribution;

            ecologyToggle.isOn = plan.ecologyPlan;
            shippingToggle.isOn = plan.shippingPlan;
            layersCover.SetActive(plan.State == Plan.PlanState.DELETED);
            activitiesCover.SetActive(plan.State == Plan.PlanState.DELETED);
        }
        else
        {
            planName.text = "";
            maxConstructionTime = 0;
            UpdateMinAndSetTime(0);

            foreach (var kvp in planLayerLayers)
            {
                kvp.Value.toggle.isOn = false;
            }

            timeSelectArea.SetActive(true);
            startPlanArea.SetActive(TeamManager.AreWeGameMaster && !GameState.GameStarted);
            startPlanToggle.isOn = false;

            if (cablePlanLayerGreenLeft != null)
            {
                cablePlanLayerGreenLeft.toggle.interactable = true;
                cablePlanLayerGreenRight.toggle.interactable = true;
            }

            if (cablePlanLayerGreyLeft != null)
            {
                cablePlanLayerGreyLeft.toggle.interactable = true;
                cablePlanLayerGreyRight.toggle.interactable = true;
            }

            energyToggle.isOn = false;
            ecologyToggle.isOn = false;
            shippingToggle.isOn = false;
            isEnergyPlan = false;
            layersCover.SetActive(false);
            activitiesCover.SetActive(false);
        }
        settingUpPlan = false;
        CheckForErrors();
    }

    public void NewPlan()
    {
        SetToPlan(null);
    }

    private void ResizeHeight()
    {
        if (planLayerLocation.childCount != planLayersChildCount)
        {
            planLayersWindow.GetComponent<LayoutElement>().preferredHeight = planWizardPlansHeight;
            planSelectionWindow.GetComponent<LayoutElement>().preferredHeight = planWizardPlansHeight;
            planLayersChildCount = planLayerLocation.transform.childCount;
        }
    }

    /// <summary>
    /// Creates and returns a plan category in both columns
    /// </summary>
    public PlanLayer_Category[] CreateCategoryPlan(string name)
    {
        PlanLayer_Category[] planRow = new PlanLayer_Category[2];

        // Layers Side
        GameObject go = Instantiate(planCategoryPrefab, planLayerLocation, false);
        PlanLayer_Category plan = go.GetComponent<PlanLayer_Category>();
        planLayers.Add(go.GetComponent<PlanLayerBase>());
        plan.title.text = name;

        planRow[0] = plan;

        // Selection Side
        go = Instantiate(planCategoryPrefab, planSelectionLocation, false);
        plan = go.GetComponent<PlanLayer_Category>();
        planSelections.Add(go.GetComponent<PlanLayerBase>());
        plan.title.text = name;

        planRow[1] = plan;

        ResizeHeight();

        return planRow;
    }

    /// <summary>
    /// Creates and returns a plan sub-category in both columns
    /// </summary>
    public PlanLayer_SubCategory[] CreateSubCategoryPlan(string name, Sprite sprite)
    {
        PlanLayer_SubCategory[] planRow = new PlanLayer_SubCategory[2];

        // Layers Side
        GameObject go = Instantiate(planSubCategoryPrefab, planLayerLocation, false);
        PlanLayer_SubCategory plan = go.GetComponent<PlanLayer_SubCategory>();
        plan.icon.sprite = sprite;
        planLayers.Add(go.GetComponent<PlanLayerBase>());
        plan.title.text = name;

        planRow[0] = plan;

        // Selection Side
        go = Instantiate(planSubCategoryPrefab, planSelectionLocation, false);
        plan = go.GetComponent<PlanLayer_SubCategory>();
        plan.icon.sprite = sprite;
        planSelections.Add(go.GetComponent<PlanLayerBase>());
        plan.title.text = name;

        planRow[1] = plan;

        ResizeHeight();

        return planRow;
    }

    /// <summary>
    /// Creates and returns a plan layer in both columns
    /// </summary>
    public PlanLayer_Layer[] CreateLayerPlan(AbstractLayer layer)
    {
        AbstractLayer tmpLayer = layer;
        PlanLayer_Layer[] planRow = new PlanLayer_Layer[2];

        // Layers Side
        GameObject go = Instantiate(planLayerPrefab, planLayerLocation, false);
        PlanLayer_Layer plan = go.GetComponent<PlanLayer_Layer>();
        planLayers.Add(go.GetComponent<PlanLayerBase>());
        plan.title.text = layer.GetShortName();

        planRow[0] = plan;
        if (layer.IsEnergyLineLayer())
        {
            if(layer.greenEnergy)
                cablePlanLayerGreenLeft = plan;
            else
                cablePlanLayerGreyLeft = plan;
        }

        // Selection Side
        go = Instantiate(planLayerPrefab, planSelectionLocation, false);
        plan = go.GetComponent<PlanLayer_Layer>();
        planSelections.Add(go.GetComponent<PlanLayerBase>());
        plan.title.text = layer.GetShortName();

        if (layer.IsEnergyLineLayer())
        {
	        if (layer.greenEnergy)
		        cablePlanLayerGreenRight = plan;
	        else
				cablePlanLayerGreyRight = plan;
        }

		planRow[1] = plan;

        // Add to dictionary
        planLayerLayers.Add(layer, planRow[0]);

        // When a plan is toggled on the left side
        planRow[0].toggle.onValueChanged.AddListener((value) =>
        {
            // The slected layer wil now appear in the right side of the window
            planRow[1].gameObject.SetActive(value);
            planRow[1].toggle.isOn = value;

            if (!settingUpPlan)
            {
                // If it is toggled on
                if (value)
                {

                    // it will select the layer and add it to the timeline below
                    if (tmpLayer.IsEnergyLayer())
                    {
                        if (!tmpLayer.IsEnergyLineLayer())
                        {
                            if (tmpLayer.greenEnergy)
                            {
                                cablePlanLayerGreenLeft.toggle.isOn = true;
                                cablePlanLayerGreenLeft.toggle.interactable = false;
                                cablePlanLayerGreenRight.toggle.interactable = false;
                            }
                            else
                            {
                                cablePlanLayerGreyLeft.toggle.isOn = true;
								cablePlanLayerGreyRight.toggle.interactable = false;
                            }
                        }
                        isEnergyPlan = true;
                    }
                    if (maxConstructionTime < tmpLayer.AssemblyTime)
                    {
                        maxConstructionTime = tmpLayer.AssemblyTime;
                        updateMinSelectableTime();
                    }

                }
                else
                {
                    // this will unselect the layer and remove it from the timelne below
                    if (tmpLayer.IsEnergyLayer())
                    {
                        int energyLayersGreen = amountOfEnergyLayersInPlan(out var energyLayersGrey);
                        if (energyLayersGreen + energyLayersGrey == 0)
                        {
                            isEnergyPlan = energyToggle.isOn;
                            //energyToggle.interactable = true;
                        }
                        else 
                        {   //Last remaining green cable layer
	                        if (energyLayersGreen == 1 && !tmpLayer.IsEnergyLineLayer())
	                        {
		                        cablePlanLayerGreenLeft.toggle.interactable = true;
		                        cablePlanLayerGreenRight.toggle.interactable = true;
	                        }

	                        if (energyLayersGrey == 1 && !tmpLayer.IsEnergyLineLayer())
	                        {
	                            cablePlanLayerGreyLeft.toggle.interactable = true;
	                            cablePlanLayerGreyRight.toggle.interactable = true;
							}
                        }
                    }
                    //Find new max construction time
                    if (maxConstructionTime == tmpLayer.AssemblyTime)
                    {
                        maxConstructionTime = 0;
                        foreach (KeyValuePair<AbstractLayer, PlanLayer_Layer> kvp in planLayerLayers)
                            if (kvp.Value.toggle.isOn && kvp.Key.AssemblyTime > maxConstructionTime)
                                maxConstructionTime = kvp.Key.AssemblyTime;
                        updateMinSelectableTime();
                    }
                }
            }
            if (!settingUpPlan)
                CheckForErrors();
        });

        // Disabling the toggle on the right side will toggle the left one off
        planRow[1].toggle.onValueChanged.AddListener((value) =>
        {
            if (planRow[0].toggle.isOn != value)
                planRow[0].toggle.isOn = value;
        });

        // default is off
        planRow[0].toggle.isOn = false;
		planRow[1].gameObject.SetActive(false);
		ResizeHeight();
        return planRow;
    }

    private int amountOfLayersInPlan()
    {
        int layersInThisPlan = 0;
        foreach (var kvp in planLayerLayers)
        {
            if (kvp.Value.toggle.isOn)
            {
                layersInThisPlan++;
            }
        }

        return layersInThisPlan;
    }

    private int amountOfLayersInPlanPerCategory(string category)
    {
        int layersInThisPlan = 0;
        foreach (var kvp in planLayerLayers)
        {
            if (kvp.Value.toggle.isOn && kvp.Key.Category == category)
            {
                layersInThisPlan++;
            }
        }

        return layersInThisPlan;
    }

    private int amountOfLayersInPlanPerSubcategory(string subcategory)
    {
        int layersInThisPlan = 0;
        foreach (var kvp in planLayerLayers)
        {
            if (kvp.Value.toggle.isOn && kvp.Key.SubCategory == subcategory)
            {
                layersInThisPlan++;
            }
        }

        return layersInThisPlan;
    }

    private int amountOfEnergyLayersInPlan(out int grey)
    {
        grey = 0;
        int green = 0;
        foreach (var kvp in planLayerLayers)
        {
            if (kvp.Value.toggle.isOn && kvp.Key.IsEnergyLayer())
            {
                if (kvp.Key.greenEnergy)
                    green++;
                else
                    grey++;
            }
        }

        return green;
    }

    public void CheckForErrors()
    {
        if (planName.text.Length == 0)
        {
            DisplayFeedback(ErrorCode.NoName);
        }
        else if (amountOfLayersInPlan() < 1 && !shippingToggle.isOn && !isEnergyPlan && !ecologyToggle.isOn)
        {
            DisplayFeedback(ErrorCode.NoSelection);
        }
		else if (minTimeSelectable >= Main.MspGlobalData.session_end_month)
		{
			DisplayFeedback(ErrorCode.InvalidDate);
		}
		else
        {
            DisplayFeedback(ErrorCode.None);
        }

        // first do subcategory because the category depends on this
        // enables or disables the category and subcategory texts
        foreach (PlanLayerBase planLayerBase in planSelections)
        {
            if (planLayerBase is PlanLayer_SubCategory)
            {
                int total = amountOfLayersInPlanPerSubcategory(planLayerBase.title.text);

                if (total > 0)
                {
                    if (!planLayerBase.gameObject.activeSelf)
                    {
                        planLayerBase.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (planLayerBase.gameObject.activeSelf)
                    {
                        planLayerBase.gameObject.SetActive(false);
                    }
                }
            }

            if (planLayerBase is PlanLayer_Category)
            {
                int total = amountOfLayersInPlanPerCategory(planLayerBase.title.text);

                if (total > 0)
                {
                    if (!planLayerBase.gameObject.activeSelf)
                    {
                        planLayerBase.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if (planLayerBase.gameObject.activeSelf)
                    {
                        planLayerBase.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void DisplayFeedback(ErrorCode code)
    {
        switch (code)
        {
            case ErrorCode.None:
                feedbackParent.gameObject.SetActive(false);
                acceptButton.interactable = true;
				monthDropdown.interactable = true;
				yearDropdown.interactable = true;
				break;
            case ErrorCode.NoName:
                feedbackParent.gameObject.SetActive(true);
                feedbackText.text = "This plan does not have a name yet.";
                acceptButton.interactable = false;
                break;
            case ErrorCode.NoSelection:
                feedbackParent.gameObject.SetActive(true);
                feedbackText.text = "No layers or activities have been selected for this plan.";
                acceptButton.interactable = false;
                break;
            case ErrorCode.InvalidDate:
                feedbackParent.gameObject.SetActive(true);
                feedbackText.text = "The plan cannot be completed before the simulation ends";
				monthDropdown.interactable = false;
				yearDropdown.interactable = false;
				acceptButton.interactable = false;
                break;
        }
    }

    public static void UpdateMinSelectableTime()
    {
        if (instance != null)
            instance.updateMinSelectableTime();
    }

    private UpdateReach updateMinSelectableTime()
    {
        minTimeSelectable = GameState.GetCurrentMonth() + 1 + maxConstructionTime;
		if (finishTime < minTimeSelectable)
            finishTime = minTimeSelectable;
        UpdateReach reach = UpdateMinSelectableYear();
        if (reach < UpdateReach.MinMonth)
        {
            reach = UpdateMinSelectableMonth();
            if(reach < UpdateReach.Month)
                UpdateSecondaryTimeText();
        }
        return reach;
    }

    /// <summary>
    /// Updates the minimum implementation date and sets a target implementatio date right afterwards.
    /// Avoids double updates that would occur if UpdateMin and SetImplementation time were called seperately.
    /// </summary>
    private void UpdateMinAndSetTime(int time)
    {
        minTimeSelectable = GameState.GetCurrentMonth() + 1 + maxConstructionTime;
        if (finishTime < minTimeSelectable)
            finishTime = minTimeSelectable;
        UpdateMinSelectableYear(false);
        SetImplementationTime(time);
    }

    /// <summary>
    /// Updates the UI text for construction time and implementation start time
    /// </summary>
    private void UpdateSecondaryTimeText()
    {
        constructionDurationText.text = "(" + maxConstructionTime.ToString() + " months construction time)";
        constructionStartText.text = Util.MonthToText(finishTime - maxConstructionTime);
    }

    private void SetImplementationTime(int time)
    {
        if (time < minTimeSelectable)
            finishTime = minTimeSelectable;
        else
            finishTime = time;

        if (SetSelectedYear((int)((float)finishTime / 12f)) < UpdateReach.Month)
            SetSelectedMonth(finishTime % 12);
    }

    private UpdateReach SetSelectedYear(int newYear, bool forceUpdated = false)
    {
        ignoreTimeUICallback = true;
        yearDropdown.value = newYear - minYearSelectable;
        finishYear = newYear;
        UpdateReach reach = UpdateMinSelectableMonth(forceUpdated);
        ignoreTimeUICallback = false;
        return reach > UpdateReach.Year ? reach : UpdateReach.Year;
    }

    private UpdateReach SetSelectedMonth(int newMonth)
    {
        ignoreTimeUICallback = true;
        monthDropdown.value = newMonth - minMonthSelectable;
        finishMonth = newMonth;
        ignoreTimeUICallback = false;
        UpdateSecondaryTimeText();
        return UpdateReach.Month;
    }

    /// <summary>
    /// Returns wether the month value was updated.
    /// </summary>
    private UpdateReach UpdateMinSelectableYear(bool setValues = true)
    {
        int newMinimum = (int)((float)minTimeSelectable / 12f);
        if (newMinimum == minYearSelectable && dropDownsFilled)
            return UpdateReach.NoWhere;

        //Adds new year options
        minYearSelectable = newMinimum;
        yearDropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = minYearSelectable; i < Main.MspGlobalData.session_num_years; i++)
            options.Add((Main.MspGlobalData.start + i).ToString());
        yearDropdown.AddOptions(options);

        //Checks if the set dropdown value needs to be updated
        if (setValues)
        {
            UpdateReach reach;
            if (finishYear < minYearSelectable)
            {
                reach = SetSelectedYear(minYearSelectable, true);
            }
            else
                reach = SetSelectedYear(finishYear);
            return reach > UpdateReach.MinYear ? reach : UpdateReach.MinYear;
        }
        else
            return UpdateReach.MinYear;
    }

    /// <summary>
    /// Returns wether the month value was updated.
    /// </summary>
    private UpdateReach UpdateMinSelectableMonth(bool forceUpdated = false)
    {
        int newMinimum = finishYear == minYearSelectable ? minTimeSelectable % 12 : 0;
        if (newMinimum == minMonthSelectable && dropDownsFilled)
            return UpdateReach.NoWhere;

        //Adds new month options
        minMonthSelectable = newMinimum;
        monthDropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = minMonthSelectable; i < 12; i++)
            options.Add(Util.MonthToMonthText(i));
        monthDropdown.AddOptions(options);

        //Checks if the set dropdown value needs to be updated
        UpdateReach reach;
        if (finishMonth < minMonthSelectable || forceUpdated)
        {
            reach = SetSelectedMonth(minMonthSelectable);
            finishTime = finishYear * 12 + finishMonth;
        }
        else
            reach = SetSelectedMonth(finishMonth);
        dropDownsFilled = true;
        return reach > UpdateReach.MinMonth ? reach : UpdateReach.MinMonth;
    }

    public void YearDropdownChanged(int value)
    {
        if (!ignoreTimeUICallback)
        {
            finishYear = value + minYearSelectable;
            finishTime = finishYear * 12 + finishMonth;

            //Update months availabe in dropdown
            if (UpdateMinSelectableMonth() == UpdateReach.NoWhere)
                UpdateSecondaryTimeText();
        }
    }

    public void MonthDropdownChanged(int value)
    {
        if (!ignoreTimeUICallback)
        {
            finishMonth = value + minMonthSelectable;
            finishTime = finishYear * 12 + finishMonth;
            UpdateSecondaryTimeText();
        }
    }

	private void SetupActivityToggles()
	{
		SetupActivityToggle(Main.IsSimulationConfigured(ESimulationType.SEL), shippingToggle);
		SetupActivityToggle(Main.IsSimulationConfigured(ESimulationType.CEL), energyToggle);
		SetupActivityToggle(Main.IsSimulationConfigured(ESimulationType.MEL), ecologyToggle);
	}

	private void SetupActivityToggle(bool available, Toggle toggle)
	{
		toggle.gameObject.SetActive(available);
		if (available)
		{
			toggle.onValueChanged.AddListener((value) =>
			{
				if (!settingUpPlan)
					CheckForErrors();
			});
		}
	}
}