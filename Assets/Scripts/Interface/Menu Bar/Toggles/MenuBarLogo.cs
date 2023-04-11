using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class MenuBarLogo : MenuBarToggle
	{ 
		public Image m_logoImage;

		private void Start()
		{
			m_logoImage.color = SessionManager.Instance.CurrentTeam.color;
			Initilise();
		}
	}
}