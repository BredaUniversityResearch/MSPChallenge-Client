using System;
using UnityEngine;
using System.Collections.Generic;
using Assets.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;


public class BatchRequest
{
	public const int BATCH_GROUP_ENERGY_DELETE = 2;
	public const int BATCH_GROUP_GEOMETRY_DELETE = 3;

	public const int BATCH_GROUP_GEOMETRY_ADD = 5;
	public const int BATCH_GROUP_GEOMETRY_UPDATE = 5;
	public const int BATCH_GROUP_GRID_ADD = 5;
	public const int BATCH_GROUP_GRID_DELETE = 5;
	public const int BATCH_GROUP_PLAN_CHANGE = 5;
	public const int BATCH_GROUP_ISSUES = 5;

	public const int BATCH_GROUP_PLAN_GRID_CHANGE = 7;
	public const int BATCH_GROUP_ENERGY_ERROR = 7;

	public const int BATCH_GROUP_GEOMETRY_DATA = 10;
	public const int BATCH_GROUP_CONNECTIONS = 10;
	public const int BATCH_GROUP_GRID_CONTENT = 10;

	public const int BATCH_GROUP_UNLOCK = 100;

	public enum EBatchStatus { AwaitingBatchID, AwaitingExecutionIDs, AwaitingResults, Success, Failed }

	EBatchStatus status = EBatchStatus.AwaitingBatchID;
	int batchID;
	int nextCallID = 1;
	bool executeWhenReady;

	Dictionary<int, ITypedCallback> callbacks; //callID to callback function
	List<QueuedBatchCall> callQueue; //Calls that are awaiting a batchID to be sent
	HashSet<int> outstandingCallRequests; //Calls that have been sent but not confirmed

	Action<BatchRequest> failureCallback;
	Action<BatchRequest> successCallback;

	public BatchRequest()
	{
		callbacks = new Dictionary<int, ITypedCallback>();
		callQueue = new List<QueuedBatchCall>();
		outstandingCallRequests = new HashSet<int>();
		NetworkForm form = new NetworkForm();
		ServerCommunication.DoRequest<int>(Server.StartBatch(), form, HandleGetBatchIDSuccess, HandleGetBatchIDFailure);
	}

	private void HandleGetBatchIDSuccess(int newBatchID)
	{
		batchID = newBatchID;

		status = EBatchStatus.AwaitingExecutionIDs;
		foreach (QueuedBatchCall execution in callQueue)
		{
			SendRequest(execution.callID, execution.endPoint, execution.data, execution.group);
		}
		callQueue.Clear();
	}

	private void HandleGetBatchIDFailure(ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			Debug.LogError($"Getting a batch ID failed. Error message: {message}");
			status = EBatchStatus.Failed;
			if (executeWhenReady)
				ExecuteBatch();
		}
	}

	public int AddRequest(string endPoint, JObject data, int group)	
	{
		if (status == EBatchStatus.Failed)
			return -1;

		int ID = nextCallID++;

		//data.Add("user", TeamManager.CurrentSessionID);
		if (status == EBatchStatus.AwaitingExecutionIDs)
		{
			SendRequest(ID, endPoint, data.ToString(), group);
		}
		else
		{
			callQueue.Add(new QueuedBatchCall(ID, endPoint, data.ToString(), group));
		}
		return ID;
	}

	public int AddRequest<T>(string endPoint, JObject data, int group, Action<T> callback)
	{
		if (status == EBatchStatus.Failed)
			return -1;

		int ID = nextCallID++;
		if (callback != null)
			callbacks.Add(ID, new TypedCallback<T>(callback));

		//data.Add("user", TeamManager.CurrentSessionID);
		if (status == EBatchStatus.AwaitingExecutionIDs)
		{
			SendRequest(ID, endPoint, data.ToString(), group);
		}
		else
		{
			callQueue.Add(new QueuedBatchCall(ID, endPoint, data.ToString(), group));
		}
		return ID;
	}

	private void SendRequest(int callID, string endPoint, string data, int group)
	{
		outstandingCallRequests.Add(callID);

		NetworkForm form = new NetworkForm();
		form.AddField("batch_id", batchID);
		form.AddField("batch_group", group);
		form.AddField("call_id", callID);
		form.AddField("endpoint", endPoint);
		form.AddField("endpoint_data", data);
		ServerCommunication.DoRequest<int>(Server.AddToBatch(), form, HandleAddRequestSuccess, HandleAddRequestFailure);
	}

	private void HandleAddRequestSuccess(int callID)
	{
		outstandingCallRequests.Remove(callID);

		if (executeWhenReady && outstandingCallRequests.Count == 0)
			ExecuteBatch();
	}

	private void HandleAddRequestFailure(ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			Debug.LogError($"Adding request to batch with ID {batchID} failed. Error message: {message}");
			status = EBatchStatus.Failed;
			if (executeWhenReady)
				ExecuteBatch();
		}
	}

	public void ExecuteBatch(Action<BatchRequest> successCallback, Action<BatchRequest> failureCallback)
	{
		this.successCallback = successCallback;
		this.failureCallback = failureCallback;
		ExecuteBatch();
	}

	private void ExecuteBatch()
	{
		Debug.Log($"Batch execution attempted. Current status: {status}, oustanding requests: {outstandingCallRequests.Count}");
		if(status == EBatchStatus.Failed)
		{
			//Something caused the batch to already fail, call the failure callback directly
			executeWhenReady = false;
			Debug.LogError($"Batch with ID {batchID} could not be executed because a call during its setup failed.");
			if (failureCallback != null)
				failureCallback.Invoke(this);

		}
		else if (outstandingCallRequests.Count == 0 && status == EBatchStatus.AwaitingExecutionIDs)
		{
			//All add requests are in, execute the batch
			executeWhenReady = false;
			status = EBatchStatus.AwaitingResults;

			NetworkForm form = new NetworkForm();
			form.AddField("batch_id", batchID);
			ServerCommunication.DoRequest<BatchExecutionResult>(Server.ExecuteBatch(), form, HandleBatchSuccess, HandleBatchFailure);
		}
		else
			executeWhenReady = true;		
	}

	private void HandleBatchFailure(ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			Debug.LogError($"Batch with ID {batchID} failed. Error message: {message}");
			status = EBatchStatus.Failed;
			if (failureCallback != null)
				failureCallback.Invoke(this);
		}
	}

	private void HandleBatchSuccess(BatchExecutionResult batchResult)
	{
		status = EBatchStatus.Success;

		foreach (BatchCallResult callResult in batchResult.results)
		{
			if (callbacks.TryGetValue(callResult.call_id, out var callback))
			{
				callback.ProcessPayload(callResult.payload);
			}
		}
		if (successCallback != null)
			successCallback.Invoke(this);
	}

	public static string FormatCallIDReference(int batchCallID, string field = null)
	{
		if(string.IsNullOrEmpty(field))
			return $"!Ref:{batchCallID}";
		else
			return $"!Ref:{batchCallID}[{field}]";
	}
}

[Serializable]
class QueuedBatchCall
{
	public int callID;
	public int group;
	public string endPoint;
	public string data;

	public QueuedBatchCall(int callID, string endPoint, string data, int group)
	{
		this.callID = callID;
		this.group = group;
		this.endPoint = endPoint;
		this.data = data;
	}
}

[Serializable]
class BatchExecutionResult
{
	public int failed_call_id;
	public List<BatchCallResult> results;
}

[Serializable]
class BatchCallResult
{
	public int call_id;
	public string payload;
}

interface ITypedCallback
{
	void ProcessPayload(string payload);
}

class TypedCallback<T> : ITypedCallback
{
	Action<T> callback;

	public TypedCallback(Action<T> callback)
	{
		this.callback = callback;
	}

	public void ProcessPayload(string payload)
	{
		try
		{
			T result = JsonConvert.DeserializeObject<T>(payload);
			callback.Invoke(result);
		}
		catch (System.Exception e)
		{
			Debug.LogError("Processing batch results failed. Value does not match expected format. Message: " + e.Message);
		}
	}
}

