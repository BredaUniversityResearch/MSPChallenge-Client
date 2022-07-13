using UnityEngine;
using UnityEngine.UI;

namespace MSP2050.Scripts
{
	public class CountryDropdownItem : MonoBehaviour
	{
		public Image countryGraphic;
		public Image countryGraphicOutline;

		public void Start()
		{
			// Set colour depending on place in hierarchy
			string tText = transform.Find("Item Label").GetComponent<Text>().text;
			Team targetTeam = SessionManager.Instance.FindTeamByName(tText);
			if (targetTeam != null)
			{
				countryGraphic.color = targetTeam.color;
			}
			else
			{
				countryGraphic.gameObject.SetActive(false);
				countryGraphicOutline.gameObject.SetActive(false);
			}
		}
	}
}