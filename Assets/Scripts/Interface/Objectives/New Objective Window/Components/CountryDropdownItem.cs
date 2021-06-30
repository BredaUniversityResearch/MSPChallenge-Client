using UnityEngine;
using UnityEngine.UI;

public class CountryDropdownItem : MonoBehaviour
{
    public Image countryGraphic;
    public Image countryGraphicOutline;

    public void Start()
    {
        // Set colour depending on place in hierarchy
        string tText = transform.Find("Item Label").GetComponent<Text>().text;
        Team targetTeam = TeamManager.FindTeamByName(tText);
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