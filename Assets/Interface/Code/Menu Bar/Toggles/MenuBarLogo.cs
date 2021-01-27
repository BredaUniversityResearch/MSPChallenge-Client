using UnityEngine.UI;
using TMPro;

public class MenuBarLogo : MenuBarToggle
{ 
    public TextMeshProUGUI text;

    public void SetRegionLogo(RegionInfo region)
    {
		text.text = region.letter;
    }
}