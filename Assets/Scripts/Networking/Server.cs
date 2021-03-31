using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

public static class Server
{
	public static string Url
	{
		get;
		private set;
	}

	public static string UrlWithoutSession
	{
		get;
		private set;
	}

	private static string protocol = "http://";
	public static string Protocol
	{
		get
		{
			return protocol;
		}
		set
		{
			protocol = value;
			UpdateUrl();
		}
	}

	private static string host = "localhost";
	public static string Host
	{
		get
		{
			return host;
		}
		set
		{
			host = value;
		}
	}

	private static string endpoint = "dev";
	public static string Endpoint
	{
		get
		{
			return endpoint;
		}
		set
		{
			endpoint = value;
			UpdateUrl();
		}
	}

	private static int gameSessionId = -1;
	public static int GameSessionId
	{
		get
		{
			return gameSessionId;
		}
		set
		{
			gameSessionId = value;
			UpdateUrl();
		}
	}

	static Server()
	{
		Url = "http://localhost/dev/";
		UrlWithoutSession = Url;
	}

	private static void UpdateUrl()
	{
		//Url = string.Format("{0}{1}/{2}/", protocol, host, endpoint);
		Url = host + '/';
		UrlWithoutSession = Url;
		if (gameSessionId > 0)
		{
			Url = string.Concat(Url, gameSessionId, "/");
		}
	}

	public static string DetectProtocolFromUrl(string wwwUrl)
	{
		if (wwwUrl.StartsWith("https://"))
		{
			return "https://";
		}

		if (wwwUrl.StartsWith("http://"))
		{
			return "http://";
		}

		UnityEngine.Debug.LogError("Unknown protocol received in url " + wwwUrl);
		return "http://";
	}

	public static string Update()
	{
		return "api/game/latest";
	}

	public static string Tick()
	{
		return "api/game/Tick";
	}

	public static string DeleteLayer()
	{
		return "api/layer/Delete";
	}

	public static string MergeLayer()
	{
		return "api/geometry/Merge";
	}

	public static string NewLayer()
	{
		return "api/layer/Post";
	}

	public static string LayerMeta()
	{
		return "api/game/meta";
	}

	public static string LayerMetaByName()
	{
		return "api/layer/MetaByName";
	}

	public static string PostLayerMeta()
	{
		return "api/layer/UpdateMeta";
	}

	public static string GetLayer()
	{
		return "api/layer/get";
	}

	public static string PostGeometry()
	{
		return "api/geometry/Post";
	}

	//public static string PostGeometrySub() //Updated to new name below
	//{
	//	return "api/geometry/AdminSubtractive";
	//}

	public static string PostGeometrySub()
	{
		return "api/geometry/PostSubtractive";
	}

	//public static string UpdateGeometry() //Updated to new name below
	//{
	//	return "api/geometry/AdminUpdate";
	//}

	public static string UpdateGeometry()
	{
		return "api/geometry/Update";
	}

	//public static string DeleteGeometry() //Updated to new name below
	//{
	//	return "api/geometry/AdminDelete";
	//}

	public static string DeleteGeometry()
	{
		return "api/geometry/Delete";
	}

	public static string MarkForDelete()
	{
		return "api/geometry/MarkForDelete";
	}

	public static string UnmarkForDelete()
	{
		return "api/geometry/UnmarkForDelete";
	}

	public static string SendGeometryData()
	{
		return "api/geometry/data";
	}

	public static string SetGameState()
	{
		return "api/game/state";
	}

	public static string SetGamePlanningTime()
	{
		return "api/game/planning";
	}

	public static string SetPlanningTimeRemaining()
	{
		return "api/game/timeleft";
	}

	public static string SetRealPlanningTime()
	{
		return "api/game/realtime";
	}

	public static string SetFuturePlanningTime()
	{
		return "api/game/FutureRealtime";
	}

	public static string SetGameSpeed()
	{
		return "api/game/speed";
	}

	public static string RequestSession()
	{
		return "api/user/RequestSession";
	}

	public static string CloseSession()
	{
		return "api/user/CloseSession";
	}

	public static string SetEndAndStartDate()
	{
		return "api/game/SetStartEndDate";
	}

	public static string IsServerOnline()
	{
		return "api/game/isOnline";
	}

	public static string PostPlan()
	{
		return "api/plan/Post";
	}

	public static string LockPlan()
	{
		return "api/plan/Lock";
	}

	public static string PostPlanFeedback()
	{
		return "api/plan/Message";
	}

	public static string UnlockPlan()
	{
		return "api/plan/Unlock";
	}

	public static string AddPlanLayer()
	{
		return "api/plan/Layer";
	}

	public static string DeletePlanLayer()
	{
		return "api/plan/DeleteLayer";
	}

	public static string RenamePlanLayer()
	{
		return "api/plan/Name";
	}

	public static string ChangePlanDate()
	{
		return "api/plan/Date";
	}

	public static string SetPlanState()
	{
		return "api/plan/SetState";
	}

	public static string SetPlanType()
	{
		return "api/plan/type";
	}

	public static string SetPlanEnergyDistribution()
	{
		return "api/plan/SetEnergyDistribution";
	}

	public static string SetPlanRestrictionAreas()
	{
		return "api/plan/SetRestrictionAreas";
	}

	public static string SetPlanDescription()
	{
		return "api/plan/description";
	}

	public static string SendIssues()
	{
		return "api/warning/post";
	}

	public static string CreateConnection()
	{
		return "api/energy/CreateConnection";
	}

	public static string UpdateConnection()
	{
		return "api/energy/UpdateConnection";
	}

	public static string DeleteEnergyConection()
	{
		return "api/energy/DeleteConnection";
	}

	public static string GetRestrictions()
	{
		return "api/plan/Restrictions";
	}
	public static string UpdateMaxCapacity()
	{
		return "api/energy/UpdateMaxCapacity";
	}

	public static string UpdateGridName()
	{
		return "api/energy/UpdateGridName";
	}

	public static string UpdateGridEnergy()
	{
		return "api/energy/UpdateGridEnergy";
	}

	public static string UpdateGridSockets()
	{
		return "api/energy/UpdateGridSockets";
	}

	public static string UpdateGridSources()
	{
		return "api/energy/UpdateGridSources";
	}

	public static string SetEnergyOutput()
	{
		return "api/energy/SetOutput";
	}

	public static string SetGridNameByGeomID()
	{
		return "api/energy/name";
	}

	public static string SetGridActualEnergy()
	{
		return "api/energy/GridActual";
	}

	public static string GetGeomUsedCapacity()
	{
		return "api/energy/GetUsedCapacity";
	}

	public static string DeleteSocket()
	{
		return "api/energy/DeleteSocket";
	}

	public static string DeleteEnergyOutput()
	{
		return "api/energy/DeleteOutput";
	}

	public static string DeleteGridName()
	{
		return "api/energy/DeleteName";
	}

	public static string DeleteGrid()
	{
		return "api/energy/DeleteGrid";
	}

	public static string AddSocket()
	{
		return "api/energy/AddSocket";
	}

	public static string AddSource()
	{
		return "api/energy/AddSource";
	}

	public static string AddEnergyGrid()
	{
		return "api/energy/AddGrid";
	}

	public static string GetCELConfig()
	{
		return "api/cel/GetCELConfig";
	}

	public static string SendFishingAmount()
	{
		return "api/plan/fishing";
	}

	public static string DeleteEnergyFromPlan()
	{
		return "api/plan/DeleteEnergy";
	}

	public static string DeleteFishingFromPlan()
	{
		return "api/plan/DeleteFishing";
	}

	public static string SetApproval()
	{
		return "api/plan/Vote";
	}
	public static string AddApproval()
	{
		return "api/plan/AddApproval";
	}

	public static string GetMELConfig()
	{
		return "api/mel/Config";
	}

	public static string ShippingKPIConfig()
	{
		return "api/sel/GetKPIDefinition";
	}

	public static string GetShippingClientConfig()
	{
		return "api/sel/GetSELGameClientConfig";
	}

	public static string DeleteObjective()
	{
		return "api/objective/delete";
	}

	public static string SendObjective()
	{
		return "api/objective/post";
	}

	public static string SetObjectiveCompleted()
	{
		return "api/objective/setCompleted";
	}

	public static string GetGlobalData()
	{
		return "api/game/Config";
	}

	public static string GetInitialFishingValues()
	{
		return "api/plan/GetInitialFishingValues";
	}

	public static string SetEnergyError()
	{
		return "api/plan/SetEnergyError";
	}

	public static string GetDependentEnergyPlans()
	{
		return "api/energy/GetDependentEnergyPlans";
	}

	public static string GetOverlappingEnergyPlans()
	{
		return "api/energy/GetOverlappingEnergyPlans";
	}

	public static string OverlapsWithPreviousEnergyPlans()
	{
		return "api/energy/GetPreviousOverlappingPlans";
	}

	public static string SetPlanRemovedGrids()
	{
		return "api/energy/SetDeleted";
	}

	public static string GetRasterUrl()
	{
		return "api/layer/GetRaster";
	}

	public static string SubmitErrorEvent()
	{
		return "api/log/Event";
	}

	public static string VerifyEnergyCapacity()
	{
		return "api/energy/VerifyEnergyCapacity";
	}

	public static string VerifyEnergyGrid()
	{
		return "api/energy/VerifyEnergyGrid";
	}

	public static string CheckApiAccess()
	{
		return "api/security/CheckAccess";
	}

	public static string RenewApiToken()
	{
		return "api/security/RequestToken";
	}

	public static string StartBatch()
	{
		return "api/batch/StartBatch";
	}

	public static string AddToBatch()
	{
		return "api/batch/AddToBatch";
	}

	public static string ExecuteBatch()
	{
		return "api/batch/ExecuteBatch";
	}
}
