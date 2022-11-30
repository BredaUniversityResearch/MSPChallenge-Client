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

		private PrefabObjectPool<GenericEntry> genericEntryPool;
		private PrefabObjectPool<GenericEntry> genericEntryIconPool;
		private PrefabObjectPool<GenericEntry> genericEntryButtonPool;
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
			genericEntryPool = new PrefabObjectPool<GenericEntry>(genericEntryPrefab, entryLocation);
			genericEntryIconPool = new PrefabObjectPool<GenericEntry>(genericEntryIconPrefab, entryLocation);
			genericEntryButtonPool = new PrefabObjectPool<GenericEntry>(genericEntryButtonPrefab, entryLocation);
		}

		public void DestroyAllContent()
		{
			genericEntryPool.ReleaseAll();
			genericEntryIconPool.ReleaseAll();
			genericEntryButtonPool.ReleaseAll();
		}

		public GenericEntry CreateEntry(string name, string content)
		{
			GenericEntry entry = genericEntryPool.Get();
			entry.SetContent(name, content);
			return entry;
		}
		public GenericEntry CreateEntry(string name, string content, Sprite icon, Color color)
		{
			GenericEntry entry = genericEntryIconPool.Get();
			entry.SetContent(name, content, icon, color);
			return entry;
		}

		public GenericEntry CreateEntry(string name, string content, UnityAction buttonCallBack)
		{
			GenericEntry entry = genericEntryButtonPool.Get();
			entry.SetContent(name, content, buttonCallBack);
			return entry;
		}
	}
}