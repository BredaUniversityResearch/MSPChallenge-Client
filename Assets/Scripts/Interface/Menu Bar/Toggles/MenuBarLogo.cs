using TMPro;

namespace MSP2050.Scripts
{
	public class MenuBarLogo : MenuBarToggle
	{ 
		public TextMeshProUGUI text;

		public void SetRegionLogo(RegionInfo region)
		{
			text.text = region.letter;
		}
	}
}