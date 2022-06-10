using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using MSP2050.Scripts;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace UnityEngine.UI
{
	public class RoundingManager : MonoBehaviour
	{
		private static RoundingManager m_instance;
		public static RoundingManager Instance
		{
			get
			{
				if (m_instance == null)
				{
					m_instance = new GameObject("RoundingManager").AddComponent<RoundingManager>();
					m_instance.m_roundingAssetDatabase = Resources.Load<RoundingAssetDatabase>("RoundingAssetDatabase");
				}

				return m_instance;
			}
		}

		private RoundingAssetDatabase m_roundingAssetDatabase;
		public static RoundingAssetDatabase RoundingAssetDatabase => Instance.m_roundingAssetDatabase;

		private int m_uiScale = 3;
		public static int UIScale => Instance.m_uiScale;

		private List<IUIScaleChangeReceiver> m_uiScaleCallbackList = new List<IUIScaleChangeReceiver>();

		void Start()
		{
			if (m_instance != null && m_instance != this)
			{
				Destroy(gameObject);
			}
			else
			{
				DontDestroyOnLoad(gameObject);
				m_instance = this;
				if(m_roundingAssetDatabase == null)
					m_roundingAssetDatabase = Resources.Load<RoundingAssetDatabase>("RoundingAssetDatabase");

			}
		}

		public static void SetUIScale(int a_newScale)
		{
			Instance.m_uiScale = a_newScale;
			foreach(IUIScaleChangeReceiver receiver in Instance.m_uiScaleCallbackList)
				receiver.OnUIScaleChange(a_newScale);
		}

		public static void RegisterUIScaleChangeReceiver(IUIScaleChangeReceiver a_receiver)
		{
			Instance.m_uiScaleCallbackList.Add(a_receiver);
		}

		public static void UnRegisterUIScaleChangeReceiver(IUIScaleChangeReceiver a_receiver)
		{
			Instance.m_uiScaleCallbackList.Remove(a_receiver);
		}
	}
}
