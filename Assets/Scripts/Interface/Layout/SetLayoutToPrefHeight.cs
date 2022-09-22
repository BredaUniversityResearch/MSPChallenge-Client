using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class SetLayoutToPrefHeight : MonoBehaviour
	{
		[SerializeField] private LayoutGroup m_targetGroup = null;
		[SerializeField] private LayoutElement m_targetElement = null;

		private float oldHeight;

		void Awake()
		{
			oldHeight = m_targetGroup.preferredHeight;
		}

		void Update()
		{
			float newHeight = m_targetGroup.preferredHeight;
			if (!Mathf.Approximately(newHeight, oldHeight))
			{
				m_targetElement.preferredHeight = newHeight;
			}
			oldHeight = newHeight;
		}
	}
}