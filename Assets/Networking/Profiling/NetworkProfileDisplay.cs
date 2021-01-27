using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Profiling
{
	//Class responsible for displaying all the requests. 
	class NetworkProfileDisplay: MonoBehaviour
	{
		private const float DEFAULT_TIME_WINDOW_SIZE = 60.0f; // in seconds;

		[SerializeField, Tooltip("Request entry prefab")]
		private GameObject requestProfileEntry = null;

		[SerializeField, Tooltip("Target transform for where the display entries should be placed")]
		private RectTransform requestProfileList = null;

		[SerializeField]
		private Slider timeSlider = null;

		private NetworkProfileManager owningManager = null;
		private ProfilerTimeSpan timeSpan = new ProfilerTimeSpan(0.0f, DEFAULT_TIME_WINDOW_SIZE);

		private Dictionary<RequestProfileEntry, RequestProfileDisplayEntry> displayEntries = new Dictionary<RequestProfileEntry, RequestProfileDisplayEntry>();

		private void Awake()
		{
			timeSlider.value = timeSlider.maxValue;
			timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		public void SetManager(NetworkProfileManager manager)
		{
			owningManager = manager;
		}

		public void UpdateProfilingEntryInUI(RequestProfileEntry entry)
		{
			if (timeSpan.Contains(entry.RequestStartTime))
			{
				CreateOrUpdateEntry(entry);
			}

			if (timeSlider.maxValue < entry.RequestStartTime)
			{
				bool updateValue = (timeSlider.value == timeSlider.maxValue);

				timeSlider.maxValue = entry.RequestStartTime; //Update max time.
				if (updateValue)
				{
					timeSlider.value = entry.RequestStartTime;
				}
			}
		}

		private void OnSliderValueChanged(float newValue)
		{
			timeSpan.Set(newValue - (DEFAULT_TIME_WINDOW_SIZE * 0.5f), newValue + (DEFAULT_TIME_WINDOW_SIZE * 0.5f));
			OnTimeSpanUpdated();
		}

		private void OnTimeSpanUpdated()
		{
			List<RequestProfileEntry> entriesToRemove = new List<RequestProfileEntry>(displayEntries.Count);
			foreach (KeyValuePair<RequestProfileEntry, RequestProfileDisplayEntry> kvp in displayEntries)
			{
				if (!timeSpan.Contains(kvp.Key.RequestStartTime))
				{
					entriesToRemove.Add(kvp.Key);
				}
			}

			foreach (RequestProfileEntry entry in entriesToRemove)
			{
				DestroyEntry(entry);
			}

			if (owningManager != null)
			{
				owningManager.OnEachEntryWithinTimeSpan(timeSpan, CreateOrUpdateEntry);
			}

			SortEntries();
		}

		private void SortEntries()
		{
			List<KeyValuePair<RequestProfileEntry, RequestProfileDisplayEntry>> sortedList = new List<KeyValuePair<RequestProfileEntry, RequestProfileDisplayEntry>>(displayEntries);
			sortedList.Sort((lhs, rhs) => lhs.Key.RequestStartTime.CompareTo(rhs.Key.RequestStartTime));

			for (int i = 0; i < sortedList.Count; ++i)
			{
				sortedList[i].Value.gameObject.transform.SetSiblingIndex(i);
			}
		}

		private void CreateOrUpdateEntry(RequestProfileEntry entry)
		{
			RequestProfileDisplayEntry uiEntry;
			if (!displayEntries.TryGetValue(entry, out uiEntry))
			{
				GameObject newInstance = Instantiate(requestProfileEntry, requestProfileList);
				uiEntry = newInstance.GetComponent<RequestProfileDisplayEntry>();
				uiEntry.UpdateRequestInfo(entry);
				displayEntries.Add(entry, uiEntry);
			}

			uiEntry.UpdateTimings(entry);
		}

		private void DestroyEntry(RequestProfileEntry entry)
		{
			RequestProfileDisplayEntry displayEntry;
			if (displayEntries.TryGetValue(entry, out displayEntry))
			{
				Destroy(displayEntry.gameObject);
				displayEntries.Remove(entry);
			}
		}
	}
}
