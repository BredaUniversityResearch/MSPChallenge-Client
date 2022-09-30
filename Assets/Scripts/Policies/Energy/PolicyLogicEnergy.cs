using System.Collections;
using System;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PolicyLogicEnergy : APolicyLogic
	{
		public override void Initialise(APolicyData a_settings)
		{ }

		public override void HandlePlanUpdate(APolicyData a_data, Plan a_plan) 
		{ 
		}

		public override void HandlePreKPIUpdate(APolicyData a_data) 
		{
			//Run output update before KPI/Grid update. Source output is required for the KPIs and Capacity for grids.
			foreach (EnergyOutputObject outputUpdate in a_Update.energy.output)
			{
				UpdateOutput(outputUpdate);
			}

			//Update grids
			if (a_Update.plan != null)
			{
				for (int i = 0; i < plans.Count; i++)
				{
					plans[i].UpdateGrids(a_Update.plan[i].deleted_grids, a_Update.plan[i].grids);
				}
			}

			//Run connection update before KPI update so cable networks are accurate in the KPIs
			foreach (EnergyConnectionObject connection in a_Update.energy.connections)
			{
				UpdateConnection(connection);
			}
		}

		public override void HandlePostKPIUpdate(APolicyData a_data) 
		{
		}

		public override APolicyData FormatPlanData(Plan a_plan) 
		{
			return null;
		}

		public override void UpdateAfterEditing(Plan a_plan) { }

		private void UpdateOutput(EnergyOutputObject outputUpdate)
		{
			SubEntity tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(outputUpdate.id);
			if (tempSubEnt == null) return;
			IEnergyDataHolder energyObj = (IEnergyDataHolder)tempSubEnt;
			energyObj.UsedCapacity = outputUpdate.capacity;
			energyObj.Capacity = outputUpdate.maxcapacity;
			tempSubEnt.UpdateTextMeshText();
		}

		private void UpdateConnection(EnergyConnectionObject connection)
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
			SubEntity tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(cableID);
			if (tempSubEnt == null) return;
			EnergyLineStringSubEntity cable = tempSubEnt as EnergyLineStringSubEntity;

			//Get the points, check if they reference to a polygon or point
			tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(startID);
			if (tempSubEnt == null) return;
			else if (tempSubEnt is EnergyPolygonSubEntity)
				point1 = (tempSubEnt as EnergyPolygonSubEntity).sourcePoint;
			else
				point1 = tempSubEnt as EnergyPointSubEntity;

			tempSubEnt = LayerManager.Instance.GetEnergySubEntityByID(endID);
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
}