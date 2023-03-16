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
		[FormerlySerializedAs("m_registerReference")]
		[SerializeField] bool m_registerNameReference = true;
		[SerializeField] bool m_registerTagsReference = false;

		void Start()
		{
			if (m_uiElement == null)
			{
				if(m_registerNameReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, gameObject);
			}
			else if(m_uiElement is Button b)
			{
				if(m_registerNameReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, b);
				b.onClick.AddListener(TriggerCallback);

			}
			else if (m_uiElement is Toggle t)
			{
				if(m_registerNameReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, t);
				t.onValueChanged.AddListener((boolv) => TriggerCallback());
			}
			else
			{
				if(m_registerNameReference)
					InterfaceCanvas.Instance.RegisterUIReference(gameObject.name, gameObject);
			}
			if(m_registerTagsReference && m_tags != null && m_tags.Length > 0)
			{
				InterfaceCanvas.Instance.RegisterUITagsReference(m_tags, gameObject);
			}
		}

		void TriggerCallback()
		{
			InterfaceCanvas.Instance.TriggerInteractionCallback(gameObject.name, m_tags);
		}

		void OnDestroy()
		{
			if (InterfaceCanvas.Instance == null)
				return;

			if (m_registerNameReference)
				InterfaceCanvas.Instance.UnregisterUIReference(gameObject.name);
			if (m_registerTagsReference && m_tags != null && m_tags.Length > 0)
				InterfaceCanvas.Instance.UnregisterUITagsReference(m_tags, gameObject);			
		}

		public void SetTags(string[] a_tags)
		{
			m_tags = a_tags;
		}

		public bool HasTag(string a_tag)
		{
			if (m_tags == null)
				return false;
			foreach (string tag in m_tags)
			{
				if (tag == a_tag)
					return true;
			}
			return false;
		}
	}
}