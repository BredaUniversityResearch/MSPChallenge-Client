using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace MSP2050.Scripts
{
	public class SetLayoutToPrefHeight : SerializedMonoBehaviour
	{
		[SerializeField] private LayoutGroup m_targetGroup = null;
		[SerializeField] private ILayoutElement m_sourceElement = null;
		[SerializeField] private LayoutElement m_targetElement = null;
		[SerializeField] private float m_maxHeight = 10000f;
		[SerializeField] private bool m_setMin = false;
		
		private float oldHeight;

		void Awake()
		{
			if(m_targetGroup != null)
				oldHeight = m_targetGroup.preferredHeight;
		}

		void Update()
		{
			float newHeight = 0f;
			if (m_targetGroup != null)
				newHeight = Mathf.Min(m_maxHeight, m_targetGroup.preferredHeight);
			else
				newHeight = Mathf.Min(m_maxHeight, m_sourceElement.preferredHeight);
			
			if (!Mathf.Approximately(newHeight, oldHeight))
			{
				if(m_setMin)
					m_targetElement.minHeight = newHeight;
				else
					m_targetElement.preferredHeight = newHeight;
			}
			oldHeight = newHeight;
		}
	}
}