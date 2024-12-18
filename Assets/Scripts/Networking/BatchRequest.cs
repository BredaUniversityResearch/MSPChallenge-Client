using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace MSP2050.Scripts
{
	public class BatchRequest
	{
		public const int BATCH_GROUP_PLAN_CREATE = 1;
		public const int BATCH_GROUP_ENERGY_DELETE = 2;
		public const int BATCH_GROUP_LAYER_ADD = 3;
		public const int BATCH_GROUP_LAYER_REMOVE = 3;
		public const int BATCH_GROUP_GEOMETRY_DELETE = 4;

		public const int BATCH_GROUP_GEOMETRY_ADD = 5;
		public const int BATCH_GROUP_GEOMETRY_UPDATE = 5;
		public const int BATCH_GROUP_GRID_ADD = 5;
		public const int BATCH_GROUP_GRID_DELETE = 5;
		public const int BATCH_GROUP_PLAN_CHANGE = 5;

		public const int BATCH_GROUP_ISSUES = 6;

		public const int BATCH_GROUP_PLAN_GRID_CHANGE = 7;
		public const int BATCH_GROUP_ENERGY_ERROR = 7;

		public const int BATCH_GROUP_GEOMETRY_DATA = 10;
		public const int BATCH_GROUP_CONNECTIONS = 10;
		public const int BATCH_GROUP_GRID_CONTENT = 10;

		public const int BATCH_GROUP_UNLOCK = 100;

		private enum EBatchStatus { Setup, AwaitingResults, Success, Failed }

		private EBatchStatus m_status = EBatchStatus.Setup;
		private Guid m_batchGuid;
		private int m_nextCallID = 1;
		private bool m_executeWhenReady;

		private Dictionary<int, ITypedCallback> m_callbacks; //callID to callback function
		private List<QueuedBatchCall> m_callQueue; //Calls that are awaiting a batchID to be sent

		private Action<BatchRequest> m_failureCallback;
		private Action<BatchRequest> m_successCallback;

		public BatchRequest()
		{
			m_callbacks = new Dictionary<int, ITypedCallback>();
			m_callQueue = new List<QueuedBatchCall>();
			NetworkForm form = new NetworkForm();
			m_batchGuid = Guid.NewGuid();
		}

		public int AddRequest(string a_endPoint, JObject a_data, int a_group)
		{
			// you can only add requests during Setup state
			if (m_status != EBatchStatus.Setup) return -1;
			int id = m_nextCallID++;
			m_callQueue.Add(new QueuedBatchCall(id, a_endPoint, a_data.ToString(), a_group));
			return id;
		}

		public int AddRequest<T>(string a_endPoint, JObject a_data, int a_group, Action<T> a_callback)
		{
			int id = AddRequest(a_endPoint, a_data, a_group);
			if (-1 == id) return -1;
			if (a_callback != null)
				m_callbacks.Add(id, new TypedCallback<T>(a_callback));
			return id;
		}

		public void ExecuteBatch(Action<BatchRequest> a_successCallback, Action<BatchRequest> a_failureCallback)
		{
			// do not execute it again, once it is executed 
			if (m_status != EBatchStatus.Setup) return;
			
			m_successCallback = a_successCallback;
			m_failureCallback = a_failureCallback;
			
			//All add requests are in, execute the batch
			m_status = EBatchStatus.AwaitingResults;

			NetworkForm form = new NetworkForm();
			form.AddField("country_id", SessionManager.Instance.CurrentUserTeamID);
			form.AddField("user_id", SessionManager.Instance.CurrentSessionID);
			form.AddField("batch_guid", m_batchGuid.ToString());
			form.AddField("requests", m_callQueue.Select(a_queuedBatchCall => new BatchSubRequest() {
				call_id = a_queuedBatchCall.m_callID,
				endpoint_data = a_queuedBatchCall.m_data,
				endpoint = a_queuedBatchCall.m_endPoint,
				group = a_queuedBatchCall.m_group
			}));
			UpdateManager.Instance.WsServerCommunicationInteractor?.RegisterBatchRequestCallbacks(m_batchGuid, HandleBatchSuccess,
				CreateHandleBatchFailureAction(ServerCommunication.Instance.DoRequest(Server.ExecuteBatch(), form)));
		}

		private Action<string> CreateHandleBatchFailureAction(ARequest a_request)
		{
			return delegate(string a_message) {
				Debug.LogError($"Batch with ID {m_batchGuid} reported error message: {a_message}");
				if (a_request.retriesRemaining > 0)
				{
					m_status = EBatchStatus.AwaitingResults;
					Debug.LogError($"Retrying: Batch with ID {m_batchGuid}...");
					ServerCommunication.Instance.RetryRequest(a_request);
				}
				else
				{
					UpdateManager.Instance.WsServerCommunicationInteractor?.UnregisterBatchRequestCallbacks(m_batchGuid);
					Debug.LogError($"Batch with ID {m_batchGuid} failed after {a_request.retriesRemaining} retries.");
					m_status = EBatchStatus.Failed;
					if (m_failureCallback != null)
					{
						m_failureCallback.Invoke(this);
					}
				}
			};
		}

		private void HandleBatchSuccess(BatchExecutionResult a_batchResult)
		{
			m_status = EBatchStatus.Success;

			foreach (BatchCallResult callResult in a_batchResult.results)
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

			UpdateManager.Instance.WsServerCommunicationInteractor?.UnregisterBatchRequestCallbacks(m_batchGuid);
		}

		public static string FormatCallIDReference(int a_batchCallID, string a_field = null)
		{
			if (string.IsNullOrEmpty(a_field))
				return $"!Ref:{a_batchCallID}";
			return $"!Ref:{a_batchCallID}[{a_field}]";
		}
	}

	[Serializable]
	class QueuedBatchCall
	{
		[FormerlySerializedAs("callID")]
		public int m_callID;
		[FormerlySerializedAs("group")]
		public int m_group;
		[FormerlySerializedAs("endPoint")]
		public string m_endPoint;
		[FormerlySerializedAs("data")]
		public string m_data;

		public QueuedBatchCall(int a_callID, string a_endPoint, string a_data, int a_group)
		{
			m_callID = a_callID;
			m_group = a_group;
			m_endPoint = a_endPoint;
			m_data = a_data;
		}
	}
	
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	struct BatchSubRequest
	{
		public int call_id;
		public int group;
		public string endpoint;
		public string endpoint_data;
	}	

	[Serializable]
	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class BatchExecutionResult
	{
		public int failed_call_id;
		public List<BatchCallResult> results;
	}

	[Serializable]
	[SuppressMessage("ReSharper", "InconsistentNaming")] // needs to match json
	public class BatchCallResult
	{
		public int call_id;
		public string payload;
	}

	interface ITypedCallback
	{
		void ProcessPayload(string a_payload);
	}

	class TypedCallback<T> : ITypedCallback
	{
		private Action<T> m_callback;

		public TypedCallback(Action<T> a_callback)
		{
			m_callback = a_callback;
		}

		public void ProcessPayload(string a_payload)
		{
			try
			{
				T result = JsonConvert.DeserializeObject<T>(a_payload);
				m_callback.Invoke(result);
			}
			catch (System.Exception e)
			{
				Debug.LogError("Processing batch results failed. Value does not match expected format. Message: " +
				               e.Message);
			}
		}
	}
}