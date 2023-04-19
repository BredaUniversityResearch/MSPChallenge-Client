using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace MSP2050.Scripts
{
	public class LayerSubCategoryBar : MonoBehaviour {

		[SerializeField] Toggle m_toggle;
		[SerializeField] Image m_icon;
		[SerializeField] TextMeshProUGUI m_name;

		Action<bool, string> m_clickCallback;
		string m_subcategoryName;

		private void Start()
		{
			m_toggle.onValueChanged.AddListener(OnClick);
		}

		public void SetContent(string a_displayName, string a_subcategoryName, Sprite a_icon, Action<bool, string> a_clickCallback, ToggleGroup a_toggleGroup)
		{
			m_name.text = a_displayName;	
			m_icon.sprite = a_icon;
			m_clickCallback = a_clickCallback;
			m_subcategoryName = a_subcategoryName;
			m_toggle.group = a_toggleGroup;
		}

		void OnClick(bool a_value)
		{
			m_clickCallback?.Invoke(a_value, m_subcategoryName);
		}

		public void SetToggled(bool a_value)
		{
			m_toggle.isOn = a_value;
		}
	}
}
