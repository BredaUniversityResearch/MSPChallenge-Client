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
		[SerializeField] IPointerClickHandler m_uiElement;
		[SerializeField] string[] m_tags;

		void Start()
		{
			if(m_uiElement is Button b)
			{
				InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, b);
				b.onClick.AddListener(() => InterfaceCanvas.Instance.TriggerInteractionCallback(gameObject.name, m_tags));

			}
			if (m_uiElement is Toggle t)
			{
				InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, t);
				t.onValueChanged.AddListener((b) => InterfaceCanvas.Instance.TriggerInteractionCallback(gameObject.name, m_tags));
			}
			else
			{
				InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, m_uiElement);
			}
		}
	}
}