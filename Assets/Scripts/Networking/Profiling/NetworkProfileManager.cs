using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking.Profiling
{
	class NetworkProfileManager: MonoBehaviour
	{
		[SerializeField, Tooltip("Parent transform where we will instantiate the profiling window")]
		private RectTransform windowParent = null;

		[SerializeField, Tooltip("Prefab for the profiling window.")]
		private GameObject windowPrefab = null;

		private Dictionary<ServerCommunication.ARequest, RequestProfileEntry> requestEntries = new Dictionary<ServerCommunication.ARequest, RequestProfileEntry>(1024);
		private NetworkProfileDisplay windowInstance = null;

		private void OnEnable()
		{
			if (Main.IsDeveloper)
			{
				ServerCommunication.OnRequestQueued += OnRequestQueued;
				ServerCommunication.OnRequestStarted += OnRequestStarted;
				ServerCommunication.OnRequestResponseReceived += OnRequestResponseReceived;
				ServerCommunication.OnRequestResponseProcessed += OnRequestResponseProcessed;

				CreateWindow();
			}
		}

		private void OnDisable()
		{
			if (Main.IsDeveloper)
			{
				ServerCommunication.OnRequestQueued -= OnRequestQueued;
				ServerCommunication.OnRequestStarted -= OnRequestStarted;
				ServerCommunication.OnRequestResponseReceived -= OnRequestResponseReceived;
				ServerCommunication.OnRequestResponseProcessed -= OnRequestResponseProcessed;

				DestroyWindow();
			}
		}

		private void Update()
		{
			if (Main.IsDeveloper)
			{
				if (Input.GetKeyDown(KeyCode.F11))
				{
					windowInstance.gameObject.SetActive(!windowInstance.gameObject.activeSelf);
				}
			}
		}

		private void OnRequestQueued(ServerCommunication.ARequest targetRequest)
		{
			requestEntries.Add(targetRequest, new RequestProfileEntry(targetRequest));
		}

		private void OnRequestStarted(ServerCommunication.ARequest targetRequest)
		{
			RequestProfileEntry entry;
			if (requestEntries.TryGetValue(targetRequest, out entry))
			{
				entry.OnRemovedFromQueue();
				OnProfileEntryUpdated(entry);
			}
			else
			{
				Debug.LogError("Request started but hasn't been added to queue? Profiler will be inaccurate.");
			}
		}

		private void OnRequestResponseReceived(ServerCommunication.ARequest targetRequest)
		{
			RequestProfileEntry entry;
			if (requestEntries.TryGetValue(targetRequest, out entry))
			{
				entry.OnResponseReceived();
				OnProfileEntryUpdated(entry);
			}
			else
			{
				Debug.LogError("Response received but request hasn't been added to queue? Profiler will be inaccurate.");
			}
		}

		private void OnRequestResponseProcessed(ServerCommunication.ARequest targetRequest)
		{
			RequestProfileEntry entry;
			if (requestEntries.TryGetValue(targetRequest, out entry))
			{
				entry.OnResponseProcessed();
				OnProfileEntryUpdated(entry);
			}
			else
			{
				Debug.LogError("Response processed but request hasn't been added to queue? Profiler will be inaccurate.");
			}
		}

		private void OnProfileEntryUpdated(RequestProfileEntry entry)
		{
			if (windowInstance != null)
			{
				windowInstance.UpdateProfilingEntryInUI(entry);
			}
		}

		private void CreateWindow()
		{
			GameObject window = Instantiate(windowPrefab, windowParent);
			windowInstance = window.GetComponent<NetworkProfileDisplay>();
			if (windowInstance != null)
			{
				windowInstance.SetManager(this);
				windowInstance.gameObject.SetActive(false);
			}
			else
			{
				Debug.LogError("Profiling window does not have the required component attached.");
			}
		}

		private void DestroyWindow()
		{
			if (windowInstance != null)
			{
				Destroy(windowInstance.gameObject);
			}
		}

		public void OnEachEntryWithinTimeSpan(ProfilerTimeSpan timeSpan, Action<RequestProfileEntry> callbackAction)
		{
			foreach (RequestProfileEntry entry in requestEntries.Values)
			{
				if (timeSpan.Contains(entry.RequestStartTime))
				{
					callbackAction.Invoke(entry);
				}
			}
		}
	}
}
