using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EnergyGrid
	{
		public enum GridColor { Green, Grey, Either };
		public enum GridPlanState { Normal, Hidden, Added, Removed, Changed };

		public List<EnergyPointSubEntity> m_sources;
		public List<EnergyPointSubEntity> m_sockets;
		public long m_sourcePower;     //Only power from sources
		public long m_sharedPower;     //Power from negative expected power
		public long AvailablePower { get { return m_sourcePower + m_sharedPower; } }

		public long m_maxCountryCapacity;
		public GridEnergyDistribution m_energyDistribution; // <countryid, (expectedEnergy, actualEnergy, maximum, sourceinput)>
		public GridActualAndWasted m_actualAndWasted;
		public bool m_distributionOnly;   //Has only the distribution been changed, or also the sources?
		public Plan m_plan;

		public string m_name = "New Grid";
		public int m_persistentID = -1;
		private int m_databaseID;
		private bool m_databaseIDSet;
		private int m_creationBatchCallID; //ID of the PostEmptyGrid call in the batch

		/// <summary>
		/// Makes a value copy of the given energy grid, with the new plan.
		/// </summary>
		public EnergyGrid(EnergyGrid a_gridToDuplicate, Plan a_plan)
		{
			m_sources = new List<EnergyPointSubEntity>(a_gridToDuplicate.m_sources);
			m_sockets = new List<EnergyPointSubEntity>(a_gridToDuplicate.m_sockets);
			m_sourcePower = a_gridToDuplicate.m_sourcePower;
			m_sharedPower = a_gridToDuplicate.m_sharedPower;
			m_maxCountryCapacity = a_gridToDuplicate.m_maxCountryCapacity;
			m_plan = a_plan;
			m_energyDistribution = new GridEnergyDistribution(a_gridToDuplicate.m_energyDistribution);
			m_persistentID = a_gridToDuplicate.m_persistentID;
			m_name = a_gridToDuplicate.m_name;
		}

		/// <summary>
		/// Goes through the energy network from the starting points connections and creates a new grid
		/// </summary>
		/// <param name="a_startSocket">A point in the grid</param>
		public EnergyGrid(EnergyPointSubEntity a_startSocket, Plan a_plan)
		{
			m_plan = a_plan;
			DetermineContents(a_startSocket);
		}

		/// <summary>
		/// Creates a new grid where the sources, sockets and max capacities are calculated from the given network. 
		/// Expected values are taken from the GridObject.
		/// </summary>
		public EnergyGrid(GridObject a_gridObject, Plan a_plan)
		{
			m_plan = a_plan;
			SetDatabaseID(a_gridObject.id);
			m_persistentID = a_gridObject.persistent;
			m_distributionOnly = a_gridObject.distribution_only;
			m_name = a_gridObject.name;
			m_energyDistribution = new GridEnergyDistribution(new Dictionary<int, CountryEnergyAmount>());

			//Find all sources
			m_sources = new List<EnergyPointSubEntity>();
			m_sourcePower = 0;
			foreach (GeomIDObject source in a_gridObject.sources)
			{
				EnergyPointSubEntity subEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(source.geometry_id, true) as EnergyPointSubEntity;
				if (subEnt != null)
				{
					m_sources.Add(subEnt);
					if (m_energyDistribution.m_distribution.ContainsKey(subEnt.Entity.Country))
						m_energyDistribution.m_distribution[subEnt.Entity.Country].m_sourceInput += subEnt.Capacity;
					else
						m_energyDistribution.m_distribution.Add(subEnt.Entity.Country, new CountryEnergyAmount(0, subEnt.Capacity));
					m_sourcePower += subEnt.Capacity;
				}
				else
					Debug.LogError(String.Format("Grid (id: {0}) is expecting source with db id: {1}, but it can't be found.", m_databaseID, source.geometry_id));
			}

			//Find all sockets
			m_sockets = new List<EnergyPointSubEntity>();
			foreach (GeomIDObject socket in a_gridObject.sockets)
			{
				EnergyPointSubEntity subEnt = PolicyLogicEnergy.Instance.GetEnergySubEntityByID(socket.geometry_id) as EnergyPointSubEntity;
				if (subEnt != null)
				{
					m_sockets.Add(subEnt);
					long newMaximum = subEnt.Capacity;
					if (m_energyDistribution.m_distribution.ContainsKey(subEnt.Entity.Country))
						newMaximum = m_energyDistribution.m_distribution[subEnt.Entity.Country].m_maximum += subEnt.Capacity; //Add maxcap to country cap and store new value
					else
						m_energyDistribution.m_distribution.Add(subEnt.Entity.Country, new CountryEnergyAmount(subEnt.Capacity));
					if (newMaximum > m_maxCountryCapacity)
						m_maxCountryCapacity = newMaximum;
				}
				else
					Debug.LogError(String.Format("Grid (id: {0}) is expecting socket with db id: {1}, but it can't be found.", m_databaseID, socket.geometry_id));
			}

			//Set expected values to those in the GridObject
			m_sharedPower = 0;
			foreach (CountryExpectedObject countryExpected in a_gridObject.energy)
			{
				CountryEnergyAmount energyAmount;
				if (!m_energyDistribution.m_distribution.TryGetValue(countryExpected.country_id, out energyAmount))
				{
					energyAmount = new CountryEnergyAmount(countryExpected.expected, countryExpected.expected, countryExpected.expected);
					m_energyDistribution.m_distribution.Add(countryExpected.country_id, energyAmount);
				}

				energyAmount.m_expected = countryExpected.expected;
				if (countryExpected.expected < 0)
				{
					m_sharedPower -= countryExpected.expected;
				}
			}
		}

		private void DetermineContents(EnergyPointSubEntity a_startSocket)
		{
			//Setup data structures
			m_sources = new List<EnergyPointSubEntity>();
			m_sockets = new List<EnergyPointSubEntity>();
			HashSet<EnergyPointSubEntity> visited = new HashSet<EnergyPointSubEntity>();
			Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

			//Add starting point
			stack.Push(a_startSocket);
			m_sockets.Add(a_startSocket);
			visited.Add(a_startSocket);

			//Find all sockets and sources in the grid
			while (stack.Count > 0)
			{
				EnergyPointSubEntity current = stack.Pop();
				foreach (Connection con in current.Connections)
				{
					EnergyPointSubEntity other = con.cable.GetConnection(!con.connectedToFirst).point;
					if (visited.Contains(other))
						continue;
					stack.Push(other);
					visited.Add(other);
					if (other.Entity.Layer.m_editingType == AbstractLayer.EditingType.Socket)
						m_sockets.Add(other);
					else if (other.Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePoint || other.Entity.Layer.m_editingType == AbstractLayer.EditingType.SourcePolygonPoint)
					{
						m_sources.Add(other);
						m_sourcePower += other.Capacity;
					}
				}
			}
		}
		
		public void SetAsCurrentGridForContent()
		{
			HashSet<int> visited = new HashSet<int>();
			Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

			//Add starting point
			stack.Push(m_sockets[0]);
			visited.Add(m_sockets[0].GetDatabaseID());

			//Find all sockets and sources in the grid
			while (stack.Count > 0)
			{
				EnergyPointSubEntity current = stack.Pop();
				current.CurrentGrid = this;

				foreach (Connection con in current.Connections)
				{
					if (visited.Contains(con.cable.GetDatabaseID()))
						continue;
					con.cable.CurrentGrid = this;
					visited.Add(con.cable.GetDatabaseID());
					EnergyPointSubEntity other = con.cable.GetConnection(!con.connectedToFirst).point;
					if (visited.Contains(other.GetDatabaseID()))
						continue;
					stack.Push(other);
					visited.Add(other.GetDatabaseID());
				}
			}
		}

		public void SetAsLastRunGridForContent(Dictionary<int, List<DirectionalConnection>> a_cableNetwork)
		{
			if (a_cableNetwork == null)
				return;

			HashSet<int> visited = new HashSet<int>();
			Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

			//Add starting point
			stack.Push(m_sockets[0]);
			visited.Add(m_sockets[0].GetDatabaseID());
			while (stack.Count > 0)
			{
				EnergyPointSubEntity current = stack.Pop();
				current.LastRunGrid = this;

				if (!a_cableNetwork.ContainsKey(current.GetDatabaseID()))
					continue;
				foreach (DirectionalConnection other in a_cableNetwork[current.GetDatabaseID()])
				{
					if (visited.Contains(other.cable.GetDatabaseID()))
						continue;
					other.cable.LastRunGrid = this;
					visited.Add(other.cable.GetDatabaseID());
					if (visited.Contains(other.point.GetDatabaseID()))
						continue;
					stack.Push(other.point);
					visited.Add(other.point.GetDatabaseID());
				}
			}
		}

		public void SetAsCurrentGridForContent(Dictionary<int, List<DirectionalConnection>> a_cableNetwork)
		{
			if (a_cableNetwork == null)
				return;

			if (m_sockets.Count == 0)
			{
				//Without any sockets can't do anything.
				Debug.LogWarning("Tried to focus a grid that has no sockets.");
				return; 
			}

			HashSet<int> visited = new HashSet<int>();
			Stack<EnergyPointSubEntity> stack = new Stack<EnergyPointSubEntity>();

			//Add starting point
			stack.Push(m_sockets[0]);
			visited.Add(m_sockets[0].GetDatabaseID());
			while (stack.Count > 0)
			{
				EnergyPointSubEntity current = stack.Pop();
				current.CurrentGrid = this;

				if (!a_cableNetwork.ContainsKey(current.GetDatabaseID()))
					continue;
				foreach (DirectionalConnection other in a_cableNetwork[current.GetDatabaseID()])
				{
					if (visited.Contains(other.cable.GetDatabaseID()))
						continue;
					other.cable.CurrentGrid = this;
					visited.Add(other.cable.GetDatabaseID());
					if (visited.Contains(other.point.GetDatabaseID()))
						continue;
					stack.Push(other.point);
					visited.Add(other.point.GetDatabaseID());
				}
			}
		}

		/// <summary>
		/// Sends the grid to the server, including the name but not the energy distribution.
		/// </summary>
		public void SubmitEmptyGridOrName(BatchRequest a_batch)
		{
			if (m_databaseIDSet)
			{
				JObject dataObject2 = new JObject();
				dataObject2.Add("id", m_databaseID);
				dataObject2.Add("name", m_name);
				a_batch.AddRequest(Server.UpdateGridName(), dataObject2, BatchRequest.BatchGroupGridAdd);
				return;
			}

			//Add new grid on server and get databaseID
			JObject dataObject = new JObject();
			dataObject.Add("name", m_name);
			dataObject.Add("plan", m_plan.GetDataBaseOrBatchIDReference());
			dataObject.Add("distribution_only", JsonConvert.SerializeObject(m_distributionOnly));
			if (m_persistentID != -1)
				dataObject.Add("persistent", m_persistentID);
			m_creationBatchCallID = a_batch.AddRequest<int>(Server.AddEnergyGrid(), dataObject, BatchRequest.BatchGroupGridAdd, AddGridCallback);
		}

		private void AddGridCallback(int a_newDatabaseID)
		{
			SetDatabaseID(a_newDatabaseID);
			if (m_persistentID == -1)
				m_persistentID = m_databaseID;
		}

		public void SetDatabaseID(int a_value)
		{
			m_databaseID = a_value;
			m_databaseIDSet = true;
			PolicyLogicEnergy.Instance.AddEnergyGrid(this);
		}

		public int GetDatabaseID()
		{
			return m_databaseID;
		}

		public bool DatabaseIDSet()
		{
			return m_databaseIDSet;
		}

		public static void SubmitGridDeletionToServer(int a_databaseID, BatchRequest a_batch)
		{
			//Delete grid by databbaseID
			JObject dataObject = new JObject();
			dataObject.Add("id", a_databaseID);
			a_batch.AddRequest(Server.DeleteGrid(), dataObject, BatchRequest.BatchGroupGridDelete);
		}

		/// <summary>
		/// Submits the expected energy values of the countries in the grid to the server.
		/// </summary>
		public void SubmitEnergyDistribution(BatchRequest a_batch)
		{
			string dbOrBatchID = m_databaseIDSet ? m_databaseID.ToString() : BatchRequest.FormatCallIDReference(m_creationBatchCallID);
			
			List<string> socketIDs = new List<string>(m_sockets.Count);
			foreach (EnergyPointSubEntity socket in m_sockets)
				socketIDs.Add(socket.GetDataBaseOrBatchIDReference());

			JObject dataObject = new JObject();
			dataObject.Add("id", dbOrBatchID);
			dataObject.Add("sockets", JToken.FromObject(socketIDs));
			a_batch.AddRequest(Server.UpdateGridSockets(), dataObject, BatchRequest.BatchGroupGridContent);

			List<string> sourceIDs = new List<string>(m_sources.Count);
			foreach (EnergyPointSubEntity source in m_sources)
				sourceIDs.Add(source.GetDataBaseOrBatchIDReference());

			dataObject = new JObject();
			dataObject.Add("id", dbOrBatchID);
			if (sourceIDs.Count > 0)
				//dataObject.Add("sources", JToken.FromObject(sourceIDs));
				dataObject.Add("sources", JToken.FromObject(sourceIDs));
			a_batch.AddRequest(Server.UpdateGridSources(), dataObject, BatchRequest.BatchGroupGridContent);

			//Update grid_energy 
			List<EnergyExpected> expected = new List<EnergyExpected>(m_energyDistribution.m_distribution.Count);
			foreach (KeyValuePair<int, CountryEnergyAmount> kvp in m_energyDistribution.m_distribution)
				expected.Add(new EnergyExpected(kvp.Key, kvp.Value.m_expected));

			dataObject = new JObject();
			dataObject.Add("id", dbOrBatchID);
			dataObject.Add("expected", JToken.FromObject(expected));
			//dataObject.Add("expected", JToken.FromObject(expected));
			a_batch.AddRequest(Server.UpdateGridEnergy(), dataObject, BatchRequest.BatchGroupGridContent);

		}

		/// <summary>
		/// Creates a new distribution with the right countries.
		/// Maximum is set correctly, expected is set to a valid estimation.
		/// </summary>
		public void CalculateInitialDistribution(EnergyGrid a_oldGrid = null)
		{
			if (m_energyDistribution != null)
				return;

			long totalCountryCapacity = 0;
			m_maxCountryCapacity = 0;
			Dictionary<int, CountryEnergyAmount> distribution = new Dictionary<int, CountryEnergyAmount>();

			//Add sockets and calculate socket capacities
			foreach (EnergyPointSubEntity socket in m_sockets)
			{
				int country = socket.Entity.Country;
				if (country <= SessionManager.AM_ID)
					Debug.LogError("Socket (ID: " + socket.GetDatabaseID() + ") has an invalid country (" + country);

				if (!distribution.ContainsKey(country))
				{
					distribution.Add(country, new CountryEnergyAmount(socket.Capacity));
					totalCountryCapacity += socket.Capacity;
				}
				else
				{
					distribution[country].m_maximum += socket.Capacity;
					totalCountryCapacity += socket.Capacity;
				}
			}

			//Add sources and determine source input
			foreach (EnergyPointSubEntity source in m_sources)
			{
				int country = source.Entity.Country;
				if (country <= SessionManager.AM_ID)
					Debug.LogError("Source (ID: " + source.GetDatabaseID() + ") has an invalid country (" + country);

				if (!distribution.ContainsKey(country))
				{
					distribution.Add(country, new CountryEnergyAmount(0, source.Capacity));
				}
				else
					distribution[country].m_sourceInput += source.Capacity;
			}

			//Spread power from sources over countries as expected power
			if (a_oldGrid == null)
			{
				foreach (KeyValuePair<int, CountryEnergyAmount> kvp in distribution)
				{
					//Calculate a new estimation for expected value
					kvp.Value.m_expected = Math.Min(kvp.Value.m_maximum, kvp.Value.m_maximum / totalCountryCapacity * m_sourcePower);
					if (kvp.Value.m_maximum > m_maxCountryCapacity)
						m_maxCountryCapacity = kvp.Value.m_maximum;
				}
			}
			else
			{
				Dictionary<int, double> fractionReceived = new Dictionary<int, double>();
				long oldTotal = a_oldGrid.AvailablePower;
				long newTotal = m_sourcePower;
				if (!Mathf.Approximately(oldTotal, 0))
				{
					foreach (KeyValuePair<int, CountryEnergyAmount> kvp in a_oldGrid.m_energyDistribution.m_distribution)
					{
						if (kvp.Value.m_expected < 0)
						{
							long sentValue = Math.Max(-distribution[kvp.Key].m_maximum, kvp.Value.m_expected);
							newTotal += -sentValue;
							m_sharedPower -= sentValue;
							distribution[kvp.Key].m_expected = sentValue;
							fractionReceived.Add(kvp.Key, -1f);
						}
						else
						{
							fractionReceived.Add(kvp.Key, (double)kvp.Value.m_expected / (double)oldTotal);
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
							kvp.Value.m_expected = Math.Min(kvp.Value.m_maximum, (long)(fractionReceived[kvp.Key] * (double)newTotal));
						}
					}
					else
						kvp.Value.m_expected = 0;
	
					if (kvp.Value.m_maximum > m_maxCountryCapacity)
						m_maxCountryCapacity = kvp.Value.m_maximum;
				}
			}

			m_energyDistribution = new GridEnergyDistribution(distribution);
		}

		public bool IsGreen
		{
			get
			{
				if (m_sockets.Count > 0)
				{
					return m_sockets[0].Entity.GreenEnergy;
				}
				return false;
			}
		}

		public bool MatchesColor(GridColor a_color)
		{
			if (a_color == GridColor.Either)
				return true;
			if (a_color == GridColor.Green && IsGreen)
				return true;
			if (a_color == GridColor.Grey && !IsGreen)
				return true;
			return false;
		}

		public bool ShouldBeShown
		{
			get
			{
				if (m_sources != null && m_sources.Count > 0)
					return true;
				if (m_energyDistribution.m_distribution.Count > 1)
					return true;
				return false;
			}
		}

		public bool CountryHasSocketInGrid(int a_countryID)
		{
			foreach (EnergyPointSubEntity socket in m_sockets)
				if (socket.Entity.Country == a_countryID)
					return true;
			return false;
		}

		/// <summary>
		/// Gets the grid's planstate for a specific plan.
		/// Assumes the grid was fetched by calling GetGridsAtPlan, meaning this grid is the latest instance of its persistentID at the plan.
		/// The targetplan time will always be equal to or earlier than the grid's plan.
		/// </summary>
		public GridPlanState GetGridPlanStateAtPlan(Plan a_targetPlan)
		{
			// If grid is part of plan, it is sure to be relevant ========================
			if (a_targetPlan == m_plan)
			{
				//Can't be removed because they would not be part of this plan
				if (m_persistentID == -1 || m_persistentID == m_databaseID)
					return GridPlanState.Added;
				return GridPlanState.Changed;
			}

			//A previous grid that was removed by geom changes in this plan
			if(a_targetPlan.TryGetPolicyData<PolicyPlanDataEnergy>(PolicyManager.ENERGY_POLICY_NAME, out var energyData))
			{
				if (energyData.removedGrids != null && energyData.removedGrids.Contains(m_persistentID))
					return GridPlanState.Removed;
			}

			// Check if the grid is relevant for the plan's country ======================
			bool countryInGrid = false;
			foreach (KeyValuePair<int, CountryEnergyAmount> kvp in m_energyDistribution.m_distribution)
			{
				if (kvp.Key != a_targetPlan.Country
					&& SessionManager.AM_ID != a_targetPlan.Country
					&& SessionManager.GM_ID != a_targetPlan.Country)
					continue;
				countryInGrid = true;
				break;
			}

			//Not part of targetPlan, depends on the plan's country if we are relevant
			if (countryInGrid)
				return GridPlanState.Normal;
			return GridPlanState.Hidden;
		}

		public bool SocketWiseIdentical(EnergyGrid a_other)
		{
			if (a_other.m_sockets.Count != m_sockets.Count)
				return false;

			bool identical = true;
			foreach (EnergyPointSubEntity newSocket in m_sockets)
			{
				bool presentInOld = false;//Is newSocket present in oldGrid
				foreach (EnergyPointSubEntity oldSocket in a_other.m_sockets)
					if (newSocket.GetPersistentID() == oldSocket.GetPersistentID()
					    && newSocket.Entity.EntityTypes[0] == oldSocket.Entity.EntityTypes[0]
					    && newSocket.Entity.Country == oldSocket.Entity.Country)
					{
						presentInOld = true;
						break;
					}
				if (presentInOld)
					continue;
				identical = false;
				break;
			}
			return identical;
		}

		public bool SocketWiseIdentical(EnergyGrid a_other, out bool a_partiallyIdentical)
		{
			bool possiblyIdentical = a_other.m_sockets.Count == m_sockets.Count;
			bool identical = possiblyIdentical;//If the #sockets don't match, they can never be identaical, but still partial
			a_partiallyIdentical = false;
			foreach (EnergyPointSubEntity newSocket in m_sockets)
			{
				bool presentInOld = false;//Is newSocket present in oldGrid
				foreach (EnergyPointSubEntity oldSocket in a_other.m_sockets)
					if (newSocket.GetPersistentID() == oldSocket.GetPersistentID()
					    && newSocket.Entity.EntityTypes[0] == oldSocket.Entity.EntityTypes[0]
					    && newSocket.Entity.Country == oldSocket.Entity.Country)
					{
						presentInOld = true;
						a_partiallyIdentical = true;
						break;
					}
				if (!presentInOld && a_partiallyIdentical)
				{
					identical = false;
					break; //We already found a partial match, and this socket is not in the old grid, no use in continueing
				}
				if (a_partiallyIdentical && !possiblyIdentical)
				{
					break; //These grids can't be identical and we already found a partial match, no use in continueing
				}
				if (!presentInOld)
				{
					//The grids arent identical, but might still be partially identical
					identical = false;
					possiblyIdentical = false;
				}
			}
			return identical;
		}

		public bool SourceWiseIdentical(EnergyGrid a_other)
		{
			bool result = false;
			if (m_sources.Count != a_other.m_sources.Count)
				return result;
			result = true;
			Dictionary<int, SourceSummary> dict = new Dictionary<int, SourceSummary>();
			List<SourceSummary> newSources = new List<SourceSummary>();
			foreach (EnergyPointSubEntity newSource in m_sources)
			{
				SourceSummary summary = new SourceSummary(newSource.Entity.Country,
					newSource.m_sourcePolygon == null ? newSource.Entity.EntityTypes[0] : newSource.m_sourcePolygon.Entity.EntityTypes[0],
					newSource.Capacity);
				if (newSource.GetPersistentID() == -1)
					newSources.Add(summary);
				else
					dict.Add(newSource.GetPersistentID(), summary);
			}
			foreach (EnergyPointSubEntity oldSource in a_other.m_sources)
			{
				SourceSummary pair;
				if(oldSource.GetPersistentID() == -1)
				{
					bool found = false;
					foreach(SourceSummary sourceSummary in newSources)
					{
						if (sourceSummary.Matches(oldSource.Entity.Country, oldSource.m_sourcePolygon == null ? oldSource.Entity.EntityTypes[0] : oldSource.m_sourcePolygon.Entity.EntityTypes[0], oldSource.Capacity))
						{
							found = true;
							break;
						}
					}
					if (found)
						continue;
					result = false;
					break;
				}
				if (!dict.TryGetValue(oldSource.GetPersistentID(), out pair)
					|| !pair.Matches(oldSource.Entity.Country, oldSource.m_sourcePolygon == null ? oldSource.Entity.EntityTypes[0] : oldSource.m_sourcePolygon.Entity.EntityTypes[0], oldSource.Capacity))
				{ 
					result = false;
					break;
				}
			}
			return result;
		}
		
		public void ShowGridOnMap()
		{
			bool green = IsGreen;
			foreach (AbstractLayer layer in PolicyLogicEnergy.Instance.m_energyLayers)
				if (layer.m_greenEnergy == green)
					LayerManager.Instance.ShowLayer(layer);
        
			CameraManager.Instance.ZoomToBounds(GetGridRect());
		}

		public Rect GetGridRect()
		{
			Vector3 min = Vector3.one * float.MaxValue;
			Vector3 max = Vector3.one * float.MinValue;
			foreach (EnergyPointSubEntity socket in m_sockets)
			{
				min = Vector3.Min(min, socket.GetPosition());
				max = Vector3.Max(max, socket.GetPosition());
			}
			foreach (EnergyPointSubEntity source in m_sources)
			{
				min = Vector3.Min(min, source.GetPosition());
				max = Vector3.Max(max, source.GetPosition());
			}
			return new Rect(min, max - min);
		}

		public void HighlightSockets()
		{
			foreach (EnergyPointSubEntity socket in m_sockets)
				HighlightManager.instance.HighlightPointSubEntity(socket);
		}
	}

	public class CountryEnergyAmount
	{
		public long m_expected;
		public long m_maximum;
		public long m_sourceInput;
		public CountryEnergyAmount(long a_maximum, long a_sourceInput = 0, long a_expected = 0)
		{
			m_maximum = a_maximum;
			m_sourceInput = a_sourceInput;
			m_expected = a_expected;
		}
	}

	public class GridEnergyDistribution
	{
		public Dictionary<int, CountryEnergyAmount> m_distribution;
		public GridEnergyDistribution(Dictionary<int, CountryEnergyAmount> a_distribution)
		{
			m_distribution = a_distribution;
		}

		/// <summary>
		/// Makes a value copy of another GridEnergyDistribution
		/// </summary>
		public GridEnergyDistribution(GridEnergyDistribution a_distributionToCopy)
		{
			m_distribution = new Dictionary<int, CountryEnergyAmount>();
			foreach (KeyValuePair<int, CountryEnergyAmount> kvp in a_distributionToCopy.m_distribution)
				m_distribution.Add(kvp.Key, new CountryEnergyAmount(kvp.Value.m_maximum, kvp.Value.m_sourceInput, kvp.Value.m_expected));
		}
	}

	public class GridActualAndWasted
	{
		public Dictionary<int, long> m_socketActual;
		public Dictionary<int, long> m_sourceActual;
		public long m_wasted;
		public long m_totalReceived;

		public GridActualAndWasted(int a_country, long a_socketActual)
		{
			m_socketActual = new Dictionary<int, long>() { { a_country, a_socketActual } };
			m_sourceActual = new Dictionary<int, long>();
			m_totalReceived = a_socketActual;
		}
	}

	[Serializable]
	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json?
	public class EnergyExpected
	{
		public int country_id;
		public long energy_expected;

		public EnergyExpected(int a_countryID, long a_energyExpected)
		{
			country_id = a_countryID;
			energy_expected = a_energyExpected;
		}
	}
}