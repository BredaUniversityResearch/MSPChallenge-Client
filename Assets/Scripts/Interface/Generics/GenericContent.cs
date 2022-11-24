using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MSP2050.Scripts
{
	public class GenericContent : MonoBehaviour
	{
		public Transform entryLocation;

		[Header("Prefabs")]
		public GameObject genericEntryPrefab;
		public GameObject genericEntryIconPrefab;
		public GameObject genericEntryButtonPrefab;

		private PrefabObjectPool genericEntryPool;
		private PrefabObjectPool genericEntryIconPool;
		private PrefabObjectPool genericEntryButtonPool;
		bool initialised = false;

		private void Awake()
		{
			Initialise();
		}

		public void Initialise()
		{
			if (initialised)
				return;

			initialised = true;
			genericEntryPool = PrefabObjectPool.Create(gameObject, genericEntryPrefab, entryLocation, transform);
			genericEntryIconPool = PrefabObjectPool.Create(gameObject, genericEntryIconPrefab, entryLocation, transform);
			genericEntryButtonPool = PrefabObjectPool.Create(gameObject, genericEntryButtonPrefab, entryLocation, transform);
		}

		/// <summary>
		/// Remove from list and destroy a content window entry
		/// </summary>
		public void DestroyGenericEntry(GenericEntry entry)
		{
			PrefabObjectPoolTracker tracker = entry.gameObject.GetComponent<PrefabObjectPoolTracker>();
			if (tracker != null)
			{
				tracker.OwningPool.Release(entry.gameObject);
			}
			else
			{
				Destroy(entry.gameObject);
			}
		}

		public void DestroyAllContent()
		{
			genericEntryPool.ReleaseAll();
		}

		public GenericEntry CreateEntry(string name, string content)
		{
			GenericEntry entry = genericEntryPool.Get().GetComponent<GenericEntry>();
			entry.SetContent(name, content);
			return entry;
		}
		public GenericEntry CreateEntry(string name, string content, Sprite icon, Color color)
		{
			GenericEntry entry = genericEntryPool.Get().GetComponent<GenericEntry>();
			entry.SetContent(name, content, icon, color);
			return entry;
		}

		public GenericEntry CreateEntry(string name, string content, UnityAction buttonCallBack)
		{
			GenericEntry entry = genericEntryPool.Get().GetComponent<GenericEntry>();
			entry.SetContent(name, content, buttonCallBack);
			return entry;
		}
	}
}