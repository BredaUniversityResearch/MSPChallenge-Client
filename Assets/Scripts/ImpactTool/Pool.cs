using System;
using System.Collections.Generic;
using UnityEngine;

namespace CradleImpactTool
{
	public class Pool<T> where T : MonoBehaviour
	{
		private GameObject m_prefab;
		private Stack<T> m_pool;
		private List<T> m_elementsInUse = new List<T>();
		private Transform m_poolParent;

		public Pool(GameObject a_prefab, int a_initialCapacity = 10)
		{
			m_prefab = a_prefab;
			m_pool = new Stack<T>(a_initialCapacity);
			m_poolParent = new GameObject("PoolStorage").transform;
			m_poolParent.gameObject.SetActive(false);
		}

		public T Get(Transform a_newParent)
		{
			T newObject = null;
			if (m_pool.Count > 0)
			{
				newObject = m_pool.Pop();
				newObject.transform.SetParent(a_newParent);
			}
			else
			{
				newObject = GameObject.Instantiate(m_prefab, a_newParent).GetComponent<T>();
			}

			newObject.gameObject.SetActive(true);
			m_elementsInUse.Add(newObject);
			return newObject;
		}

		public void Release(T a_object)
		{
			m_elementsInUse.Remove(a_object);

			a_object.gameObject.SetActive(false);
			a_object.transform.SetParent(m_poolParent);
			m_pool.Push(a_object);
		}

		public void ReleaseRange(IEnumerable<T> a_objects)
		{
			foreach (T element in a_objects)
			{
				Release(element);
			}
		}

		public void ReleaseAll()
		{
			for (int i = 0; i < m_elementsInUse.Count; )
			{
				Release(m_elementsInUse[0]);
			}
		}
	}
}
