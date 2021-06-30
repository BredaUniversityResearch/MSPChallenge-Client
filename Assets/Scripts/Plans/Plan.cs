using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class Plan : IComparable<Plan>
{
	public delegate void PlanLockAction(Plan plan);

	public enum PlanState { DESIGN = 0, CONSULTATION = 1, APPROVAL = 2, APPROVED = 3, IMPLEMENTED = 4, DELETED = 5 };

	public int ID;
	public string Name;
	public string Description;
	public int StartTime;
	public int ConstructionStartTime;
	public PlanState State;
	public int Country;
	public int LockedBy;

	public List<PlanLayer> PlanLayers { get; private set; }

	public Dictionary<int, EPlanApprovalState> countryApproval;
	public List<EnergyGrid> energyGrids;
	public HashSet<int> removedGrids; //persis ID of removed grids
	public FishingDistributionDelta fishingDistributionDelta;
	public bool energyPlan;
	public bool shippingPlan;
	public bool ecologyPlan;
	public bool energyError; 
	public bool altersEnergyDistribution; 

	private bool requestingLock;

	public Plan(PlanObject planObject, Dictionary<AbstractLayer, int> layerUpdateTimes)
	{
		//=================================== BASE INFO =====================================
		ID = planObject.id;
		Name = planObject.name;
		Description = planObject.description;
		StartTime = planObject.startdate;
		State = StringToPlanState(planObject.state);
		Country = planObject.country;
		//Owner = TeamManager.GetTeamByIndex(planObject.user);

		//Set locked state
		int lockedByUser = -1;
		if (planObject.locked != null)
			lockedByUser = Util.ParseToInt(planObject.locked);
		LockedBy = lockedByUser;

        //Set required approval
        if (planObject.votes.Count > 0)
        {
            countryApproval = new Dictionary<int, EPlanApprovalState>();
            foreach (ApprovalObject obj in planObject.votes)
                countryApproval.Add(obj.country, obj.vote);
        }


		//=================================== PLANLAYERS =====================================

		//Create new plan layers
		PlanLayers = new List<PlanLayer>();
		foreach (PlanLayerObject layer in planObject.layers)
		{
			PlanLayer newPlanLayer = new PlanLayer(this, layer, layerUpdateTimes);
			PlanLayers.Add(newPlanLayer);
			newPlanLayer.DrawGameObjects();
		}

		//Determine contruction time and add to base layer
		int maxConstructionTime = 0;
		foreach (PlanLayer planLayer in PlanLayers)
		{
			if (State != PlanState.DELETED)
				planLayer.BaseLayer.AddPlanLayer(planLayer);
			if (planLayer.BaseLayer.AssemblyTime > maxConstructionTime)
				maxConstructionTime = planLayer.BaseLayer.AssemblyTime;
		}
		ConstructionStartTime = StartTime - maxConstructionTime;

		//=================================== PLAN TYPE =====================================

		//Determine plan type
		if (planObject.type != null)
		{
			string[] types = planObject.type.Split(',');
			energyPlan = types[0] == "1" && Main.IsSimulationConfigured(ESimulationType.CEL); //MSP-1856, Energy plans only valid when CEL is configured.
			ecologyPlan = types[1] == "1";
			shippingPlan = types[2] == "1";
		}

		if (ecologyPlan)
		{
			if (planObject.fishing == null)
			{
				fishingDistributionDelta = new FishingDistributionDelta(); //If null, it cant pick the right contructor automatically
			}
			else
			{
				fishingDistributionDelta = new FishingDistributionDelta(planObject.fishing);
			}
		}

		if (energyPlan)
		{
			//removedGrids = planObject.deleted_grids;
			//energyGrids = new List<EnergyGrid>();
			//foreach (GridObject obj in planObject.grids)
			//	energyGrids.Add(new EnergyGrid(obj, this));
            altersEnergyDistribution = planObject.alters_energy_distribution;
        }
        energyError = planObject.energy_error == "1";
	}

	public bool IsRequestingLock()
	{
		return requestingLock;
	}

	public bool IsLocked
	{
		get { return LockedBy != -1; }
	}

	public void AttemptLock(PlanLockAction actionOnSuccess, PlanLockAction actionOnFail)
	{
        if (PlanManager.UserHasPlanLocked(TeamManager.CurrentSessionID))
        {
            if (actionOnFail != null)
                actionOnFail(this);
            DialogBoxManager.instance.NotificationWindow("Lock failed", "You already have another plan locked.", () => { });
            return;
        }

        requestingLock = true;
		NetworkForm form = new NetworkForm();
		form.AddField("id", ID);
		form.AddField("user", TeamManager.CurrentSessionID.ToString());
		ServerCommunication.DoRequest<string>(Server.LockPlan(), form, (_) => HandleAttemptLockSuccess(actionOnSuccess), (r,m) => HandleAttemptLockFailure(actionOnFail, r, m));
	}

	private void HandleAttemptLockSuccess(PlanLockAction actionOnSuccess)
	{
		LockedBy = TeamManager.CurrentSessionID;
		PlanDetails.LockStateChanged(this, IsLocked);
		PlanManager.PlanLockUpdated(this);
		if (actionOnSuccess != null)
			actionOnSuccess(this);
		requestingLock = false;
	}

	private void HandleAttemptLockFailure(PlanLockAction actionOnFail, ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			Debug.Log($"Request failed with message: {message}.. Retrying {request.retriesRemaining} more times.");
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			requestingLock = false;
			Debug.Log("Failed to Lock " + Name + ". Error message: " + message);

			if (actionOnFail != null)
				actionOnFail(this);
			DialogBoxManager.instance.NotificationWindow("Lock failed", "The plan was locked by another user and could not be modified.", () => { });
		}
	}

	public void AttemptUnlock()
	{
		AttemptUnlock(false);
	}

	public void AttemptUnlock(bool forceUnlock)
	{
		AttemptUnlock(forceUnlock, null);
	}

	public void AttemptUnlock(bool forceUnlock, Action<string> callback)
	{
		NetworkForm form = new NetworkForm();
		form.AddField("id", ID);
		form.AddField("force_unlock", forceUnlock ? "1" : "0");
		form.AddField("user", TeamManager.CurrentSessionID.ToString());
		ServerCommunication.DoRequest<string>(Server.UnlockPlan(), form, callback, ServerCommunication.EWebRequestFailureResponse.Crash);
	}

	public void AttemptUnlock(BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", ID);
		dataObject.Add("force_unlock", 0);
		dataObject.Add("user", TeamManager.CurrentSessionID.ToString());
		batch.AddRequest(Server.UnlockPlan(), dataObject, BatchRequest.BATCH_GROUP_UNLOCK);
	}

	public bool HasErrors()
	{
		return energyError || IssueManager.instance.HasError(this);
	}

    public void UpdatePlan(PlanObject updatedData, Dictionary<AbstractLayer, int> layerUpdateTimes)
    {
        //=================================== BASE INFO UPDATE =====================================
        bool nameOrDescriptionChanged = false, timeChanged = false, stateChanged = false, forceMonitorUpdate = false;
        bool inTimelineBefore = ShouldBeVisibleInTimeline;

        //Handle name
        if (updatedData.name != Name)
        {
            Name = updatedData.name;
            nameOrDescriptionChanged = true;
        }

        //Handle description
        if (updatedData.description != Description)
        {
            Description = updatedData.description;
            nameOrDescriptionChanged = true;
        }

        //Handle locks
        int lockedByUser = -1;
        if (updatedData.locked != null)
            lockedByUser = Util.ParseToInt(updatedData.locked);
        if (lockedByUser != LockedBy)
        {
            LockedBy = lockedByUser;
            PlanManager.PlanLockUpdated(this);
            PlanDetails.LockStateChanged(this, IsLocked);
			stateChanged = true;
        }

        //Handle state
        PlanState oldState = State;
        State = StringToPlanState(updatedData.state);
        if (oldState != State)
        {
			if (State != PlanState.DESIGN)
			{
				bool editingLayers = Main.CurrentlyEditingPlan != null && Main.CurrentlyEditingPlan.ID == updatedData.id;
				bool editingContent = Main.EditingPlanDetailsContent && PlanDetails.GetSelectedPlan().ID == updatedData.id;
				//Cancel editing if we were editing it before
				if (editingLayers || editingContent)
				{
					PlanDetails.instance.CancelEditingContent();

					if (State == PlanState.DELETED)
					{
						DialogBoxManager.instance.NotificationWindow("Plan archived", "The plan's construction start time has passed. The plan was archived and changes have not been saved. Change the plan's implementation date and return it to the design phase and continue editing.", () => { });
					}
					else
					{
						DialogBoxManager.instance.NotificationWindow("Plan state changed", "Another player moved the plan out of the design state while you were editing. The current changes have not been saved. Change the plan state back to design to start editing again.", () => { });
					}
				}
			}

			if (State == PlanState.DELETED) //was deleted, disable and remove plan layers
            {
                //Stop viewing plan if we were before
                if (PlanManager.planViewing != null)
                    if (PlanManager.planViewing.ID == ID)
                        PlanManager.HideCurrentPlan();

                //Remove planlayers from their respective layers (AFTER REDRAWING)
                foreach (PlanLayer layer in PlanLayers)
                {
                    layer.BaseLayer.RemovePlanLayer(layer);
                    IssueManager.instance.DeleteIssuesForPlanLayer(layer);
                }
            }
            else if (State == PlanState.IMPLEMENTED)
            {
                foreach (PlanLayer layer in PlanLayers)
                    IssueManager.instance.DeleteIssuesForPlanLayer(layer);
                //Stop viewing plan if we were before
                if (PlanManager.planViewing != null)
                    if (PlanManager.planViewing.ID == ID)
                        PlanManager.HideCurrentPlan();
            }
            else if (oldState == PlanState.DELETED) //was deleted before, re-enable and add layers to base
            {
                foreach (PlanLayer layer in PlanLayers)
                {
                    layer.BaseLayer.AddPlanLayer(layer);
                    IssueManager.instance.InitialiseIssuesForPlanLayer(layer);
                }
            }

            stateChanged = true;
        }

        //Handle approval
        if (updatedData.votes == null)
        {
            if (countryApproval != null)
                stateChanged = true;
            countryApproval = null;
        }
        else
        {
            Dictionary<int, EPlanApprovalState> newCountryApproval = new Dictionary<int, EPlanApprovalState>();
            foreach (ApprovalObject obj in updatedData.votes)
            {
                if (countryApproval == null || !countryApproval.ContainsKey(obj.country) || countryApproval[obj.country] != obj.vote)
                    stateChanged = true; //Approval value is new or different
                newCountryApproval.Add(obj.country, obj.vote);
            }
            //Approval was already new or different OR approval has been removed
            stateChanged = stateChanged || (countryApproval != null && newCountryApproval.Count != countryApproval.Count);
            countryApproval = newCountryApproval;
        }

        //Handle time
        int oldStartTime = StartTime;
        if (updatedData.startdate != StartTime)
        {
            StartTime = updatedData.startdate;
            PlanManager.UpdatePlanTime(this);
            if (State != PlanState.DELETED)
                foreach (PlanLayer planLayer in PlanLayers)
                    planLayer.BaseLayer.UpdatePlanLayerTime(planLayer);
            timeChanged = true;
        }

        bool typeChanged = false;
        bool layersChanged = false;
        //PlanLayerUpdateTracker planLayerUpdateTracker = new PlanLayerUpdateTracker();

        //Do not update if we have the plan locked, no one could have changed it.
        //This persists if we stop editing until all data has been sent.
        if (!IsLockedByUs)
        {
            //=================================== GEOMETRY UPDATE =====================================
            //Keep track of planlayers that are not present after the update anymore
            HashSet<PlanLayer> removedPlanLayers = new HashSet<PlanLayer>(PlanLayers);
            foreach (PlanLayerObject updatedLayer in updatedData.layers)
            {
                PlanLayer planLayer = getPlanLayerForID(updatedLayer.layerid);
                if (planLayer == null)
                {
                    //Create new planLayer
                    planLayer = new PlanLayer(this, updatedLayer, layerUpdateTimes);
                    PlanLayers.Add(planLayer);
                    if (State != PlanState.DELETED)
                        planLayer.BaseLayer.AddPlanLayer(planLayer);
                    PlanManager.PlanLayerAdded(this, planLayer);
                    planLayer.DrawGameObjects();
                    layersChanged = true;
                }
                else
                {
                    //Update existing PlanLayer
                    planLayer.UpdatePlanLayer(updatedLayer, layerUpdateTimes);
                    removedPlanLayers.Remove(planLayer); //Still exists
                    //planLayerUpdateTracker.AddLayer(planLayer);
                }
            }
            //Remove planlayers that no longer exist
            foreach (PlanLayer removedPlanLayer in removedPlanLayers)
            {
                PlanLayers.Remove(removedPlanLayer);
                PlanManager.PlanLayerRemoved(this, removedPlanLayer);
                removedPlanLayer.BaseLayer.RemovePlanLayerAndEntities(removedPlanLayer);
                removedPlanLayer.RemoveGameObjects();
                layersChanged = true;
            }

            //Update construction start time
            int maxConstructionTime = 0;
            foreach (PlanLayer planLayer in PlanLayers)
                if (planLayer.BaseLayer.AssemblyTime > maxConstructionTime)
                    maxConstructionTime = planLayer.BaseLayer.AssemblyTime;
            ConstructionStartTime = StartTime - maxConstructionTime;
        //}

        ////Do not update geometry or type if it is being edited. It is locked by this user anyway.
        //if (Main.CurrentlyEditingPlan == null || Main.CurrentlyEditingPlan.ID != updatedData.id)
        //{
            //=================================== PLAN TYPE UPDATE =====================================

            bool newEnergyPlan = false;
            bool newEcologyPlan = false;
            bool newShippingPlan = false;
            if (updatedData.type != null)
            {
                string[] types = updatedData.type.Split(',');
                newEnergyPlan = types[0] == "1";
                newEcologyPlan = types[1] == "1";
                newShippingPlan = types[2] == "1";
                typeChanged = true;
            }

            //Update energy
            if (energyPlan && !newEnergyPlan)
            {
                //If error changed, plandetails needs update
                if (energyError)
                    stateChanged = true;

                energyGrids = null;
                energyError = false;
                altersEnergyDistribution = false;
                forceMonitorUpdate = true;
            }
            else if (newEnergyPlan)
            {
                bool oldEnergyError = energyError;
                energyError = updatedData.energy_error == "1";
                altersEnergyDistribution = updatedData.alters_energy_distribution;
                forceMonitorUpdate = true;

                //If error changed, plandetails needs update
                if (energyError != oldEnergyError)
                    stateChanged = true;
            }
            energyPlan = newEnergyPlan;

            //Update fishing
            if (ecologyPlan && !newEcologyPlan)
                fishingDistributionDelta = null;
            else if (newEcologyPlan)
                fishingDistributionDelta = new FishingDistributionDelta(updatedData.fishing);
            ecologyPlan = newEcologyPlan;

            //Update shipping
            shippingPlan = newShippingPlan;
        }
        //ServerCommunication.WaitForCondition(planLayerUpdateTracker.CompletedPlanLayerUpdates, () => planUpdateTracker.CompletedUpdate());

        LayerManager.UpdateVisibleLayersFromPlan(this);
        PlanManager.UpdatePlanInUI(this, nameOrDescriptionChanged, timeChanged, stateChanged, layersChanged, typeChanged, forceMonitorUpdate, oldStartTime, oldState, inTimelineBefore);
    }

	public void UpdateGrids(HashSet<int> deleted, List<GridObject> newGrids)
	{
        //Don't update grids if we have the plan locked. Prevents updates while editing.
        if (!IsLockedByUs)
        {
            removedGrids = deleted;
            energyGrids = new List<EnergyGrid>();
            foreach (GridObject obj in newGrids)
                energyGrids.Add(new EnergyGrid(obj, this));
        }
	}

	public PlanLayer GetPlanLayerForLayer(AbstractLayer baseLayer)
	{
		foreach (PlanLayer planLayer in PlanLayers)
			if (planLayer.BaseLayer.ID == baseLayer.ID)
				return planLayer;

		return null;
	}

	public PlanLayer getPlanLayerForID(int planLayerID)
	{
		foreach (PlanLayer planLayer in PlanLayers)
			if (planLayer.ID == planLayerID)
				return planLayer;

		return null;
	}

	public void SetState(PlanState newState, BatchRequest batch)
	{
		if (newState == State)
			return;
		if (newState == PlanState.APPROVAL)
		{
			//makes sure approval is reset if we move to the approval state
			//SubmitRequiredApproval(batch); //todo: reenable
		}

		JObject dataObject = new JObject();
		dataObject.Add("id", ID);
		dataObject.Add("state", newState.ToString());
		dataObject.Add("user", TeamManager.CurrentSessionID.ToString());
		batch.AddRequest(Server.SetPlanState(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public static void SendPlan(string planName, List<AbstractLayer> layers, int time, string type, bool altersEnergyDistribution)
	{
		NetworkForm form = new NetworkForm();
		form.AddField("country", TeamManager.CurrentUserTeamID);
		form.AddField("name", planName);
		form.AddField("time", time);

		if (layers != null && layers.Count > 0)
		{
			List<int> layerIDs = new List<int>(layers.Count);
			foreach (AbstractLayer layer in layers)
				layerIDs.Add(layer.ID);
			Debug.Log(JToken.FromObject(layerIDs).ToString());
			form.AddField("layers", JToken.FromObject(layerIDs));
		}
		form.AddField("type", type);
		form.AddField("alters_energy_distribution", altersEnergyDistribution ? 1 : 0);
		Debug.Log(form.ToString());
		ServerCommunication.DoRequest<int>(Server.PostPlan(), form, PlanPostedCallback);
	}

    static void PlanPostedCallback(int newPlanID)
    {
         PlanManager.ViewPlanWithIDWhenReceived(newPlanID);
    }

	public static void SetPlanType(int planId, string type, BatchRequest batch)
	{
		JObject dataObject = new JObject();

		dataObject.Add("id", planId);
		dataObject.Add("type", type);

		batch.AddRequest(Server.SetPlanType(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public static void SetEnergyDistribution(int planId, bool altersEnergyDistribution, BatchRequest batch)
	{
		JObject dataObject = new JObject();

		dataObject.Add("id", planId);
		dataObject.Add("alters_energy_distribution", altersEnergyDistribution ? 1 : 0);

		batch.AddRequest(Server.SetPlanEnergyDistribution(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void SetDescription(string newDescription, BatchRequest batch)
	{
		JObject dataObject = new JObject();

		dataObject.Add("id", ID);
		dataObject.Add("description", string.IsNullOrEmpty(newDescription) ? " " : newDescription);

		batch.AddRequest(Server.SetPlanDescription(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void RenamePlan(string newName, BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", ID);
		dataObject.Add("name", newName);
		batch.AddRequest(Server.RenamePlanLayer(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void ChangePlanDate(int newDate, BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", ID);
		dataObject.Add("date", newDate);
		batch.AddRequest(Server.ChangePlanDate(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void AddNewPlanLayer(AbstractLayer layer, BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", ID);
		dataObject.Add("layerid", layer.ID);
		batch.AddRequest(Server.AddPlanLayer(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void RemovePlanLayer(PlanLayer planLayerToRemove, BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", planLayerToRemove.ID);
		batch.AddRequest(Server.DeletePlanLayer(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public void SubmitRequiredApproval(BatchRequest batch, Dictionary<int, EPlanApprovalState> newApproval)
	{
		JObject dataObject = new JObject();
		if (newApproval.Count > 0)
		{
			List<int> countries = new List<int>(newApproval.Count);
			foreach (KeyValuePair<int, EPlanApprovalState> kvp in newApproval)
				countries.Add(kvp.Key);
			dataObject.Add("countries", JToken.FromObject(countries));
		}
		dataObject.Add("id", ID);
		batch.AddRequest(Server.AddApproval(), dataObject, BatchRequest.BATCH_GROUP_PLAN_CHANGE);
	}

	public Rect GetBounds()
	{
		if (PlanLayers.Count == 0) { return new Rect(); }
		Rect result = PlanLayers[0].GetBounds();
		for (int i = 1; i < PlanLayers.Count; ++i)
		{
			Rect planLayerRect = PlanLayers[i].GetBounds();
			if (planLayerRect.width > 0 && planLayerRect.height > 0)
			{
				if (result.width > 0 && result.height > 0)
				{
					Vector2 min = Vector2.Min(result.min, planLayerRect.min);
					Vector2 max = Vector2.Max(result.max, planLayerRect.max);
					result = new Rect(min, max - min);
				}
				else
				{
					result = planLayerRect;
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Duplicates (value copies) the given energy grid and adds it to this plan.
	/// </summary>
	public EnergyGrid DuplicateEnergyGridToPlan(EnergyGrid gridToDuplicate)
	{
		EnergyGrid duplicate = new EnergyGrid(gridToDuplicate, this);
		duplicate.distributionOnly = true;
		energyGrids.Add(duplicate);
		return duplicate;
	}

	public Dictionary<int, EPlanApprovalState> CalculateRequiredApproval(HashSet<int> countriesAffectedByRemovedGrids)
	{
		bool requireAMApproval = false;
		EApprovalType requiredApprovalLevel = EApprovalType.NotDependent;
		Dictionary<int, EPlanApprovalState>  newCountryApproval = new Dictionary<int, EPlanApprovalState>();

		//Store this so we don't have to find removed geometry twice per layer
		List<List<SubEntity>> removedGeom = new List<List<SubEntity>>();

		//Check required approval for layers in plan
		for (int i = 0; i < PlanLayers.Count; i++)
		{
			//Check removed geometry
			List<SubEntity> removedSubEntities = PlanLayers[i].GetInstancesOfRemovedGeometry();
			foreach (SubEntity t in removedSubEntities)
			{
				foreach (EntityType type in t.Entity.EntityTypes)
				{
					if (type.requiredApproval == EApprovalType.AreaManager)
						requireAMApproval = true;
					else if (type.requiredApproval > requiredApprovalLevel)
						requiredApprovalLevel = type.requiredApproval;
				}
			}
			//Check new geometry
			for (int entityIndex = 0; entityIndex < PlanLayers[i].GetNewGeometryCount(); ++entityIndex)
			{
				Entity t = PlanLayers[i].GetNewGeometryByIndex(entityIndex);
				foreach (EntityType type in t.EntityTypes)
				{
					if (type.requiredApproval == EApprovalType.AreaManager)
						requireAMApproval = true;
					else if (type.requiredApproval > requiredApprovalLevel)
						requiredApprovalLevel = type.requiredApproval;
				}
			}
			removedGeom.Add(removedSubEntities);
		}

		if (requireAMApproval)
			newCountryApproval.Add(TeamManager.AM_ID, EPlanApprovalState.Maybe);

		if (ecologyPlan)
		{
			SetupEcologyApproval(newCountryApproval, ref requiredApprovalLevel);
		}

		//If not all approval required yet, check energy required approval
		if (energyPlan && requiredApprovalLevel < EApprovalType.AllCountries)
		{
			//Removed grids
			if (countriesAffectedByRemovedGrids != null)
				foreach (int i in countriesAffectedByRemovedGrids)
					if (!newCountryApproval.ContainsKey(i))
						newCountryApproval.Add(i, EPlanApprovalState.Maybe);

			//Added grids
			if (energyGrids != null)
				foreach (EnergyGrid grid in energyGrids)
					foreach (KeyValuePair<int, CountryEnergyAmount> countryAmount in grid.energyDistribution.distribution)
						if (!newCountryApproval.ContainsKey(countryAmount.Key))
							newCountryApproval.Add(countryAmount.Key, EPlanApprovalState.Maybe);
		}

		//All team approval required, there is no chance for AM approval
		if (requiredApprovalLevel >= EApprovalType.AllCountries)
		{
			foreach (KeyValuePair<int, Team> kvp in TeamManager.GetTeamsByID())
				if (!kvp.Value.IsManager && kvp.Value.ID != TeamManager.CurrentUserTeamID)
					newCountryApproval.Add(kvp.Value.ID, EPlanApprovalState.Maybe);
			return newCountryApproval;
		}

		if (requiredApprovalLevel > 0 && LayerManager.EEZLayer != null)
		{
			List<PolygonEntity> EEZs = LayerManager.EEZLayer.Entities;
			int userCountry = TeamManager.CurrentUserTeamID;
			for (int i = 0; i < PlanLayers.Count; i++)
			{
				//The overlap function depends on the layer type
				Func<PolygonSubEntity, SubEntity, bool> overlapCheck;
				if (PlanLayers[i].BaseLayer is PolygonLayer)
					overlapCheck = (a, b) => Util.PolygonPolygonIntersection(a, b as PolygonSubEntity);
				else if (PlanLayers[i].BaseLayer is LineStringLayer)
					overlapCheck = (a, b) => Util.PolygonLineIntersection(a, b as LineStringSubEntity);
				else
					overlapCheck = (a, b) => Util.PolygonPointIntersection(a, b as PointSubEntity);

				//Check for new geometry
				for (int entityIndex = 0; entityIndex < PlanLayers[i].GetNewGeometryCount(); ++entityIndex)
				{
					Entity t = PlanLayers[i].GetNewGeometryByIndex(entityIndex);
					if (t.Country != userCountry && !newCountryApproval.ContainsKey(t.Country))
						newCountryApproval.Add(t.Country, EPlanApprovalState.Maybe);
					foreach (PolygonEntity eez in EEZs)
						if (eez.Country != userCountry && !newCountryApproval.ContainsKey(eez.Country) && overlapCheck(eez.GetPolygonSubEntity(), t.GetSubEntity(0)))
							newCountryApproval.Add(eez.Country, EPlanApprovalState.Maybe);
				}
			}
		}

		//Check for removed geometry. Only the country which owns the geometry will need to give their approval.
		for (int i = 0; i < PlanLayers.Count; i++)
		{
			foreach (SubEntity t in removedGeom[i])
			{
				if (t.Entity.Country != Entity.INVALID_COUNTRY_ID && t.Entity.Country != TeamManager.CurrentUserTeamID && !newCountryApproval.ContainsKey(t.Entity.Country))
				{
					newCountryApproval.Add(t.Entity.Country, EPlanApprovalState.Maybe);
				}
			}
		}

		//Remove owner from required approval
		if (newCountryApproval.ContainsKey(Country))
			newCountryApproval.Remove(Country);
        if (newCountryApproval.ContainsKey(-1))
            newCountryApproval.Remove(-1);
        if (newCountryApproval.ContainsKey(TeamManager.GM_ID))
            newCountryApproval.Remove(TeamManager.GM_ID);

		return newCountryApproval;
	}

	private void SetupEcologyApproval(Dictionary<int, EPlanApprovalState> approvalStates, ref EApprovalType requiredApprovalLevel)
	{
		if (fishingDistributionDelta == null)
		{
			Debug.LogError("Need to calculate Ecology approval for a plan without fishing distributions.");
			return;
		}

		bool hasChangedFishingValue = false;
		foreach (KeyValuePair<string, Dictionary<int, float>> fishingFleets in fishingDistributionDelta.GetValuesByFleet())
		{
			foreach (KeyValuePair<int, float> fishingValues in fishingFleets.Value)
			{
				if (!approvalStates.ContainsKey(fishingValues.Key))
				{
					approvalStates.Add(fishingValues.Key, EPlanApprovalState.Maybe);
					hasChangedFishingValue = true;
				}
			}
		}

		if (hasChangedFishingValue && requiredApprovalLevel < EApprovalType.EEZ)
		{
			requiredApprovalLevel = EApprovalType.EEZ;
		}
	}

	public bool NeedsApproval()
	{
		return countryApproval != null && countryApproval.Count > 0;
	}

	public bool NeedsApprovalFrom(int country)
	{
		return countryApproval != null && countryApproval.ContainsKey(country);
	}

	public bool HasApproval()
	{
		if (countryApproval == null)
			return true;
		foreach (KeyValuePair<int, EPlanApprovalState> kvp in countryApproval)
			if (kvp.Value != EPlanApprovalState.Approved)
				return false;
		return true;
	}

	public bool InInfluencingState
	{
		get { return State.IsInfluencingState(); }
	}

	public bool ShouldBeVisibleInUI
	{
		get
        {
            return (InInfluencingState && StartTime >= 0) || TeamManager.CurrentUserTeamID == Country || TeamManager.AreWeManager;
        }
	}

    public bool ShouldBeVisibleInTimeline
    {
        get
        {
            return ShouldBeVisibleInUI && State != Plan.PlanState.DELETED;
        }
    }

    public bool IsLockedByUs
    {
        get { return TeamManager.CurrentSessionID == LockedBy; }
    }

	public bool RequiresTimeChange
	{
		get { return State == PlanState.DELETED && ConstructionStartTime <= GameState.GetCurrentMonth(); }
	}

	public static PlanState StringToPlanState(string state)
	{
		try
		{
			return (PlanState)Enum.Parse(typeof(PlanState), state);
		}
		catch (Exception e)
		{
			Debug.LogError("Could not parse: \"" + state + "\" to a valid planstate. Exception: " + e.Message);
		}
		return PlanState.DESIGN;
	}

	public void SubmitEnergyError(bool value, bool checkDependencies, BatchRequest batch)
	{
		JObject dataObject = new JObject();
		dataObject.Add("id", ID);
		dataObject.Add("error", value ? 1 : 0);
		dataObject.Add("check_dependent_plans", checkDependencies ? 1 : 0);
		batch.AddRequest(Server.SetEnergyError(), dataObject, BatchRequest.BATCH_GROUP_ENERGY_ERROR);
	}

    public void SubmitRemovedGrids(BatchRequest batch)
    {
		JObject dataObject = new JObject();
		dataObject.Add("plan", ID);
		if(removedGrids!= null && removedGrids.Count > 0)
			dataObject.Add("delete", JToken.FromObject(removedGrids));
		batch.AddRequest(Server.SetPlanRemovedGrids(), dataObject, BatchRequest.BATCH_GROUP_PLAN_GRID_CHANGE);
    }

	public bool IsLayerpartOfPlan(AbstractLayer layer)
	{
		foreach (PlanLayer pl in PlanLayers)
			if (pl.BaseLayer.ID == layer.ID)
				return true;
		return false;
	}

	public int CompareTo(Plan other)
	{
		if (other == null)
			return 1;
		if (other.StartTime != StartTime)
			return StartTime.CompareTo(other.StartTime);
		else
			return ID.CompareTo(other.ID);
	}

    public SubEntity CheckForInvalidGeometry()
    {
        foreach (PlanLayer layer in PlanLayers)
        {
            PolygonLayer polyLayer = layer.BaseLayer as PolygonLayer;
            if (polyLayer != null)
            {
                foreach (Entity ent in layer.GetNewGeometry())
                {
                    PolygonSubEntity polySubEnt = ent.GetSubEntity(0) as PolygonSubEntity;
                    if (polySubEnt.InvalidPoints != null && polySubEnt.InvalidPoints.Count > 0)
                        return polySubEnt;
                }
            }
        }
        return null;
    }

    public bool CheckForInvalidCables()
    {
        foreach (PlanLayer planLayer in PlanLayers)
        {
            //Check all new geometry in cable layers
            if (planLayer.BaseLayer.IsEnergyLineLayer() && planLayer.GetNewGeometryCount() > 0)
            {
                //Create layer states for energy layers of a marching color, ignoring the cable layer
                Dictionary<AbstractLayer, LayerState> energyLayerStates = new Dictionary<AbstractLayer, LayerState>();
                foreach (AbstractLayer energyLayer in LayerManager.energyLayers)
                    if (energyLayer.greenEnergy == planLayer.BaseLayer.greenEnergy && energyLayer.ID != planLayer.BaseLayer.ID)
                        energyLayerStates.Add(energyLayer, energyLayer.GetLayerStateAtPlan(this));

                foreach (Entity entity in planLayer.GetNewGeometry())
                {
                    //Check the 2 connections for valid points
                    EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)entity.GetSubEntity(0);
                    foreach (Connection conn in cable.connections)
                    {
                        bool found = false;
                        AbstractLayer targetPointLayer = conn.point.sourcePolygon == null ? conn.point.Entity.Layer : conn.point.sourcePolygon.Entity.Layer;
                        foreach (Entity existingEntity in energyLayerStates[targetPointLayer].baseGeometry)
                        {
                            if (existingEntity.DatabaseID == conn.point.GetDatabaseID())
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public void ZoomToPlan()
    { 
        if(RectValid())
            CameraManager.Instance.ZoomToBounds(GetPlanRect());
    }

    public bool RectValid()
    {
        if (PlanLayers.Count == 0) return false;
        for (int i = 0; i < PlanLayers.Count; i++)
        {
            if (PlanLayers[i].RemovedGeometry.Count > 0) return true;
            if (PlanLayers[i].GetNewGeometryCount() > 0) return true;
        }
        return false;
    }

    public Rect GetPlanRect()
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        for (int i = 0; i < PlanLayers.Count; i++)
        {
            //Check removed geometry
            List<SubEntity> removedSubEntities = PlanLayers[i].GetInstancesOfRemovedGeometry();
            foreach (SubEntity subEntity in removedSubEntities)
            {
                min = Vector3.Min(min, subEntity.BoundingBox.min);
                max = Vector3.Max(max, subEntity.BoundingBox.max);
            }
            //Check new geometry
            for (int entityIndex = 0; entityIndex < PlanLayers[i].GetNewGeometryCount(); ++entityIndex)
            {
                SubEntity subEntity = PlanLayers[i].GetNewGeometryByIndex(entityIndex).GetSubEntity(0);
                min = Vector3.Min(min, subEntity.BoundingBox.min);
                max = Vector3.Max(max, subEntity.BoundingBox.max);
            }
        }
        return new Rect(min, max - min); 
    }

    public void AddSystemMessage(string text)
    {
        NetworkForm form = new NetworkForm();
        form.AddField("plan", ID);
        form.AddField("team_id", TeamManager.GM_ID);
        form.AddField("user_name", "[SYSTEM]");
        form.AddField("text", text);

        ServerCommunication.DoRequest(Server.PostPlanFeedback(), form);
    }
}

public class PlanLayerUpdateTracker
{
	List<PlanLayer> remainingLayers;

	public PlanLayerUpdateTracker()
	{
		remainingLayers = new List<PlanLayer>();
	}

	public void AddLayer(PlanLayer layer)
	{
		remainingLayers.Add(layer);
	}
}


