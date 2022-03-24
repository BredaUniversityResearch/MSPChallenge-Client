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
	private int m_BatchID;
	private int m_NextCallID = 1;
	private bool m_ExecuteWhenReady;
	private bool m_Async;

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
		form.AddField("country_id", TeamManager.CurrentUserTeamID);
		form.AddField("user_id", TeamManager.CurrentSessionID);
		ServerCommunication.DoRequest<int>(Server.StartBatch(), form, HandleGetBatchIDSuccess, HandleGetBatchIDFailure);
	}

	private void HandleGetBatchIDSuccess(int newBatchID)
	{
		m_BatchID = newBatchID;

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
			if (m_ExecuteWhenReady)
			{
				ExecuteBatch(m_Async);
			}
		}
	}

	public int AddRequest(string endPoint, JObject data, int group)
	{
		if (status == EBatchStatus.Failed)
			return -1;

		int ID = m_NextCallID++;

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

		int ID = m_NextCallID++;
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
		form.AddField("batch_id", m_BatchID);
		form.AddField("batch_group", group);
		form.AddField("call_id", callID);
		form.AddField("endpoint", endPoint);
		form.AddField("endpoint_data", data);
		ServerCommunication.DoRequest<int>(Server.AddToBatch(), form, HandleAddRequestSuccess, HandleAddRequestFailure);
	}

	private void HandleAddRequestSuccess(int callID)
	{
		outstandingCallRequests.Remove(callID);

		if (m_ExecuteWhenReady && outstandingCallRequests.Count == 0)
		{
			ExecuteBatch(m_Async);
		}
	}

	private void HandleAddRequestFailure(ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			Debug.LogError($"Adding request to batch with ID {m_BatchID} failed. Error message: {message}");
			status = EBatchStatus.Failed;
			if (m_ExecuteWhenReady)
			{
				ExecuteBatch(m_Async);
			}
		}
	}

	public void ExecuteBatch(Action<BatchRequest> successCallback, Action<BatchRequest> failureCallback)
	{
		this.successCallback = successCallback;
		this.failureCallback = failureCallback;
		ExecuteBatch();
	}

	public void ExecuteBatchAsync(Action<BatchRequest> successCallback, Action<BatchRequest> failureCallback)
	{
		this.successCallback = successCallback;
		this.failureCallback = failureCallback;
		ExecuteBatch(true);
	}

	private void ExecuteBatch(bool async = false)
	{
		m_Async = async;
		if(status == EBatchStatus.Failed)
		{
			UpdateData.WsServerCommunication.UnregisterBatchRequestCallbacks(m_BatchID);

			//Something caused the batch to already fail, call the failure callback directly
			m_ExecuteWhenReady = false;
			Debug.LogError($"Batch with ID {m_BatchID} could not be executed because a call during its setup failed.");
			if (failureCallback != null)
			{
				failureCallback.Invoke(this);
			}
		}
		else if (outstandingCallRequests.Count == 0 && status == EBatchStatus.AwaitingExecutionIDs)
		{
			//All add requests are in, execute the batch
			m_ExecuteWhenReady = false;
			status = EBatchStatus.AwaitingResults;

			NetworkForm form = new NetworkForm();
			form.AddField("batch_id", m_BatchID);
			form.AddField("async", m_Async.ToString());

			if (m_Async)
			{
				UpdateData.WsServerCommunication.RegisterBatchRequestCallbacks(m_BatchID, HandleBatchSuccess);
				ServerCommunication.DoRequest(Server.ExecuteBatch(), form);
			}
			else
			{
				ServerCommunication.DoRequest<BatchExecutionResult>(Server.ExecuteBatch(), form, HandleBatchSuccess, HandleBatchFailure);
			}

			m_Async = false; // reset to default, no async.
		}
		else
		{
			m_ExecuteWhenReady = true;
		}
	}

	private void HandleBatchFailure(ServerCommunication.ARequest request, string message)
	{
		if (request.retriesRemaining > 0)
		{
			ServerCommunication.RetryRequest(request);
		}
		else
		{
			UpdateData.WsServerCommunication.UnregisterBatchRequestCallbacks(m_BatchID);
			Debug.LogError($"Batch with ID {m_BatchID} failed. Error message: {message}");
			status = EBatchStatus.Failed;
			if (failureCallback != null)
			{
				failureCallback.Invoke(this);
			}
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
		{
			successCallback.Invoke(this);
		}

		UpdateData.WsServerCommunication.UnregisterBatchRequestCallbacks(m_BatchID);
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
public class BatchExecutionResult
{
	public int failed_call_id;
	public List<BatchCallResult> results;
}

[Serializable]
public class BatchCallResult
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

