using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MSP2050.Scripts
{
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

		private enum EBatchStatus { AwaitingBatchID, AwaitingExecutionIDs, AwaitingResults, Success, Failed }

		private EBatchStatus m_status = EBatchStatus.AwaitingBatchID;
		private int m_batchID;
		private int m_nextCallID = 1;
		private bool m_executeWhenReady;
		private bool m_async;

		private Dictionary<int, ITypedCallback> m_callbacks; //callID to callback function
		private List<QueuedBatchCall> m_callQueue; //Calls that are awaiting a batchID to be sent
		private HashSet<int> m_outstandingCallRequests; //Calls that have been sent but not confirmed

		private Action<BatchRequest> m_failureCallback;
		private Action<BatchRequest> m_successCallback;

		public BatchRequest(bool async = false)
		{
			m_async = async;
			m_callbacks = new Dictionary<int, ITypedCallback>();
			m_callQueue = new List<QueuedBatchCall>();
			m_outstandingCallRequests = new HashSet<int>();
			NetworkForm form = new NetworkForm();
			form.AddField("country_id", TeamManager.CurrentUserTeamID);
			form.AddField("user_id", TeamManager.CurrentSessionID);
			ServerCommunication.DoRequest<int>(Server.StartBatch(), form, HandleGetBatchIDSuccess, HandleGetBatchIDFailure);
		}

		private void HandleGetBatchIDSuccess(int newBatchID)
		{
			m_batchID = newBatchID;

			m_status = EBatchStatus.AwaitingExecutionIDs;
			foreach (QueuedBatchCall execution in m_callQueue)
			{
				SendRequest(execution.callID, execution.endPoint, execution.data, execution.group);
			}
			m_callQueue.Clear();
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
				m_status = EBatchStatus.Failed;
				if (m_executeWhenReady)
				{
					ExecuteBatch();
				}
			}
		}

		public int AddRequest(string endPoint, JObject data, int group)
		{
			if (m_status == EBatchStatus.Failed)
				return -1;

			int ID = m_nextCallID++;

			//data.Add("user", TeamManager.CurrentSessionID);
			if (m_status == EBatchStatus.AwaitingExecutionIDs)
			{
				SendRequest(ID, endPoint, data.ToString(), group);
			}
			else
			{
				m_callQueue.Add(new QueuedBatchCall(ID, endPoint, data.ToString(), group));
			}
			return ID;
		}

		public int AddRequest<T>(string endPoint, JObject data, int group, Action<T> callback)
		{
			if (m_status == EBatchStatus.Failed)
				return -1;

			int ID = m_nextCallID++;
			if (callback != null)
				m_callbacks.Add(ID, new TypedCallback<T>(callback));

			//data.Add("user", TeamManager.CurrentSessionID);
			if (m_status == EBatchStatus.AwaitingExecutionIDs)
			{
				SendRequest(ID, endPoint, data.ToString(), group);
			}
			else
			{
				m_callQueue.Add(new QueuedBatchCall(ID, endPoint, data.ToString(), group));
			}
			return ID;
		}

		private void SendRequest(int callID, string endPoint, string data, int group)
		{
			m_outstandingCallRequests.Add(callID);

			NetworkForm form = new NetworkForm();
			form.AddField("batch_id", m_batchID);
			form.AddField("batch_group", group);
			form.AddField("call_id", callID);
			form.AddField("endpoint", endPoint);
			form.AddField("endpoint_data", data);
			ServerCommunication.DoRequest<int>(Server.AddToBatch(), form, HandleAddRequestSuccess, HandleAddRequestFailure);
		}

		private void HandleAddRequestSuccess(int callID)
		{
			m_outstandingCallRequests.Remove(callID);

			if (m_executeWhenReady && m_outstandingCallRequests.Count == 0)
			{
				ExecuteBatch();
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
				Debug.LogError($"Adding request to batch with ID {m_batchID} failed. Error message: {message}");
				m_status = EBatchStatus.Failed;
				if (m_executeWhenReady)
				{
					ExecuteBatch();
				}
			}
		}

		public void ExecuteBatch(Action<BatchRequest> successCallback, Action<BatchRequest> failureCallback)
		{
			this.m_successCallback = successCallback;
			this.m_failureCallback = failureCallback;
			ExecuteBatch();
		}

		private void ExecuteBatch()
		{
			if (m_status == EBatchStatus.Failed)
			{
				UpdateData.WsServerCommunicationInteractor?.UnregisterBatchRequestCallbacks(m_batchID);

				//Something caused the batch to already fail, call the failure callback directly
				m_executeWhenReady = false;
				Debug.LogError($"Batch with ID {m_batchID} could not be executed because a call during its setup failed.");
				if (m_failureCallback != null)
				{
					m_failureCallback.Invoke(this);
				}
			}
			else if (m_outstandingCallRequests.Count == 0 && m_status == EBatchStatus.AwaitingExecutionIDs)
			{
				//All add requests are in, execute the batch
				m_executeWhenReady = false;
				m_status = EBatchStatus.AwaitingResults;

				NetworkForm form = new NetworkForm();
				form.AddField("batch_id", m_batchID);
				form.AddField("async", m_async.ToString());

				if (m_async)
				{
					UpdateData.WsServerCommunicationInteractor?.RegisterBatchRequestCallbacks(m_batchID, HandleBatchSuccess,
						CreateHandleBatchFailureAction(ServerCommunication.DoRequest(Server.ExecuteBatch(), form))); // todo : handle error of executebatch
				}
				else
				{
					ServerCommunication.DoRequest<BatchExecutionResult>(Server.ExecuteBatch(), form, HandleBatchSuccess,
						HandleBatchFailure);
				}
			}
			else
			{
				m_executeWhenReady = true;
			}
		}

		private Action<string> CreateHandleBatchFailureAction(ServerCommunication.ARequest request)
		{
			return delegate(string message) {
				if (request.retriesRemaining > 0)
				{
					ServerCommunication.RetryRequest(request);
				}
				else
				{
					UpdateData.WsServerCommunicationInteractor?.UnregisterBatchRequestCallbacks(m_batchID);
					Debug.LogError($"Batch with ID {m_batchID} failed. Error message: {message}");
					m_status = EBatchStatus.Failed;
					if (m_failureCallback != null)
					{
						m_failureCallback.Invoke(this);
					}
				}
			};
		}

		private void HandleBatchFailure(ServerCommunication.ARequest request, string message)
		{
			CreateHandleBatchFailureAction(request)(message);
		}

		private void HandleBatchSuccess(BatchExecutionResult batchResult)
		{
			m_status = EBatchStatus.Success;

			foreach (BatchCallResult callResult in batchResult.results)
			{
				if (m_callbacks.TryGetValue(callResult.call_id, out var callback))
				{
					callback.ProcessPayload(callResult.payload);
				}
			}
			if (m_successCallback != null)
			{
				m_successCallback.Invoke(this);
			}

			UpdateData.WsServerCommunicationInteractor?.UnregisterBatchRequestCallbacks(m_batchID);
		}

		public static string FormatCallIDReference(int batchCallID, string field = null)
		{
			if (string.IsNullOrEmpty(field))
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
				Debug.LogError("Processing batch results failed. Value does not match expected format. Message: " +
				               e.Message);
			}
		}
	}
}