using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FilterToggle : MonoBehaviour {

    public Color color;
    public Image toggleTransparentBackground, toggleOpaqueBackground;
    public Toggle toggle;

	public int teamId { get; private set; }

	void Start()
    {
        SetColor(color);
        toggle.onValueChanged.AddListener((b) => Toggle(b));
    }

	public void Initialize(int teamId)
	{
		this.teamId = teamId;
	}

	public void SetColor(Color col)
    {
        toggleTransparentBackground.color = new Color(col.r, col.g, col.b, toggleTransparentBackground.color.a);
        toggleOpaqueBackground.color = new Color(col.r, col.g, col.b, toggleOpaqueBackground.color.a);

    }

    public void Toggle(bool toggle)
    {
        // Disables the opaque image
        toggleOpaqueBackground.gameObject.SetActive(toggle);
    }
}
