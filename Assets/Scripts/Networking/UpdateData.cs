using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;

public static class UpdateData
{
	private static float updateSpeed = 1.0f;
	private static bool canUpdate = true;
	private static DialogBox disconnectDialogBox = null;
	private static double lastUpdateTimestamp = -1;
	public static double LastUpdateTimeStamp => lastUpdateTimestamp;
	public static UpdateObject lastUpdate;
	public static bool stopProcessingUpdates = false;

	public static IEnumerator GetFirstUpdate()
	{
		canUpdate = false;
		NetworkForm form = new NetworkForm();
		form.AddField("team_id", TeamManager.CurrentUserTeamID);
		form.AddField("last_update_time", lastUpdateTimestamp.ToString());
		form.AddField("user", TeamManager.CurrentSessionID.ToString());
		ServerCommunication.DoRequest<UpdateObject>(Server.Update(), form, HandleUpdateSucessCallback, HandleUpdateFailCallback);

		while (!canUpdate)
		{
			yield return null;
		}

		Main.FirstUpdateTickComplete();
	}

	public static IEnumerator GetUpdates()
	{
		while (true)
		{
			canUpdate = false;
			NetworkForm form = new NetworkForm();
			form.AddField("team_id", TeamManager.CurrentUserTeamID);
			form.AddField("last_update_time", lastUpdateTimestamp.ToString());
			form.AddField("user", TeamManager.CurrentSessionID.ToString());
			ServerCommunication.DoRequest<UpdateObject>(Server.Update(), form, HandleUpdateSucessCallback, HandleUpdateFailCallback);

			while (!canUpdate)
			{
				yield return null;
			}

			yield return new WaitForSeconds(updateSpeed);
		}
	}

	public static IEnumerator TickServerCoroutine()
	{
		UnityWebRequest lastTickRequest = null;
		while (true)
		{
			if (lastTickRequest != null)
			{
				while (!lastTickRequest.isDone)
				{
					yield return lastTickRequest;
				}

				TickResult result = Util.DeserializeObject<TickResult>(lastTickRequest);
				if(lastTickRequest.error != null)
				{
					Debug.LogError(string.Format($"Error when ticking the server ({lastTickRequest.url}): {lastTickRequest.error}"));
				}
				else if (!result.success || !string.IsNullOrEmpty(result.payload))
				{
					Debug.LogError(string.Format("Error in request to {0}: {1}", lastTickRequest.url, result.message));
				}
			}

			lastTickRequest = UnityWebRequest.Get(Server.Url + Server.Tick());

			ServerCommunication.AddDefaultHeaders(lastTickRequest);

			lastTickRequest.SendWebRequest();
			//We just need to tick the server to keep it updating. Don't care about anything that it returns.
			yield return new WaitForSeconds(1.0f);
		}
	}

	public class TickResult
	{
		public bool success;
		public string message;
		public string payload;
	}

	private static void HandleUpdateSucessCallback(UpdateObject updateData)
	{
		ProcessUpdates(updateData);
		HideDisconnectedDialogBox();
		canUpdate = true;
	}

	private static void HandleUpdateFailCallback(ServerCommunication.ARequest request, string message)
	{
		ShowDisconnectedDialogBox();
		Debug.LogError("Fetching update failed. Message: " + message);
		canUpdate = true;
	}

	private static void ShowDisconnectedDialogBox()
	{
		if (disconnectDialogBox == null)
		{
			disconnectDialogBox = DialogBoxManager.instance.NotificationWindow("Disconnected", "Your connection to the server has been interrupted.\n\nHold on while we are trying to re-establish the connection.",
				() => {
                    Main.QuitGame();
                }, "Close Game");
		}
	}

	private static void HideDisconnectedDialogBox()
	{
		if (disconnectDialogBox != null)
		{
			DialogBoxManager.instance.DestroyDialogBox(disconnectDialogBox);
		}
	}

	private static void ProcessUpdates(UpdateObject updates)
	{
		if (stopProcessingUpdates || updates.update_time <= lastUpdateTimestamp)
			return;

		lastUpdateTimestamp = updates.update_time;
		lastUpdate = updates;

		Dictionary<AbstractLayer, int> layerUpdateTimes = new Dictionary<AbstractLayer, int>();
		List<Plan> plans = null;

		if (updates.plan != null)
		{
			//Sort plans by time and ID so there are no issues with dependencies when loading them in
			updates.plan.Sort();
			plans = new List<Plan>(updates.plan.Count);
			foreach (PlanObject plan in updates.plan)
			{
				plans.Add(PlanManager.ProcessReceivedPlan(plan, layerUpdateTimes));
			}
		}

		foreach (RasterUpdateObject raster in updates.raster)
		{
			AbstractLayer layer = LayerManager.GetLayerByID(raster.id);

			RasterLayer rasterLayer = layer as RasterLayer;
			if (rasterLayer != null && LayerManager.LayerIsVisible(rasterLayer))
			{
				rasterLayer.ReloadLatestRaster();
			}
		}

		//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
		foreach (EnergyOutputObject outputUpdate in updates.energy.output)
		{
			UpdateOutput(outputUpdate);
		}

		//Update grids
		if (updates.plan != null)
		{
			for(int i = 0; i < plans.Count; i++)
			{
				plans[i].UpdateGrids(updates.plan[i].deleted_grids, updates.plan[i].grids);
			}
		}

		//Run connection update before KPI update so cable networks are accurate in the KPIs
		foreach (EnergyConnectionObject connection in updates.energy.connections)
		{
			UpdateConnection(connection);
		}

		GameState.UpdateTime(updates.tick);

		if (updates.kpi != null)
		{
			if (updates.kpi.energy != null && updates.kpi.energy.Length > 0)
			{
				KPIManager.ReceiveEnergyKPIUpdate(updates.kpi.energy);
			}

			if (updates.kpi.ecology != null && updates.kpi.ecology.Length > 0)
			{
				KPIManager.ReceiveEcologyKPIUpdate(updates.kpi.ecology);
			}

			if (updates.kpi.shipping != null && updates.kpi.shipping.Length > 0)
			{
				KPIManager.ReceiveShippingKPIUpdate(updates.kpi.shipping);
			}
		}

		if (updates.objectives.Count > 0)
		{
			InterfaceCanvas.Instance.objectivesMonitor.UpdateObjectivesFromServer(updates.objectives);
		}

		PlanDetails.AddFeedbackFromServer(updates.planmessages);

		if (PlanManager.planViewing != null && !Main.InEditMode && !Main.EditingPlanDetailsContent)
		{
			int viewingTime = PlanManager.planViewing.StartTime;
			foreach (KeyValuePair<AbstractLayer, int> kvp in layerUpdateTimes)
			{
				if (kvp.Value <= viewingTime)
				{
					kvp.Key.SetEntitiesActiveUpTo(PlanManager.planViewing);
					kvp.Key.RedrawGameObjects(CameraManager.Instance.gameCamera);
				}
			}
		}

		if (updates.warning != null)
		{
			IssueManager.instance.OnIssuesReceived(updates.warning); //MSP-2358, ensure Warnings are processed after all the plan updates are done.
		}

		PlanManager.CheckIfExpectedplanReceived();
	}

	private static void UpdateOutput(EnergyOutputObject outputUpdate)
	{
		SubEntity tempSubEnt = LayerManager.GetEnergySubEntityByID(outputUpdate.id);
		if (tempSubEnt == null) return;
		IEnergyDataHolder energyObj = (IEnergyDataHolder)tempSubEnt;
		energyObj.UsedCapacity = outputUpdate.capacity;
		energyObj.Capacity = outputUpdate.maxcapacity;
		tempSubEnt.UpdateTextMeshText();
	}

	private static void UpdateConnection(EnergyConnectionObject connection)
	{
		if (connection.active == "0")
			return;

		int startID = Util.ParseToInt(connection.start);
		int endID = Util.ParseToInt(connection.end);
		int cableID = Util.ParseToInt(connection.cable);
		string[] temp = connection.coords.Split(',');
		Vector3 firstCoord = new Vector2(Util.ParseToFloat(temp[0].Substring(1)), Util.ParseToFloat(temp[1].Substring(0, temp[1].Length - 1)));

		EnergyPointSubEntity point1;
		EnergyPointSubEntity point2;
		SubEntity tempSubEnt = LayerManager.GetEnergySubEntityByID(cableID);
		if (tempSubEnt == null) return;
		EnergyLineStringSubEntity cable = tempSubEnt as EnergyLineStringSubEntity;

		//Get the points, check if they reference to a polygon or point
		tempSubEnt = LayerManager.GetEnergySubEntityByID(startID);
		if (tempSubEnt == null) return;
		else if (tempSubEnt is EnergyPolygonSubEntity)
			point1 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
		else
			point1 = tempSubEnt as EnergyPointSubEntity;

		tempSubEnt = LayerManager.GetEnergySubEntityByID(endID);
		if (tempSubEnt == null) return;
		else if (tempSubEnt is EnergyPolygonSubEntity)
			point2 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
		else
			point2 = tempSubEnt as EnergyPointSubEntity;

		Connection conn1 = new Connection(cable, point1, true);
		Connection conn2 = new Connection(cable, point2, false);

		//Cables store connections and attach them to points when editing starts
		cable.AddConnection(conn1);
		cable.AddConnection(conn2);
		cable.SetEndPointsToConnections();
	}
}

/// <summary>
/// Keeps track of the update state of all plans in the update.
/// When geometry updates are completed CompletedPlanUpdates will return true and ExecutePostPlanUpdates can be called
/// </summary>
//public class PlanUpdateTracker
//{
//	private int totalPlans;
//	private int completedPlans;
//	private Dictionary<Plan, KeyValuePair<HashSet<int>, List<GridObject>>> tasks;
//    private Dictionary<AbstractLayer, int> layerUpdateTimes;

//    private EnergyKPIObject[] energyKpiUpdates;
//	private EnergyObject energyUpdate;

//	private WarningObject warningUpdate;

//	public PlanUpdateTracker(EnergyKPIObject[] energyKpiUpdates, EnergyObject energyUpdate, WarningObject warningUpdate)
//	{
//		this.energyKpiUpdates = energyKpiUpdates;
//		this.energyUpdate = energyUpdate;
//		this.warningUpdate = warningUpdate;
//		tasks = new Dictionary<Plan, KeyValuePair<HashSet<int>, List<GridObject>>>();
//        layerUpdateTimes = new Dictionary<AbstractLayer, int>();
//	}

//	public void StartedUpdate()
//	{
//		totalPlans++;
//	}

//	public void CompletedUpdate()
//	{
//		completedPlans++;
//	}

//	public void AddCompletionTask(Plan plan, HashSet<int> deleted, List<GridObject> newGrids)
//	{
//		tasks.Add(plan, new KeyValuePair<HashSet<int>, List<GridObject>>(deleted, newGrids));
//	}

//    public void AddLayerChange(AbstractLayer layer, int time)
//    {
//        int existingUpdate = 0;
//        if (layerUpdateTimes.TryGetValue(layer, out existingUpdate))
//        {
//            if (time < existingUpdate)
//                layerUpdateTimes[layer] = existingUpdate;
//        }
//        else
//            layerUpdateTimes.Add(layer, time);
//    }

//	public bool CompletedPlanUpdates()
//	{
//		return totalPlans <= completedPlans;
//	}

//	public void ExecutePostUpdate()
//	{
//		//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
//		foreach (EnergyOutputObject outputUpdate in energyUpdate.output)
//		{
//			SubEntity tempSubEnt = LayerManager.GetEnergySubEntityByID(outputUpdate.id);
//			if (tempSubEnt == null) continue;
//            IEnergyDataHolder energyObj = (IEnergyDataHolder)tempSubEnt;
//            energyObj.UsedCapacity = outputUpdate.capacity;
//            energyObj.Capacity = outputUpdate.maxcapacity;
//            tempSubEnt.UpdateTextMeshText();
//		}

//        //Update grids
//		if (tasks.Count > 0)
//		{
//			foreach (KeyValuePair<Plan, KeyValuePair<HashSet<int>, List<GridObject>>> task in tasks)
//				task.Key.UpdateGrids(task.Value.Key, task.Value.Value);

//			EnergyGridReceivedEvent.Invoke();
//		}

//		//Run connection update before KPI update so cable networks are accurate in the KPIs
//		foreach (EnergyConnectionObject connection in energyUpdate.connections)
//		{
//			if (connection.active == "0")
//				continue;

//			int startID = Util.ParseToInt(connection.start);
//			int endID = Util.ParseToInt(connection.end);
//			int cableID = Util.ParseToInt(connection.cable);
//			string[] temp = connection.coords.Split(',');
//			Vector3 firstCoord = new Vector2(Util.ParseToFloat(temp[0].Substring(1)), Util.ParseToFloat(temp[1].Substring(0, temp[1].Length - 1)));

//			EnergyPointSubEntity point1;
//			EnergyPointSubEntity point2;
//			SubEntity tempSubEnt = LayerManager.GetEnergySubEntityByID(cableID);
//			if (tempSubEnt == null) continue;
//			EnergyLineStringSubEntity cable = tempSubEnt as EnergyLineStringSubEntity;

//			//Get the points, check if they reference to a polygon or point
//			tempSubEnt = LayerManager.GetEnergySubEntityByID(startID);
//			if (tempSubEnt == null) continue;
//			else if (tempSubEnt is EnergyPolygonSubEntity)
//				point1 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
//			else
//				point1 = tempSubEnt as EnergyPointSubEntity;

//			tempSubEnt = LayerManager.GetEnergySubEntityByID(endID);
//			if (tempSubEnt == null) continue;
//			else if (tempSubEnt is EnergyPolygonSubEntity)
//				point2 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
//			else
//				point2 = tempSubEnt as EnergyPointSubEntity;
			
//            Connection conn1 = new Connection(cable, point1, true);
//			Connection conn2 = new Connection(cable, point2, false);

//			//Cables store connections and attach them to points when editing starts
//			cable.AddConnection(conn1);
//			cable.AddConnection(conn2);
//            cable.SetEndPointsToConnections();
//        }

//		if (energyKpiUpdates != null && energyKpiUpdates.Length > 0)
//			KPIManager.ReceiveEnergyKPIUpdate(energyKpiUpdates);

//        if (PlanManager.planViewing != null && !Main.InEditMode && !Main.EditingPlanDetailsContent)
//        {
//            int viewingTime = PlanManager.planViewing.StartTime;
//            foreach (KeyValuePair<AbstractLayer, int> kvp in layerUpdateTimes)
//            {
//                if (kvp.Value <= viewingTime)
//                {
//                    kvp.Key.SetEntitiesActiveUpTo(PlanManager.planViewing);
//                    kvp.Key.RedrawGameObjects(CameraManager.Instance.gameCamera);
//                }
//            }
//        }

//		if (warningUpdate != null)
//		{
//			IssueManager.instance.OnIssuesReceived(warningUpdate); //MSP-2358, ensure Warnings are processed after all the plan updates are done.
//		}

//        PlanManager.CheckIfExpectedplanReceived();
//    }
//}

public class PlanLayerObject
{
	public int layerid;     //plan layer id
	public int original;    //base layer id
	public string state;
	public List<SubEntityObject> geometry;
	public List<int> deleted;
}

public class ObjectiveObject
{
	public int objective_id;
	public int country_id;
	public string title;
	public string description;
	public int deadline;
	public bool active;
	public bool complete;
}

public class TaskObject
{
	public string sector;
	public string category;
	public string subcategory;
	public string function;
	public float value;
	public string description;
}

public class PlanObject : IComparable<PlanObject>
{
	public int id;
	public string name;
	public string description;
	public int startdate;
	public string state;
	public string previousstate;
	public int country;
	public double lastupdate;
	public string locked;
	public string active;
	public string type; // energy,fishing,shipping : ex 1,0,1
    public bool alters_energy_distribution;
	public List<PlanLayerObject> layers;
	public List<GridObject> grids;
	public List<FishingObject> fishing;
	public HashSet<int> deleted_grids;
	public string energy_error;
	public List<ApprovalObject> votes;
	public RestrictionAreaObject[] restriction_settings;

	public int CompareTo(PlanObject other)
	{
		if (other == null)
			return 1;
		if (other.startdate != startdate)
			return startdate.CompareTo(other.startdate);
		else
			return id.CompareTo(other.id);
	}
}

public class PlanMessageObject
{
	public int message_id;
	public int plan_id;
	public int team_id;

	public string user_name;
	public string message;
	public string time;
}

public class RasterUpdateObject
{
	public string raster;
	public int id;
}

public class UpdateObject
{
	public List<PlanObject> plan;
	public List<PlanMessageObject> planmessages;
	public List<RasterUpdateObject> raster;
	public KPIObject kpi;
	public EnergyObject energy;
	public WarningObject warning;
	public List<ObjectiveObject> objectives;
	public TimelineState tick;
	public double update_time; //Timestamp received from the server at which this update was accurate.
}

public class EnergyObject
{
	public List<EnergyConnectionObject> connections;
	public List<EnergyOutputObject> output;
}

public class EnergyOutputObject
{
	public int id;
	public long capacity;
	public long maxcapacity;
	public int active;
}

public class EnergyConnectionObject
{
	public string start;
	public string end;
	public string cable;
	public string coords;
	public string active;
}

public class KPIObject
{
	public EcologyKPIObject[] ecology;
	public EnergyKPIObject[] energy;
	public EcologyKPIObject[] shipping; //Because code re-use
}

public class EcologyKPIObject
{
	public string name;
	public float value;
	public int month;
	public string type;
	public double lastupdate;
}

public class EnergyKPIObject
{
	public int grid;
	public int month;
	public int country;
	public long actual;
}

public class GridObject
{
	public int id;
	public int persistent;
	public string name;
	public int active;
	public bool distribution_only;
	public List<GeomIDObject> sources;
	public List<GeomIDObject> sockets;
	public List<CountryExpectedObject> energy;
}

public class GeomIDObject
{
	public int geometry_id;
}

public class CountryExpectedObject
{
	public int country_id;
	public long expected; //Expected WHAT? Cows? Apples? 
}

public class FishingObject
{
	public int country_id;
	public string type;
	public float amount;
}

public class ApprovalObject
{
	public int country;
	public EPlanApprovalState vote;
}

public class TimelineState
{
	public string state { get; set; }
	public string month { get; set; }

	public string era_gametime { get; set; }
	public string era_realtime { get; set; }
	public string planning_era_realtime { get; set; }
	public string era_timeleft { get; set; }
	public string era_monthsdone { get; set; }
	public string era_time { get; set; }
}

public class WarningObject
{
	public List<PlanIssueObject> plan_issues;
	public List<ShippingIssueObject> shipping_issues;
}