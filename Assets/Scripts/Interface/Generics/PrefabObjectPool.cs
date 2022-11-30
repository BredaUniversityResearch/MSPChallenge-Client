using System.Collections.Generic;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class PrefabObjectPool<T> where T : MonoBehaviour
	{
		private GameObject m_entryPrefab;
		private Transform m_prefabParent;

		private List<T> m_entries = new List<T>();
		int m_nextIndex;

		public PrefabObjectPool(GameObject a_prefab, Transform a_prefabParent)
		{
			this.m_entryPrefab = a_prefab;
			this.m_prefabParent = a_prefabParent;
		}

		public T Get()
		{
			if (m_nextIndex < m_entries.Count)
			{
				m_nextIndex++;
				return m_entries[m_nextIndex - 1];
			}

			T result = GameObject.Instantiate(m_entryPrefab, m_prefabParent).GetComponent<T>();
			m_entries.Add(result);
			m_nextIndex++;
			return result;
		}

		public void ReleaseAll()
        {
			for (int i = 0; i < m_nextIndex; i++)
				m_entries[i].gameObject.SetActive(false);
			m_nextIndex = 0;
		}
	}
}