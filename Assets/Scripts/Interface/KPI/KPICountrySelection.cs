using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class KPICountrySelection : MonoBehaviour
	{
		[SerializeField] Toggle m_toggle = null;
		[SerializeField] Graphic m_teamImage = null;

		public void SetSelected(bool a_isSelected)
		{
			m_toggle.isOn = a_isSelected;
		}

		public void SetTeamColor(Color a_teamColor, ToggleGroup a_group)
		{
			m_teamImage.color = a_teamColor;
			m_toggle.group = a_group;
		}

		public void SetToggleChangeHandler(UnityAction<bool> a_callback)
		{
			m_toggle.onValueChanged.AddListener(a_callback);
		}
	}
}
