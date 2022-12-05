using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class PlansGroupBar : MonoBehaviour
	{
		[SerializeField] TextMeshProUGUI m_title;
		[SerializeField] Toggle m_expandToggle;
		[SerializeField] Image m_icon;
		[SerializeField] Transform m_contentContainer;
		[SerializeField] AddTooltip m_tooltip;
		[SerializeField] GameObject m_emptyEntry;

		public Transform ContentParent => m_contentContainer;

		private void Start()
		{
			m_expandToggle.onValueChanged.AddListener(m_contentContainer.gameObject.SetActive);
		}

		public void SetContent(string a_title, string a_tooltip, Sprite a_icon)
		{
			m_title.text = a_title;
			m_tooltip.text = a_tooltip;
			if (a_icon != null)
				m_icon.sprite = a_icon;
			else
				m_icon.gameObject.SetActive(false);
			m_emptyEntry.SetActive(true);
		}

		public void CheckEmpty()
		{
			for(int i = 1; i < m_contentContainer.childCount; i++)
			{
				if(m_contentContainer.GetChild(i).gameObject.activeSelf)
				{
					m_emptyEntry.SetActive(false);
					return;
				}
			}
			m_emptyEntry.SetActive(true);
		}

		public void SetExpanded(bool a_value)
		{
			m_expandToggle.isOn = a_value;
		}
	}
}