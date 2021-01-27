#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using System.Linq;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EnergyVerifier : OdinEditorWindow
{
	[MenuItem("MSP 2050/Energy verifier")]
	private static void OpenWindow()
	{
		var window = GetWindow<EnergyVerifier>();

		// Nifty little trick to quickly position the window in the middle of the editor.
		window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 300);
	}

	[InfoBox("To verify the energy system the game must be running and connected to the server. The local client will break from verification, so you will need to reconnect afterwards.")]
	[Button("Begin verification")]
	public void VerifyEnergy()
	{
		GameObject obj = new GameObject("EnergyVerifierObj");
		EnergyVerifierObj verifier = obj.AddComponent<EnergyVerifierObj>();
		verifier.VerifyEnergy();
	}
}

public class EnergyVerifierObj : MonoBehaviour
{
	int awaitingGridReponses = 0;
	int errors = 0;

	public void VerifyEnergy()
	{
		if (LayerManager.energySubEntities == null || LayerManager.energySubEntities.Count == 0)
		{
			Debug.Log("No energy objects found");
			Destroy(gameObject);
			return;
		}
		awaitingGridReponses = 0;
		errors = 0;

		//Stops the game from processing updates permanently
		UpdateData.stopProcessingUpdates = true;

		//Setup world state and variables for verification
		if (Main.InEditMode)
			PlanDetails.LayersTab.ForceCancelChanges();
		PlanManager.HideCurrentPlan(true);

		HashSet<int> socketIDs = new HashSet<int>(); //DB ids of all sockets active at the current time

		foreach (AbstractLayer layer in LayerManager.energyLayers)
		{
			LayerManager.ShowLayer(layer);
			layer.ResetEnergyConnections();
			if (layer.editingType == AbstractLayer.EditingType.Socket)
			{
				foreach (SubEntity socket in layer.GetActiveSubEntities())
					socketIDs.Add(socket.GetDatabaseID());
			}
		}

		//Have the cable layer activate all connections that are present in the current state, required for later grid checks
		if (LayerManager.energyCableLayerGreen != null)
			LayerManager.energyCableLayerGreen.ActivateCableLayerConnections();
		if (LayerManager.energyCableLayerGrey != null)
			LayerManager.energyCableLayerGrey.ActivateCableLayerConnections();

		List<EnergyGrid> currentGrids = PlanManager.GetEnergyGridsAtTime(GameState.GetCurrentMonth(), EnergyGrid.GridColor.Either);

		//CABLE CONNECTIONS =================================================================================================
		Debug.Log("Beginning cable connection check.");
		//Check if all cables have 2 connections
		if (LayerManager.energyCableLayerGreen != null)
		{
			errors += CheckCables(LayerManager.energyCableLayerGreen);
		}
		if (LayerManager.energyCableLayerGrey != null)
		{
			errors += CheckCables(LayerManager.energyCableLayerGrey);
		}
		Debug.Log($"Cable connection check complete, {errors} errors found.");
		errors = 0;


		//SOCKETS  ===========================================================================================================
		Debug.Log("Beginning socket check.");
		//Check if all sockets are part of a grid and all expected sockets in grids can be found
		foreach (EnergyGrid grid in currentGrids)
		{
			foreach(EnergyPointSubEntity socket in grid.sockets)
			{
				if (!socketIDs.Remove(socket.GetDatabaseID()))
				{
					Debug.LogError($"Active grid had socket with id: {socket.GetDatabaseID()}, but this socket was not in the ActiveSubentities at the current time.");
					errors++;
				}
			}
		}
		if(socketIDs.Count > 0)
		{
			foreach(int id in socketIDs)
				Debug.LogError($"Socket with id: {id} was in ActiveSubentities but not part of any grid.");
			errors += socketIDs.Count;
		}
		Debug.Log($"Socket check complete, {errors} errors found.");
		errors = 0;

		//GRIDS  =====================================================================================================================
		StartCoroutine("CheckGrids", currentGrids);		
	}

	int CheckCables(LineStringLayer layer)
	{
		int errors = 0;
		foreach (var subEnt in layer.GetAllSubEntities())
		{
			EnergyLineStringSubEntity cable = (EnergyLineStringSubEntity)subEnt;
			if (cable.connections == null || cable.connections.Count == 0)
			{
				Debug.LogError($"Cable without connections found: {cable.GetDatabaseID()}");
				errors++;
			}
			if (cable.connections.Count < 2)
			{
				Debug.LogError($"Cable with missing connection found: {cable.GetDatabaseID()}. Only connected to: {cable.connections[0].point.GetDatabaseID()}");
				errors++;
			}
		}
		return errors;
	}

	private IEnumerator CheckGrids(List<EnergyGrid> currentGrids)
	{
		Debug.Log("Beginning grid content check.");
		foreach (EnergyGrid grid in currentGrids)
		{
			int gridId = grid.GetDatabaseID();

			//Compare grid to the grid we get when retracing from a single source
			EnergyGrid retracedGrid = new EnergyGrid(grid.sockets[0], null);
			List<int> originalSockets = new List<int>(grid.sockets.Count);
			List<int> originalSources = new List<int>(grid.sources.Count);
			foreach (EnergyPointSubEntity socket in grid.sockets)
				originalSockets.Add(socket.GetDatabaseID());
			//string originalSourceIDs = string.Join(", ", grid.sources.Select<EnergyPointSubEntity, string>(source => { return source.GetDatabaseID().ToString(); }));
			if (!retracedGrid.SourceWiseIdentical(grid))
			{
				string retracedIDs = string.Join(", ", retracedGrid.sources.Select<EnergyPointSubEntity, string>(source => { return source.GetDatabaseID().ToString(); }));
				Debug.LogError($"Sources in grid with id: {gridId} were different when it was retraced in the current state.\nOriginal sources: {JsonConvert.SerializeObject(originalSources)}.\nRetraced sources: {retracedIDs}.");
				errors++;
			}
			//string originalSocketIDs = string.Join(", ", grid.sockets.Select<EnergyPointSubEntity, string>(socket => { return socket.GetDatabaseID().ToString(); }));
			foreach (EnergyPointSubEntity source in grid.sources)
				originalSources.Add(source.GetDatabaseID());
			if (!retracedGrid.SocketWiseIdentical(grid))
			{
				string retracedIDs = string.Join(", ", retracedGrid.sockets.Select<EnergyPointSubEntity, string>(socket => { return socket.GetDatabaseID().ToString(); }));
				Debug.LogError($"Sockets in grid with id: {gridId} were different when it was retraced in the current state.\nOriginal sockets: {JsonConvert.SerializeObject(originalSockets)}.\nRetraced sockets: {retracedIDs}.");
				errors++;
			}

			//Compare grid with version stored on the server
			awaitingGridReponses++;
			NetworkForm form = new NetworkForm();
			form.AddField("grid_id", gridId);
            if(originalSources.Count > 0)
			    form.AddField("source_ids", JToken.FromObject(originalSources));
            if(originalSockets.Count > 0)
			    form.AddField("socket_ids", JToken.FromObject(originalSockets));
			ServerCommunication.DoRequest<GridVerificationResult>(Server.VerifyEnergyGrid(), form, result => GridVerificationResultHandler(result, gridId));
		}
		yield return awaitingGridReponses == 0;
		Debug.Log($"Grid content check complete, {errors} errors found.");
		errors = 0;

		//Continue to next check when done
		CheckCapacity();
	}

	void GridVerificationResultHandler(GridVerificationResult result, int gridId)
	{
		awaitingGridReponses--;
		if (result != null && result.HasErrors())
			Debug.LogError($"Grid verification of grid {gridId} failed.{result.GetErrorText()}");
	}

	void CheckCapacity()
	{
		Debug.Log("Beginning capacity check.");
		//Check of all energy subentities have their capacity stored on the server
		List<int> ids = new List<int>();
		foreach (AbstractLayer layer in LayerManager.energyLayers)
		{
			foreach (SubEntity sub in layer.GetActiveSubEntities())
			{
				int id = sub.GetDatabaseID();
				if (LayerManager.GetEnergySubEntityByID(id) == null)
				{
					Debug.LogError($"Energy subentity with id: {id} was not found in LayerManager's energy subentities.");
					errors++;
				}
				ids.Add(id);
			}
		}
		NetworkForm form = new NetworkForm();
		form.AddField("ids", JToken.FromObject(ids));
		ServerCommunication.DoRequest<string>(Server.VerifyEnergyCapacity(), form, CapacityCheckResultHandler);
	}

	void CapacityCheckResultHandler(string result)
	{

        if (!string.IsNullOrEmpty(result))
        {
            string[] missingCapacityIds = result.Split(',');
            foreach (string id in missingCapacityIds)
            {
                Debug.LogError($"Energy subentity with id: {id} does not have a capacity on the server.");
                errors++;
            }
        }

		//Ends the verification process
		Debug.Log($"Capacity check complete, {errors} errors found.");
		Debug.Log($"Energy verification check complete.");
	}
}

public class GridVerificationResult
{
	public List<int> clientMissingSourceIDs;
	public List<int> clientExtraSourceIDs;
	public List<int> clientMissingSocketIDs;
	public List<int> clientExtraSocketIDs;

	public bool HasErrors()
	{
		return (clientMissingSourceIDs != null && clientMissingSourceIDs.Count > 0) ||
			(clientExtraSourceIDs != null && clientExtraSourceIDs.Count > 0) ||
			(clientMissingSocketIDs != null && clientMissingSocketIDs.Count > 0) ||
			(clientExtraSocketIDs != null && clientExtraSocketIDs.Count > 0);
	}

	public string GetErrorText()
	{
		StringBuilder sb = new StringBuilder(50);
		if (clientMissingSourceIDs != null && clientMissingSourceIDs.Count > 0)
			sb.Append("\nClient missing source IDs: " + string.Join(", ", clientMissingSourceIDs));
		if (clientExtraSourceIDs != null && clientExtraSourceIDs.Count > 0)
			sb.Append("\nClient extra source IDs: " + string.Join(", ", clientExtraSourceIDs));
		if (clientMissingSocketIDs != null && clientMissingSocketIDs.Count > 0)
			sb.Append("\nClient missing socket IDs: " + string.Join(", ", clientMissingSocketIDs));
		if (clientExtraSocketIDs != null && clientExtraSocketIDs.Count > 0)
			sb.Append("\nClient extra socket IDs: " + string.Join(", ", clientExtraSocketIDs));
		return sb.ToString();
	}
}
#endif

