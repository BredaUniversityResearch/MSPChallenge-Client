using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class EnergyGrid
{
	public enum GridColor { Green, Grey, Either };
	public enum GridPlanState { Normal, Hidden, Added, Removed, Changed };

	public List<EnergyPointSubEntity> sources;
	public List<EnergyPointSubEntity> sockets;
	public long sourcePower;     //Only power from sources
	public long sharedPower;     //Power from negative expected power
	public long AvailablePower { get { return sourcePower + sharedPower; } }

	public long maxCountryCapacity;
	public GridEnergyDistribution energyDistribution; // <countryid, (expectedEnergy, actualEnergy, maximum, sourceinput)>
	public GridActualAndWasted actualAndWasted;
	public bool distributionOnly;   //Has only the distribution been changed, or also the sources?
	public Plan plan;

	public string name = "New Grid";
	public int persistentID = -1;
	private int databaseID;
	private bool databaseIDSet;
	private int creationBatchCallID; //ID of the PostEmptyGrid call in the batch

	/// <summary>
	/// Makes a value copy of the given energy grid, with the new plan.
	/// </summary>
	public EnergyGrid(EnergyGrid gridToDuplicate, Plan plan)
	{
		sources = new List<EnergyPointSubEntity>(gridToDuplicate.sources);
		sockets = new List<EnergyPointSubEntity>(gridToDuplicate.sockets);
		sourcePower = gridToDuplicate.sourcePower;
		sharedPower = gridToDuplicate.sharedPower;
		maxCountryCapacity = gridToDuplicate.maxCountryCapacity;
		this.plan = plan;
		energyDistribution = new GridEnergyDistribution(gridToDuplicate.energyDistribution);
		persistentID = gridToDuplicate.persistentID;
		name = gridToDuplicate.name;
	}

	/// <summary>
	/// Goes through the energy network from the starting points connections and creates a new grid
	/// </summary>
	/// <param name="startSocket">A point in the grid</param>
	public EnergyGrid(EnergyPointSubEntity startSocket, Plan plan)
	{
		this.plan = plan;
		DetermineContents(startSocket);
	}

	/// <summary>
	/// Creates a new grid where the sources, sockets and max capacities are calculated from the given network. 
	/// Expected values are taken from the GridObject.
	/// </summary>
	public EnergyGrid(GridObject gridObject, Plan plan)
	{
		this.plan = plan;
		SetDatabaseID(gridObject.id);
		persistentID = gridObject.persistent;
		distributionOnly = gridObject.distribution_only;
		name = gridObject.name;
		energyDistribution = new GridEnergyDistribution(new Dictionary<int, CountryEnergyAmount>());

		//Find all sources
		sources = new List<EnergyPointSubEntity>();
		sourcePower = 0;
		foreach (GeomIDObject source in gridObject.sources)
		{
			EnergyPointSubEntity subEnt = LayerManager.GetEnergySubEntityByID(source.geometry_id, true) as EnergyPointSubEntity;
			if (subEnt != null)
			{
				sources.Add(subEnt);
				if (energyDistribution.distribution.ContainsKey(subEnt.Entity.Country))
					energyDistribution.distribution[subEnt.Entity.Country].sourceInput += subEnt.Capacity;
				else
					energyDistribution.distribution.Add(subEnt.Entity.Country, new CountryEnergyAmount(0, subEnt.Capacity));
				sourcePower += subEnt.Capacity;
			}
			else
				Debug.LogError(String.Format("Grid (id: {0}) is expecting source with db id: {1}, but it can't be found.", databaseID, source.geometry_id));
		}

		//Find all sockets
		sockets = new List<EnergyPointSubEntity>();
		foreach (GeomIDObject socket in gridObject.sockets)
		{
			EnergyPointSubEntity subEnt = LayerManager.GetEnergySubEntityByID(socket.geometry_id) as EnergyPointSubEntity;
			if (subEnt != null)
			{
				sockets.Add(subEnt);
                long newMaximum = subEnt.Capacity;
				if (energyDistribution.distribution.ContainsKey(subEnt.Entity.Country))
					newMaximum = energyDistribution.distribution[subEnt.Entity.Country].maximum += subEnt.Capacity; //Add maxcap to country cap and store new value
				else
					energyDistribution.distribution.Add(subEnt.Entity.Country, new CountryEnergyAmount(subEnt.Capacity));
				if (newMaximum > maxCountryCapacity)
					maxCountryCapacity = newMaximum;
			}
			else
				Debug.LogError(String.Format("Grid (id: {0}) is expecting socket with db id: {1}, but it can't be found.", databaseID, socket.geometry_id));
		}

		if(sockets.Count < 2 && sources.Count == 0)
		{
			Debug.LogError($"Grid received with a single socket and no sources. This should be impossible. Grid id: {databaseID}");
		}

		//Set expected values to those in the GridObject
		sharedPower = 0;
		foreach (CountryExpectedObject countryExpected in gridObject.energy)
		{
			CountryEnergyAmount energyAmount;
			if (!energyDistribution.distribution.TryGetValue(countryExpected.country_id, out energyAmount))
			{
				energyAmount = new CountryEnergyAmount(countryExpected.expected, countryExpected.expected, countryExpected.expected);
				energyDistribution.distribution.Add(countryExpected.country_id, energyAmount);
			}

			energyAmount.expected = countryExpected.expected;
			if (countryExpected.expected < 0)
			{
				sharedPower -= countryExpected.expected;
			}
		}
	}

	private void DetermineContents(EnergyPointSubEntity startSocket)
	{
		//Setup data structures
		sources = new List<EnergyPointSubEntity>();
		sockets = new List<EnergyPointSubEntity>();
		HashSet<EnergyPointSubEntity> visited = new HashSet<EnergyPointSubEntity>();
		Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

		//Add starting point
		stack.Push(startSocket);
		sockets.Add(startSocket);
		visited.Add(startSocket);

		//Find all sockets and sources in the grid
		while (stack.Count > 0)
		{
			EnergyPointSubEntity current = stack.Pop();
			foreach (Connection con in current.connections)
			{
				EnergyPointSubEntity other = con.cable.GetConnection(!con.connectedToFirst).point;
				if (!visited.Contains(other))
				{
					stack.Push(other);
					visited.Add(other);
					if (other.Entity.Layer.editingType == AbstractLayer.EditingType.Socket)
						sockets.Add(other);
					else if (other.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePoint || other.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
					{
						sources.Add(other);
						sourcePower += other.Capacity;
					}
				}
			}
		}
	}

	//private void DetermineContents(EnergyPointSubEntity startSocket, Dictionary<int, List<EnergyPointSubEntity>> network)
	//{
	//	//Setup data structures
	//	sources = new List<EnergyPointSubEntity>();
	//	sockets = new List<EnergyPointSubEntity>();
	//	HashSet<int> visited = new HashSet<int>();
	//	Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

	//	//Add starting point
	//	stack.Push(startSocket);
	//	sockets.Add(startSocket);
	//	visited.Add(startSocket.GetDatabaseID());

	//	//Find all sockets and sources in the grid
	//	while (stack.Count > 0)
	//	{
	//		EnergyPointSubEntity current = stack.Pop();

	//		foreach (EnergyPointSubEntity other in network[current.GetDatabaseID()])
	//		{
	//			if (!visited.Contains(other.GetDatabaseID()))
	//			{
	//				stack.Push(other);
	//				visited.Add(other.GetDatabaseID());
	//				if (other.Entity.Layer.editingType == AbstractLayer.EditingType.Socket)
	//					sockets.Add(other);
	//				else if (other.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePoint || other.Entity.Layer.editingType == AbstractLayer.EditingType.SourcePolygonPoint)
	//				{
	//					sources.Add(other);
	//					sourcePower += other.Capacity;
	//				}
	//			}
	//		}
	//	}
	//}

	public void SetAsCurrentGridForContent()
	{
		HashSet<int> visited = new HashSet<int>();
		Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

		//Add starting point
		stack.Push(sockets[0]);
		visited.Add(sockets[0].GetDatabaseID());

		//Find all sockets and sources in the grid
		while (stack.Count > 0)
		{
			EnergyPointSubEntity current = stack.Pop();
			current.CurrentGrid = this;

			foreach (Connection con in current.connections)
			{
				if (!visited.Contains(con.cable.GetDatabaseID()))
				{
					con.cable.CurrentGrid = this;
					visited.Add(con.cable.GetDatabaseID());
					EnergyPointSubEntity other = con.cable.GetConnection(!con.connectedToFirst).point;
					if (!visited.Contains(other.GetDatabaseID()))
					{
						stack.Push(other);
						visited.Add(other.GetDatabaseID());
					}
				}
			}
		}
	}

	public void SetAsLastRunGridForContent(Dictionary<int, List<DirectionalConnection>> cableNetwork)
	{
		if (cableNetwork == null)
			return;

		HashSet<int> visited = new HashSet<int>();
		Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

		//Add starting point
		stack.Push(sockets[0]);
		visited.Add(sockets[0].GetDatabaseID());
		while (stack.Count > 0)
		{
			EnergyPointSubEntity current = stack.Pop();
			current.LastRunGrid = this;

			if (cableNetwork.ContainsKey(current.GetDatabaseID()))
			{
				foreach (DirectionalConnection other in cableNetwork[current.GetDatabaseID()])
				{
					if (!visited.Contains(other.cable.GetDatabaseID()))
					{
						other.cable.LastRunGrid = this;
						visited.Add(other.cable.GetDatabaseID());
						if (!visited.Contains(other.point.GetDatabaseID()))
						{
							stack.Push(other.point);
							visited.Add(other.point.GetDatabaseID());
						}
					}
				}
			}
		}
	}

	public void SetAsCurrentGridForContent(Dictionary<int, List<DirectionalConnection>> cableNetwork)
	{
		if (cableNetwork == null)
			return;

		if (sockets.Count == 0)
		{
			//Without any sockets can't do anything.
			Debug.LogWarning("Tried to focus a grid that has no sockets.");
			return; 
		}

		HashSet<int> visited = new HashSet<int>();
		Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

		//Add starting point
		stack.Push(sockets[0]);
		visited.Add(sockets[0].GetDatabaseID());
		while (stack.Count > 0)
		{
			EnergyPointSubEntity current = stack.Pop();
			current.CurrentGrid = this;

			if (cableNetwork.ContainsKey(current.GetDatabaseID()))
			{
				foreach (DirectionalConnection other in cableNetwork[current.GetDatabaseID()])
				{
					if (!visited.Contains(other.cable.GetDatabaseID()))
					{
						other.cable.CurrentGrid = this;
						visited.Add(other.cable.GetDatabaseID());
						if (!visited.Contains(other.point.GetDatabaseID()))
						{
							stack.Push(other.point);
							visited.Add(other.point.GetDatabaseID());
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Sends the grid to the server, including the name but not the energy distribution.
	/// </summary>
	public void SubmitEmptyGridToServer(BatchRequest batch)
	{
		if (databaseIDSet)
			return;

		//Add new grid on server and get databaseID
		JObject dataObject = new JObject();
		dataObject.Add("name", name);
		dataObject.Add("plan", plan.ID);
		dataObject.Add("distribution_only", JsonConvert.SerializeObject(distributionOnly));
		if (persistentID != -1)
			dataObject.Add("persistent", persistentID);
		creationBatchCallID = batch.AddRequest<int>(Server.AddEnergyGrid(), dataObject, BatchRequest.BATCH_GROUP_GRID_ADD, AddGridCallback);
	}

	public void AddGridCallback(int newDatabaseID)
	{
		SetDatabaseID(newDatabaseID);
		if (persistentID == -1)
			persistentID = databaseID;
	}

	public void SetDatabaseID(int value)
	{
		databaseID = value;
		databaseIDSet = true;
		PlanManager.AddEnergyGrid(this);
	}

	public int GetDatabaseID()
	{
		return databaseID;
	}

	public bool DatabaseIDSet()
	{
		return databaseIDSet;
	}

	public static void SubmitGridDeletionToServer(int databaseID, BatchRequest batch)
	{
		//Delete grid by databbaseID
		JObject dataObject = new JObject();
		dataObject.Add("id", databaseID);
		batch.AddRequest(Server.DeleteGrid(), dataObject, BatchRequest.BATCH_GROUP_GRID_DELETE);
	}

	/// <summary>
	/// Submits the expected energy values of the countries in the grid to the server.
	/// </summary>
	public void SubmitEnergyDistribution(BatchRequest batch)
	{
		string dbOrBatchID = databaseIDSet ? databaseID.ToString() : BatchRequest.FormatCallIDReference(creationBatchCallID);

		//Update grid_socket
		//string socketsString = "";
		//foreach (EnergyPointSubEntity socket in sockets)
		//	socketsString += socket.GetDatabaseID().ToString() + ",";
		//if (socketsString.Length > 0)
		//	socketsString = socketsString.Substring(0, socketsString.Length - 1);

		List<string> socketIDs = new List<string>(sockets.Count);
		foreach (EnergyPointSubEntity socket in sockets)
			socketIDs.Add(socket.GetDataBaseOrBatchIDReference());

		JObject dataObject = new JObject();
		dataObject.Add("id", dbOrBatchID);
		dataObject.Add("sockets", JToken.FromObject(socketIDs));
		//dataObject.Add("sockets", JToken.FromObject(socketIDs));
		batch.AddRequest(Server.UpdateGridSockets(), dataObject, BatchRequest.BATCH_GROUP_GRID_CONTENT);

		//Update grid_source
		//string sourceString = "";
		//foreach (EnergyPointSubEntity source in sources)
		//	sourceString += source.GetDatabaseID().ToString() + ",";
		//if (sourceString.Length > 0)
		//	sourceString = sourceString.Substring(0, sourceString.Length - 1);

		List<string> sourceIDs = new List<string>(sources.Count);
		foreach (EnergyPointSubEntity source in sources)
			sourceIDs.Add(source.GetDataBaseOrBatchIDReference());

		dataObject = new JObject();
		dataObject.Add("id", dbOrBatchID);
		if (sourceIDs.Count > 0)
			//dataObject.Add("sources", JToken.FromObject(sourceIDs));
			dataObject.Add("sources", JToken.FromObject(sourceIDs));
		batch.AddRequest(Server.UpdateGridSources(), dataObject, BatchRequest.BATCH_GROUP_GRID_CONTENT);

		//Update grid_energy 
		List<EnergyExpected> expected = new List<EnergyExpected>(energyDistribution.distribution.Count);
		foreach (KeyValuePair<int, CountryEnergyAmount> kvp in energyDistribution.distribution)
			expected.Add(new EnergyExpected(kvp.Key, kvp.Value.expected));

		dataObject = new JObject();
		dataObject.Add("id", dbOrBatchID);
		dataObject.Add("expected", JToken.FromObject(expected));
		//dataObject.Add("expected", JToken.FromObject(expected));
		batch.AddRequest(Server.UpdateGridEnergy(), dataObject, BatchRequest.BATCH_GROUP_GRID_CONTENT);

	}

	/// <summary>
	/// Sets the grids name. Sends it to the server if it already has a database id. 
	/// Otherwise it will automatically be sent when the grid is sent to the server.
	/// </summary>
	public void SetName(string newName)
	{
		if (databaseIDSet)
		{
			//Submit new name to server (for databaseID)
			NetworkForm form = new NetworkForm();
			form.AddField("id", databaseID);
			form.AddField("name", name);
			ServerCommunication.DoRequest(Server.UpdateGridName(), form);
		}
	}

	/// <summary>
	/// Creates a new distribution with the right countries.
	/// Maximum is set correctly, expected is set to a valid estimation.
	/// </summary>
	public void CalculateInitialDistribution(EnergyGrid oldGrid = null)
	{
		if (energyDistribution != null)
			return;

        long totalCountryCapacity = 0;
		maxCountryCapacity = 0;
		Dictionary<int, CountryEnergyAmount> distribution = new Dictionary<int, CountryEnergyAmount>();

		//Add sockets and calculate socket capacities
		foreach (EnergyPointSubEntity socket in sockets)
		{
            int country = socket.Entity.Country;
            if (country <= TeamManager.AM_ID)
                Debug.LogError("Socket (ID: " + socket.GetDatabaseID() + ") has an invalid country (" + country);

            if (!distribution.ContainsKey(country))
			{
				distribution.Add(country, new CountryEnergyAmount(socket.Capacity));
				totalCountryCapacity += socket.Capacity;
			}
			else
			{
				distribution[country].maximum += socket.Capacity;
				totalCountryCapacity += socket.Capacity;
			}
		}

		//Add sources and determine source input
		foreach (EnergyPointSubEntity source in sources)
		{
            int country = source.Entity.Country;
            if (country <= TeamManager.AM_ID)
                Debug.LogError("Source (ID: " + source.GetDatabaseID() + ") has an invalid country (" + country);

            if (!distribution.ContainsKey(country))
			{
				distribution.Add(country, new CountryEnergyAmount(0, source.Capacity));
			}
			else
				distribution[country].sourceInput += source.Capacity;
		}

		//Spread power from sources over countries as expected power
		if (oldGrid == null)
		{
			foreach (KeyValuePair<int, CountryEnergyAmount> kvp in distribution)
			{
				//Calculate a new estimation for expected value
				kvp.Value.expected = Math.Min(kvp.Value.maximum, kvp.Value.maximum / totalCountryCapacity * sourcePower);
				if (kvp.Value.maximum > maxCountryCapacity)
					maxCountryCapacity = kvp.Value.maximum;
			}
		}
		else
		{
            Dictionary<int, double> fractionReceived = new Dictionary<int, double>();
            long oldTotal = oldGrid.AvailablePower;
            long newTotal = sourcePower;
            if (!Mathf.Approximately(oldTotal, 0))
            {
                foreach (KeyValuePair<int, CountryEnergyAmount> kvp in oldGrid.energyDistribution.distribution)
                {
                    if (kvp.Value.expected < 0)
                    {
                        long sentValue = Math.Max(-distribution[kvp.Key].maximum, kvp.Value.expected);
                        newTotal += -sentValue;
                        distribution[kvp.Key].expected = sentValue;
                        fractionReceived.Add(kvp.Key, -1f);
                    }
                    else
                    {
                        fractionReceived.Add(kvp.Key, (double)kvp.Value.expected / (double)oldTotal);
                    }
                }
            }

            foreach (KeyValuePair<int, CountryEnergyAmount> kvp in distribution)
			{
                //Use the old expected value, making sure it is still valid
                if (fractionReceived.ContainsKey(kvp.Key))
                {
                    if (fractionReceived[kvp.Key] >= 0)
                    {
                        kvp.Value.expected = Math.Min(kvp.Value.maximum, (long)(fractionReceived[kvp.Key] * (double)newTotal));
                    }
                }
                else
                    kvp.Value.expected = 0;
	
				if (kvp.Value.maximum > maxCountryCapacity)
					maxCountryCapacity = kvp.Value.maximum;
			}
		}

		energyDistribution = new GridEnergyDistribution(distribution);
	}

	/// <summary>
	/// Creates a new distribution with the right countries and maximum.
	/// </summary>
	public void CalculateInitialMaximum()
	{
		if (energyDistribution != null)
			return;

        long totalCountryCapacity = 0;
		maxCountryCapacity = 0;
		Dictionary<int, CountryEnergyAmount> distribution = new Dictionary<int, CountryEnergyAmount>();

		//Add sockets and calculate socket capacities
		foreach (EnergyPointSubEntity socket in sockets)
		{
			if (!distribution.ContainsKey(socket.Entity.Country))
			{
				distribution.Add(socket.Entity.Country, new CountryEnergyAmount(socket.Capacity));
				totalCountryCapacity += socket.Capacity;
			}
			else
			{
				distribution[socket.Entity.Country].maximum += socket.Capacity;
				totalCountryCapacity += socket.Capacity;
			}
		}

		//Add sources and determine source input
		foreach (EnergyPointSubEntity source in sources)
		{
			if (!distribution.ContainsKey(source.Entity.Country))
			{
				distribution.Add(source.Entity.Country, new CountryEnergyAmount(0, source.Capacity));
			}
			else
				distribution[source.Entity.Country].sourceInput += source.Capacity;
		}

		//Determine maxCountryCapacity
		foreach (KeyValuePair<int, CountryEnergyAmount> kvp in distribution)
		{
			if (kvp.Value.maximum > maxCountryCapacity)
				maxCountryCapacity = kvp.Value.maximum;
		}

		energyDistribution = new GridEnergyDistribution(distribution);
	}

	public bool IsGreen
	{
		get
		{
			if (sockets.Count > 0)
			{
				return sockets[0].Entity.GreenEnergy;
			}
			else
			{
				return false;
			}
		}
	}

	public bool MatchesColor(GridColor color)
	{
		if (color == GridColor.Either)
			return true;
		else if (color == GridColor.Green && IsGreen)
			return true;
		else if (color == GridColor.Grey && !IsGreen)
			return true;
		return false;
	}

	public bool ShouldBeShown
	{
		get
		{
			if (sources != null && sources.Count > 0)
				return true;
			if (energyDistribution.distribution.Count > 1)
				return true;
			return false;
		}
	}

	public bool CountryHasSocketInGrid(int countryID)
	{
		foreach (EnergyPointSubEntity socket in sockets)
			if (socket.Entity.Country == countryID)
				return true;
		return false;
	}

	/// <summary>
	/// Gets the grid's planstate for a specific plan.
	/// Assumes the grid was fetched by calling GetGridsAtPlan, meaning this grid is the latest instance of its persistentID at the plan.
	/// The targetplan time will always be equal to or earlier than the grid's plan.
	/// </summary>
	public GridPlanState GetGridPlanStateAtPlan(Plan targetPlan)
	{
		// Check if the grid is relevant at all ======================================
		if (!ShouldBeShown)
			return GridPlanState.Hidden;

		// If grid is part of plan, it is sure to be relevant ========================
		if (targetPlan == plan)
		{
			//Can't be removed because they would not be part of this plan
			if (persistentID == -1 || persistentID == databaseID)
				return GridPlanState.Added;
			return GridPlanState.Changed;
		}

		//A previous grid that was removed by geom changes in this plan
		if (targetPlan.removedGrids != null && targetPlan.removedGrids.Contains(persistentID))
			return GridPlanState.Removed;

		// Check if the grid is relevant for the plan's country ======================
		bool countryInGrid = false;
		foreach (KeyValuePair<int, CountryEnergyAmount> kvp in energyDistribution.distribution)
		{
			if (kvp.Key == targetPlan.Country 
				|| TeamManager.AM_ID == targetPlan.Country
				|| TeamManager.GM_ID == targetPlan.Country)
			{
				countryInGrid = true;
				break;
			}
		}

		//Not part of targetPlan, depends on the plan's country if we are relevant
		if (countryInGrid)
			return GridPlanState.Normal;
		else
			return GridPlanState.Hidden;
	}

	public bool SocketWiseIdentical(EnergyGrid other)
	{
		if (other.sockets.Count != sockets.Count)
			return false;

		bool identical = true;
		foreach (EnergyPointSubEntity newSocket in sockets)
		{
			bool presentInOld = false;//Is newSocket present in oldGrid
			foreach (EnergyPointSubEntity oldSocket in other.sockets)
				if (newSocket.GetPersistentID() == oldSocket.GetPersistentID()
					&& newSocket.Entity.EntityTypes[0] == oldSocket.Entity.EntityTypes[0]
					&& newSocket.Entity.Country == oldSocket.Entity.Country)
				{
					presentInOld = true;
					break;
				}
			if (!presentInOld)
			{
				identical = false;
				break;
			}
		}
		return identical;
	}

	public bool SocketWiseIdentical(EnergyGrid other, out bool partiallyIdentical)
	{
		bool possiblyIdentical = other.sockets.Count == sockets.Count;
		bool identical = possiblyIdentical;//If the #sockets don't match, they can never be identaical, but still partial
		partiallyIdentical = false;
		foreach (EnergyPointSubEntity newSocket in sockets)
		{
			bool presentInOld = false;//Is newSocket present in oldGrid
			foreach (EnergyPointSubEntity oldSocket in other.sockets)
				if (newSocket.GetPersistentID() == oldSocket.GetPersistentID()
					&& newSocket.Entity.EntityTypes[0] == oldSocket.Entity.EntityTypes[0]
					&& newSocket.Entity.Country == oldSocket.Entity.Country)
				{
					presentInOld = true;
					partiallyIdentical = true;
					break;
				}
			if (!presentInOld && partiallyIdentical)
			{
				identical = false;
				break; //We already found a partial match, and this socket is not in the old grid, no use in continueing
			}
			else if (partiallyIdentical && !possiblyIdentical)
			{
				break; //These grids can't be identical and we already found a partial match, no use in continueing
			}
			else if (!presentInOld)
			{
				//The grids arent identical, but might still be partially identical
				identical = false;
				possiblyIdentical = false;
			}
		}
		return identical;
	}

	public bool SourceWiseIdentical(EnergyGrid other)
	{
		bool result = false;
		if (sources.Count == other.sources.Count)
		{
			result = true;
			Dictionary<int, SourceSummary> dict = new Dictionary<int, SourceSummary>();
			foreach (EnergyPointSubEntity newSource in sources)
			{
				//Grids with new geom will never be identical to existing ones (even if it IS the same)
				if (newSource.GetPersistentID() == -1)
					return false;
				dict.Add(newSource.GetPersistentID(), 
					new SourceSummary(newSource.Entity.Country, 
					newSource.sourcePolygon == null ? newSource.Entity.EntityTypes[0] : newSource.sourcePolygon.Entity.EntityTypes[0], 
					newSource.Capacity));
			}
			foreach (EnergyPointSubEntity oldSource in other.sources)
			{
				SourceSummary pair;
				if (!dict.TryGetValue(oldSource.GetPersistentID(), out pair)
					|| pair.type != (oldSource.sourcePolygon == null ? oldSource.Entity.EntityTypes[0] : oldSource.sourcePolygon.Entity.EntityTypes[0])
					|| pair.country != oldSource.Entity.Country
					|| pair.capacity != oldSource.Capacity)
				{ 
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public Dictionary<int, string> GetSocketNamesPerCountry()
	{
		Dictionary<int, string> result = new Dictionary<int, string>();
		foreach (EnergyPointSubEntity socket in sockets)
		{
			if (socket.Entity.name != "")
			{
				if (result.ContainsKey(socket.Entity.Country))
					result[socket.Entity.Country] += ", " + socket.Entity.name;
				else
					result.Add(socket.Entity.Country, socket.Entity.name);
			}
			else
			{
				if (result.ContainsKey(socket.Entity.Country))
					result[socket.Entity.Country] += ", Unnamed";
				else
					result.Add(socket.Entity.Country, "Unnamed");
			}
		}
		return result;
	}

    public void ShowGridOnMap()
    {
        bool green = IsGreen;
        foreach (AbstractLayer layer in LayerManager.energyLayers)
            if (layer.greenEnergy == green)
                LayerManager.ShowLayer(layer);
        
        CameraManager.Instance.ZoomToBounds(GetGridRect());
    }

	public Rect GetGridRect()
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = Vector3.one * float.MinValue;
		foreach (EnergyPointSubEntity socket in sockets)
		{
			min = Vector3.Min(min, socket.GetPosition());
			max = Vector3.Max(max, socket.GetPosition());
		}
		foreach (EnergyPointSubEntity source in sources)
		{
			min = Vector3.Min(min, source.GetPosition());
			max = Vector3.Max(max, source.GetPosition());
		}
		return new Rect(min, max - min);
	}

	public void HighlightSockets()
	{
		foreach (EnergyPointSubEntity socket in sockets)
			HighlightManager.instance.HighlightPointSubEntity(socket);
	}
}

public class CountryEnergyAmount
{
	public long expected;
	public long maximum;
	public long sourceInput;
	public CountryEnergyAmount(long maximum, long sourceInput = 0, long expected = 0)
	{
		this.maximum = maximum;
		this.sourceInput = sourceInput;
		this.expected = expected;
	}
}

public class GridEnergyDistribution
{
	public Dictionary<int, CountryEnergyAmount> distribution;
	public GridEnergyDistribution(Dictionary<int, CountryEnergyAmount> distribution)
	{
		this.distribution = distribution;
	}

	/// <summary>
	/// Makes a value copy of another GridEnergyDistribution
	/// </summary>
	public GridEnergyDistribution(GridEnergyDistribution distributionToCopy)
	{
		distribution = new Dictionary<int, CountryEnergyAmount>();
		foreach (KeyValuePair<int, CountryEnergyAmount> kvp in distributionToCopy.distribution)
			distribution.Add(kvp.Key, new CountryEnergyAmount(kvp.Value.maximum, kvp.Value.sourceInput, kvp.Value.expected));
	}
}

public class GridActualAndWasted
{
	public Dictionary<int, long> socketActual;
	public Dictionary<int, long> sourceActual;
	public long wasted;
	public long totalReceived;

	public GridActualAndWasted(int country, long socketActual)
	{
		this.socketActual = new Dictionary<int, long>() { { country, socketActual } };
		this.sourceActual = new Dictionary<int, long>();
		totalReceived = socketActual;
	}
}

[Serializable]
public class EnergyExpected
{
	public int country_id;
	public long energy_expected;

	public EnergyExpected(int country_id, long energy_expected)
	{
		this.country_id = country_id;
		this.energy_expected = energy_expected;
	}
}

