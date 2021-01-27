using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SetTextToYear : MonoBehaviour {

    public int era;
    public int yearOffset;

    private void Start()
    {
        if (Main.MspGlobalData != null)
        {
            SetYear();
        }
        else
        {
            Main.OnGlobalDataLoaded += GlobalDataLoaded;
        }
    }

    void GlobalDataLoaded()
    {
        Main.OnGlobalDataLoaded -= GlobalDataLoaded;
        SetYear();
    }

    void SetYear()
    {
        GetComponent<TextMeshProUGUI>().text = (Main.MspGlobalData.start + era * Main.MspGlobalData.YearsPerEra + yearOffset).ToString();
    }
}
