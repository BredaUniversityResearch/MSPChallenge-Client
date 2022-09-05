using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace MSP2050.Scripts
{
	public class RegisterInteraction : SerializedMonoBehaviour
	{
		[SerializeField] Selectable m_uiElement;
		[SerializeField] string[] m_tags;
		[SerializeField] bool m_registerReference = true;

		void Start()
		{
			if (m_uiElement == null)
			{
				if(m_registerReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, gameObject);
			}
			else if(m_uiElement is Button b)
			{
				if(m_registerReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, b);
				b.onClick.AddListener(() => InterfaceCanvas.Instance.TriggerInteractionCallback(gameObject.name, m_tags));

			}
			else if (m_uiElement is Toggle t)
			{
				if(m_registerReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, t);
				t.onValueChanged.AddListener((boolv) => InterfaceCanvas.Instance.TriggerInteractionCallback(gameObject.name, m_tags));
			}
			else
			{
				if(m_registerReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, gameObject);
			}
		}

		void OnDestroy()
		{
			if (m_registerReference)
				InterfaceCanvas.Instance.UnregisterUIReference(gameObject.name);
		}
	}
}