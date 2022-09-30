using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;
using Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace MSP2050.Scripts
{
	public class UpdateManager : MonoBehaviour
	{
		private static UpdateManager m_singleton;
		public static UpdateManager Instance
		{
			get
			{
				if (m_singleton == null)
					m_singleton = FindObjectOfType<UpdateManager>();
				return m_singleton;
			}
		}

		private double m_LastUpdateTimestamp = -1;
		public double LastUpdateTimeStamp => m_LastUpdateTimestamp;
		public UpdateObject LastUpdate;
		public bool StopProcessingUpdates = false;
	
		private bool? m_WsServerConnected;

		[CanBeNull]
		public IWsServerCommunicationInteractor WsServerCommunicationInteractor => m_WsServerCommunication;

		private WsServerCommunication m_WsServerCommunication;
		private readonly Queue<UpdateObject> m_NextUpdates = new Queue<UpdateObject>();

		void Start()
		{
			if (m_singleton != null && m_singleton != this)
				Destroy(this);
			else
				m_singleton = this;
		}

		void OnDestroy()
		{
			m_singleton = null;
		}

		public IEnumerator GetFirstUpdate()
		{
			m_WsServerCommunication = new WsServerCommunication(
				Server.GameSessionId,
				SessionManager.Instance.CurrentUserTeamID,
				SessionManager.Instance.CurrentSessionID,
				HandleUpdateSuccessCallback
			);
			m_WsServerCommunication.Start();

			// wait for a first update(s) to arrive
			while (m_NextUpdates.Count == 0)
			{
				HandleWsServerConnectionChanges();
				yield return null;
			}

			// process the first update(s)
			ProcessUpdates(m_NextUpdates);
			Main.Instance.FirstUpdateTickComplete();
		}

		public IEnumerator GetUpdates()
		{
			while (true)
			{
				HandleWsServerConnectionChanges();
				m_WsServerCommunication.Update();
				ProcessUpdates(m_NextUpdates);
				yield return null;
			}
		}
	
		private void HandleUpdateSuccessCallback(UpdateObject a_UpdateData)
		{
			m_NextUpdates.Enqueue(a_UpdateData);
		}

		private void HandleWsServerConnectionChanges()
		{
			if (m_WsServerConnected == m_WsServerCommunication.IsConnected())
			{
				return;
			}

			m_WsServerConnected = m_WsServerCommunication.IsConnected();
			if (m_WsServerConnected == null) // no connection value yet
			{
				return;
			}

			Object.FindObjectsOfType<WsServerConnectionChangeBehaviour>().ForEach(item =>
				item.NotifyConnection(m_WsServerConnected.Value));
		}

		private void ProcessUpdates(Queue<UpdateObject> a_Updates)
		{
			if (a_Updates.Count == 0)
			{
				return;
			}

			Debug.Log("Updates to process: " + a_Updates.Count);

			// process next in queue. Note that is not a while loop since we only want to do a single update per client tick
			//  This is because currently, multiple updates per tick cause skipping of essential plan unlocks..
			ProcessUpdates(a_Updates.Dequeue());
		}

		private void ProcessUpdates(UpdateObject a_Update)
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
					plans.Add(PlanManager.Instance.ProcessReceivedPlan(plan, layerUpdateTimes));
				}
			}

			foreach (RasterUpdateObject raster in a_Update.raster)
			{
				AbstractLayer layer = LayerManager.Instance.GetLayerByID(raster.id);

				RasterLayer rasterLayer = layer as RasterLayer;
				if (rasterLayer != null && LayerManager.Instance.LayerIsVisible(rasterLayer))
				{
					rasterLayer.ReloadLatestRaster();
				}
			}

			PolicyManager.Instance.RunPreSimulationUpdate(a_Update.policy_updates);

			//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
			//foreach (EnergyOutputObject outputUpdate in a_Update.energy.output)
			//{
			//	UpdateOutput(outputUpdate);
			//}

			////Update grids
			//if (a_Update.plan != null)
			//{
			//	for(int i = 0; i < plans.Count; i++)
			//	{
			//		plans[i].UpdateGrids(a_Update.plan[i].deleted_grids, a_Update.plan[i].grids);
			//	}
			//}

			////Run connection update before KPI update so cable networks are accurate in the KPIs
			//foreach (EnergyConnectionObject connection in a_Update.energy.connections)
			//{
			//	UpdateConnection(connection);
			//}

			TimeManager.Instance.UpdateTime(a_Update.tick);

			SimulationManager.Instance.RunGeneralUpdate(a_Update.simulation_updates);

			//if (a_Update.kpi != null)
			//{
			//	if (a_Update.kpi.energy != null && a_Update.kpi.energy.Length > 0)
			//	{
			//		KPIManager.Instance.ReceiveEnergyKPIUpdate(a_Update.kpi.energy);
			//	}

			//	if (a_Update.kpi.ecology != null && a_Update.kpi.ecology.Length > 0)
			//	{
			//		KPIManager.Instance.ReceiveEcologyKPIUpdate(a_Update.kpi.ecology);
			//	}

			//	if (a_Update.kpi.shipping != null && a_Update.kpi.shipping.Length > 0)
			//	{
			//		KPIManager.Instance.ReceiveShippingKPIUpdate(a_Update.kpi.shipping);
			//	}
			//}

			PolicyManager.Instance.RunPostSimulationUpdate(a_Update.policy_updates);

			if (a_Update.objectives.Count > 0)
			{
				InterfaceCanvas.Instance.objectivesMonitor.UpdateObjectivesFromServer(a_Update.objectives);
			}

			PlanDetails.AddFeedbackFromServer(a_Update.planmessages);

			if (PlanManager.Instance.planViewing != null && !Main.InEditMode)
			{
				int viewingTime = PlanManager.Instance.planViewing.StartTime;
				foreach (KeyValuePair<AbstractLayer, int> kvp in layerUpdateTimes)
				{
					if (kvp.Value <= viewingTime)
					{
						kvp.Key.SetEntitiesActiveUpTo(PlanManager.Instance.planViewing);
						kvp.Key.RedrawGameObjects(CameraManager.Instance.gameCamera);
					}
				}
			}

			if (a_Update.plan_issues != null)
			{
				IssueManager.Instance.OnIssuesReceived(a_Update.plan_issues);
			}

			PlanManager.Instance.CheckIfExpectedPlanReceived();
		}
	}
	
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
		public List<PlanLayerObject> layers;
		public List<ApprovalObject> votes;
		[JsonConverter(typeof(PolicyPlanUpdateJsonConverter))]
		public APolicyData[] policies;
		//public string type; // energy,fishing,shipping : ex 1,0,1
		//public bool alters_energy_distribution;
		//public List<GridObject> grids;
		//public List<FishingObject> fishing;
		//public HashSet<int> deleted_grids;
		//public string energy_error;
		//public RestrictionAreaObject[] restriction_settings;

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
		//public KPIObject kpi;
		//public EnergyObject energy;
		//public WarningObject warning;
		public List<ObjectiveObject> objectives;
		public TimelineState tick;
		public double prev_update_time;
		public double update_time; //Timestamp received from the server at which this update was accurate.

		public List<PlanIssueObject> plan_issues; //Moved from WarningObject
		[JsonConverter(typeof(PolicyUpdateJsonConverter))]
		public APolicyData[] policy_updates;
		[JsonConverter(typeof(SimulationUpdateJsonConverter))]
		public ASimulationData[] simulation_updates;
	}

	public class KPIObject
	{
		public string name;
		public float value;
		public int month;
		//public string type;
		public double lastupdate;
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
}