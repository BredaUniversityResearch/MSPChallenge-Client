using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class LayerCategoryBar : MonoBehaviour
	{
		[SerializeField] Transform m_contentParent;
		[SerializeField] TextMeshProUGUI m_title;

        public Transform ContentParent => m_contentParent;

		public void SetContent(string a_text)
		{
			m_title.text = a_text;
		}
    }
}
