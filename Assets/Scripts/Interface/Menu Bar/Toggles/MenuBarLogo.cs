using TMPro;

namespace MSP2050.Scripts
{
	public class MenuBarLogo : MenuBarToggle
	{ 
		public TextMeshProUGUI text;

		public void SetRegionLetter(string letter)
		{
			text.text = letter;
		}
	}
}