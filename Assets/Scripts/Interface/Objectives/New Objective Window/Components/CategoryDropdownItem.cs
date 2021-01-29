using UnityEngine;
using UnityEngine.UI;

public class CategoryDropdownItem : MonoBehaviour
{
    public GameObject header;
    public GameObject option;

    public Text headerLabel;
    public Text optionLabel;

    public Toggle rootToggle;
    public Toggle optionToggle;

    public void Start()
    {
        bool isHeader = optionLabel.text.Contains("-");

        header.SetActive(isHeader);
        option.SetActive(!isHeader);

        headerLabel.text = optionLabel.text.TrimStart('-');

        if (rootToggle.isOn == true) {
            optionToggle.Select();
        }
    }
}