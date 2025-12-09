using System;

namespace MSP2050.Scripts
{
	public static class Server
	{
		public static Uri WsServerUri = new Uri("ws://localhost:45001");

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
			Url = host;
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
			return "api/Game/Latest";
		}

		public static string DeleteLayer()
		{
			return "api/Layer/Delete";
		}

		public static string MergeLayer()
		{
			return "api/Geometry/Merge";
		}

		public static string NewLayer()
		{
			return "api/Layer/Post";
		}

		public static string LayerMeta()
		{
			return "api/Game/Meta";
		}

		public static string PolicySimSettings()
		{
			return "api/Game/PolicySimSettings";
		}

		public static string LayerMetaByName()
		{
			return "api/Layer/MetaByName";
		}

		public static string PostLayerMeta()
		{
			return "api/Layer/UpdateMeta";
		}

		public static string GetLayer()
		{
			return "api/Layer/Get";
		}

		public static string PostGeometry()
		{
			return "api/Geometry/Post";
		}

		public static string PostGeometrySub()
		{
			return "api/Geometry/PostSubtractive";
		}

		public static string UpdateGeometry()
		{
			return "api/Geometry/Update";
		}

		public static string DeleteGeometry()
		{
			return "api/Geometry/Delete";
		}

		public static string MarkForDelete()
		{
			return "api/Geometry/MarkForDelete";
		}

		public static string UnmarkForDelete()
		{
			return "api/Geometry/UnmarkForDelete";
		}

		public static string SendGeometryData()
		{
			return "api/Geometry/Data";
		}

		public static string SetGameState()
		{
			return "api/Game/State";
		}

		public static string SetGamePlanningTime()
		{
			return "api/Game/Planning";
		}

		public static string SetRealPlanningTime()
		{
			return "api/Game/Realtime";
		}

		public static string SetFuturePlanningTime()
		{
			return "api/Game/FutureRealtime";
		}

		public static string SetGameSpeed()
		{
			return "api/Game/Speed";
		}

		public static string RequestSession()
		{
			return "api/User/RequestSession";
		}

		public static string CloseSession()
		{
			return "api/User/CloseSession";
		}

		public static string GetUserList()
		{
			return "api/User/List";
		}

		public static string IsServerOnline()
		{
			return "api/Game/IsOnline";
		}

		public static string PostPlan()
		{
			return "api/Plan/Post";
		}

		public static string LockPlan()
		{
			return "api/Plan/Lock";
		}

		public static string PostPlanFeedback()
		{
			return "api/Plan/Message";
		}

		public static string UnlockPlan()
		{
			return "api/Plan/Unlock";
		}

		public static string AddPlanLayer()
		{
			return "api/Plan/Layer";
		}

		public static string DeletePlanLayer()
		{
			return "api/Plan/DeleteLayer";
		}

		public static string RenamePlan()
		{
			return "api/Plan/Name";
		}

		public static string ChangePlanDate()
		{
			return "api/Plan/Date";
		}

		public static string SetPlanState()
		{
			return "api/Plan/SetState";
		}

		public static string SetPlanType()
		{
			return "api/Plan/Type";
		}

		public static string SetPlanEnergyDistribution()
		{
			return "api/Plan/SetEnergyDistribution";
		}

		public static string SetPlanRestrictionAreas()
		{
			return "api/Plan/SetRestrictionAreas";
		}

		public static string SetPlanDescription()
		{
			return "api/Plan/Description";
		}

		public static string SendIssues()
		{
			return "api/Warning/Post";
		}

		public static string CreateConnection()
		{
			return "api/Energy/CreateConnection";
		}

		public static string UpdateConnection()
		{
			return "api/Energy/UpdateConnection";
		}

		public static string DeleteEnergyConnection()
		{
			return "api/Energy/DeleteConnection";
		}

		public static string GetRestrictions()
		{
			return "api/Plan/Restrictions";
		}

		public static string SetGeneralPolicyData()
		{
			return "api/Plan/SetGeneralPolicyData";
		}

		public static string DeleteGeneralPolicy()
		{
			return "api/Plan/DeleteGeneralPolicy";
		}

		public static string UpdateMaxCapacity()
		{
			return "api/Energy/UpdateMaxCapacity";
		}

		public static string UpdateGridName()
		{
			return "api/Energy/UpdateGridName";
		}

		public static string UpdateGridEnergy()
		{
			return "api/Energy/UpdateGridEnergy";
		}

		public static string UpdateGridSockets()
		{
			return "api/Energy/UpdateGridSockets";
		}

		public static string UpdateGridSources()
		{
			return "api/Energy/UpdateGridSources";
		}

		public static string SetEnergyOutput()
		{
			return "api/Energy/SetOutput";
		}

		public static string SetGridNameByGeomID()
		{
			return "api/Energy/Name";
		}

		public static string SetGridActualEnergy()
		{
			return "api/Energy/GridActual";
		}

		public static string GetGeomUsedCapacity()
		{
			return "api/Energy/GetUsedCapacity";
		}

		public static string DeleteSocket()
		{
			return "api/Energy/DeleteSocket";
		}

		public static string DeleteEnergyOutput()
		{
			return "api/Energy/DeleteOutput";
		}

		public static string DeleteGridName()
		{
			return "api/Energy/DeleteName";
		}

		public static string DeleteGrid()
		{
			return "api/Energy/DeleteGrid";
		}

		public static string AddSocket()
		{
			return "api/Energy/AddSocket";
		}

		public static string AddSource()
		{
			return "api/Energy/AddSource";
		}

		public static string AddEnergyGrid()
		{
			return "api/Energy/AddGrid";
		}

		//public static string GetCELConfig()
		//{
		//	return "api/cel/GetCELConfig";
		//}

		public static string SendFishingAmount()
		{
			return "api/Plan/Fishing";
		}

		public static string DeleteEnergyFromPlan()
		{
			return "api/Plan/DeleteEnergy";
		}

		public static string DeleteFishingFromPlan()
		{
			return "api/Plan/DeleteFishing";
		}

		public static string SetApproval()
		{
			return "api/Plan/Vote";
		}
		public static string AddApproval()
		{
			return "api/Plan/AddApproval";
		}

		//public static string GetMELConfig()
		//{
		//	return "api/mel/Config";
		//}

		//public static string ShippingKPIConfig()
		//{
		//	return "api/sel/GetKPIDefinition";
		//}

		//public static string GetShippingClientConfig()
		//{
		//	return "api/sel/GetSELGameClientConfig";
		//}

		public static string DeleteObjective()
		{
			return "api/objective/Delete";
		}

		public static string SendObjective()
		{
			return "api/objective/Post";
		}

		public static string SetObjectiveCompleted()
		{
			return "api/objective/SetCompleted";
		}

		public static string GetGlobalData()
		{
			return "api/Game/Config";
		}

		public static string SetEnergyError()
		{
			return "api/Plan/SetEnergyError";
		}

		public static string GetDependentEnergyPlans()
		{
			return "api/Energy/GetDependentEnergyPlans";
		}

		public static string GetOverlappingEnergyPlans()
		{
			return "api/Energy/GetOverlappingEnergyPlans";
		}

		public static string OverlapsWithPreviousEnergyPlans()
		{
			return "api/Energy/GetPreviousOverlappingPlans";
		}

		public static string SetPlanRemovedGrids()
		{
			return "api/Energy/SetDeleted";
		}

		public static string GetRasterUrl()
		{
			return "api/Layer/GetRaster";
		}

		public static string SubmitErrorEvent()
		{
			return "api/log/Event";
		}

		public static string VerifyEnergyCapacity()
		{
			return "api/Energy/VerifyEnergyCapacity";
		}

		public static string VerifyEnergyGrid()
		{
			return "api/Energy/VerifyEnergyGrid";
		}

		public static string RefreshApiToken()
		{
			return "api/User/RequestToken";
		}

		public static string ExecuteBatch()
		{
			return "api/Batch/ExecuteBatch";
		}

		public static string ImmersiveSessions()
		{
			return "api/immersive_sessions";
		}
	}
}
