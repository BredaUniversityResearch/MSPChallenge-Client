using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Networking;
using Networking.WsServerConnectionChangeBehaviour;
using Sirenix.Utilities;
using Object = UnityEngine.Object;

public static class UpdateData
{
	private const float UpdateSpeed = 1.0f;
	private static DialogBox m_DisconnectDialogBox = null;
	private static double m_LastUpdateTimestamp = -1;
	public static double LastUpdateTimeStamp => m_LastUpdateTimestamp;
	public static UpdateObject LastUpdate;
	public static bool StopProcessingUpdates = false;
	
	public static bool? WsServerConnected = null;
	private static WsServerCommunication m_WsServerCommunication;
	private static readonly Queue<UpdateObject> m_NextUpdates = new Queue<UpdateObject>();

	public static IEnumerator GetFirstUpdate()
	{
		m_WsServerCommunication = new WsServerCommunication(
			Server.GameSessionId,
			TeamManager.CurrentUserTeamID,
			TeamManager.CurrentSessionID.ToString(),
			HandleUpdateSuccessCallback
		);
		m_WsServerCommunication.Start();

		while (m_NextUpdates.Count == 0)
		{
			HandleWsServerConnectionChanges();
			yield return null;
		}

		ProcessUpdates(m_NextUpdates);
		HideDisconnectedDialogBox();
		Main.FirstUpdateTickComplete();
	}
	
	public static void StopWsServerCommunication()
	{
		m_WsServerCommunication.Stop();
	}

	public static IEnumerator GetUpdates()
	{
		while (true)
		{
			while (m_NextUpdates.Count == 0)
			{
				HandleWsServerConnectionChanges();
				yield return null;
			}
			
			ProcessUpdates(m_NextUpdates);
			HideDisconnectedDialogBox();
	
			yield return new WaitForSeconds(UpdateSpeed);
		}
	}
	
	private static void HandleUpdateSuccessCallback(UpdateObject a_UpdateData)
	{
		m_NextUpdates.Enqueue(a_UpdateData);
	}

	private static void ShowDisconnectedDialogBox()
	{
		if (m_DisconnectDialogBox == null)
		{
			m_DisconnectDialogBox = DialogBoxManager.instance.NotificationWindow("Disconnected", "Your connection to the server has been interrupted.\n\nHold on while we are trying to re-establish the connection.",
				() => {
                    Main.QuitGame();
                }, "Close Game");
		}
	}

	private static void HideDisconnectedDialogBox()
	{
		if (m_DisconnectDialogBox != null)
		{
			DialogBoxManager.instance.DestroyDialogBox(m_DisconnectDialogBox);
		}
	}

	private static void HandleWsServerConnectionChanges()
	{
		if (WsServerConnected == m_WsServerCommunication.IsConnected)
		{
			return;
		}

		WsServerConnected = m_WsServerCommunication.IsConnected;
		if (WsServerConnected == null) // no connection value yet
		{
			return;
		}

		Object.FindObjectsOfType<WsServerConnectionChangeBehaviour>().ForEach(item =>
			item.NotifyConnection(WsServerConnected.Value));
	}

	private static void ProcessUpdates(Queue<UpdateObject> a_Updates)
	{
		if (a_Updates.Count == 0)
		{
			return; // this should never happen...
		}

		// process next in queue. Note that is not a while loop since we only want to do a single update per client tick
		//  This is because currently, multiple updates per tick cause skipping of essential plan unlocks..
		ProcessUpdates(a_Updates.Dequeue());
	}

	private static void ProcessUpdates(UpdateObject a_Update)
	{
		HandleWsServerConnectionChanges();
		if (StopProcessingUpdates || a_Update.update_time <= m_LastUpdateTimestamp)
		{
			Debug.Log("stopProcessingUpdates: " + StopProcessingUpdates + ", update.update_time <= lastUpdateTimestamp: " + (a_Update.update_time <= m_LastUpdateTimestamp));
			return;
		}

		m_LastUpdateTimestamp = a_Update.update_time;
		LastUpdate = a_Update;

		Dictionary<AbstractLayer, int> layerUpdateTimes = new Dictionary<AbstractLayer, int>();
		List<Plan> plans = null;

		if (a_Update.plan != null)
		{
			//Sort plans by time and ID so there are no issues with dependencies when loading them in
			a_Update.plan.Sort();
			plans = new List<Plan>(a_Update.plan.Count);
			foreach (PlanObject plan in a_Update.plan)
			{
				plans.Add(PlanManager.ProcessReceivedPlan(plan, layerUpdateTimes));
			}
		}

		foreach (RasterUpdateObject raster in a_Update.raster)
		{
			AbstractLayer layer = LayerManager.GetLayerByID(raster.id);

			RasterLayer rasterLayer = layer as RasterLayer;
			if (rasterLayer != null && LayerManager.LayerIsVisible(rasterLayer))
			{
				rasterLayer.ReloadLatestRaster();
			}
		}

		//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
		foreach (EnergyOutputObject outputUpdate in a_Update.energy.output)
		{
			UpdateOutput(outputUpdate);
		}

		//Update grids
		if (a_Update.plan != null)
		{
			for(int i = 0; i < plans.Count; i++)
			{
				plans[i].UpdateGrids(a_Update.plan[i].deleted_grids, a_Update.plan[i].grids);
			}
		}

		//Run connection update before KPI update so cable networks are accurate in the KPIs
		foreach (EnergyConnectionObject connection in a_Update.energy.connections)
		{
			UpdateConnection(connection);
		}

		GameState.UpdateTime(a_Update.tick);

		if (a_Update.kpi != null)
		{
			if (a_Update.kpi.energy != null && a_Update.kpi.energy.Length > 0)
			{
				KPIManager.ReceiveEnergyKPIUpdate(a_Update.kpi.energy);
			}

			if (a_Update.kpi.ecology != null && a_Update.kpi.ecology.Length > 0)
			{
				KPIManager.ReceiveEcologyKPIUpdate(a_Update.kpi.ecology);
			}

			if (a_Update.kpi.shipping != null && a_Update.kpi.shipping.Length > 0)
			{
				KPIManager.ReceiveShippingKPIUpdate(a_Update.kpi.shipping);
			}
		}

		if (a_Update.objectives.Count > 0)
		{
			InterfaceCanvas.Instance.objectivesMonitor.UpdateObjectivesFromServer(a_Update.objectives);
		}

		PlanDetails.AddFeedbackFromServer(a_Update.planmessages);

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

		if (a_Update.warning != null)
		{
			IssueManager.instance.OnIssuesReceived(a_Update.warning); //MSP-2358, ensure Warnings are processed after all the plan updates are done.
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
	public double prev_update_time;
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