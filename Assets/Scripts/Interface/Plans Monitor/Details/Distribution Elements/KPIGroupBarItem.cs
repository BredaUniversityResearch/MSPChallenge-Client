using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KPIGroupBarItem : MonoBehaviour {

    public Image teamGraphic;
    public TextMeshProUGUI numbers;
    public TextMeshProUGUI title;
    [HideInInspector]
	public float value;
    [HideInInspector]
    public int team;
}