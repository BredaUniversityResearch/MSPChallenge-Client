using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OnHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Graphics to change")]
    public Graphic[] graphics;

	[Header("Override base (default is img color)")]
	public bool overrideBaseColor;
	public Color fadeFromColor;


	[Header("Override target (default is accent)")]
	public bool overrideTargetColor;
	public Color fadeToColor;

    [HideInInspector]
    public bool lockColor;
    private Color[] defaultColors;
    private float transval;
    private Selectable selectable;
    private bool pointerOnButton;

    void Awake()
    {
		defaultColors = new Color[graphics.Length];
        selectable = GetComponent<Selectable>();
		for(int i = 0; i < graphics.Length; i++)
			defaultColors[i] = overrideBaseColor ? fadeFromColor : graphics[i].color;
    }

    void Update() {
        if (selectable.interactable && !lockColor && !pointerOnButton)
		{
            if (transval > 0f) {
                transval -= Time.deltaTime * 3f;
				for (int i = 0; i < graphics.Length; i++)
					graphics[i].color = Color.Lerp(defaultColors[i], overrideTargetColor ? fadeToColor : TeamManager.CurrentTeamColor, transval);
			}
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable.interactable && !lockColor)
		{
			for (int i = 0; i < graphics.Length; i++)
				graphics[i].color = overrideTargetColor ? fadeToColor : TeamManager.CurrentTeamColor;
		}

        pointerOnButton = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectable.interactable)
		{
            transval = 1f;
        }
        pointerOnButton = false;
    }

	public void LockToColor(Color color)
	{
		lockColor = true;
		for (int i = 0; i < graphics.Length; i++)
			graphics[i].color = color;
	}

	public void UnlockColor()
	{
		lockColor = false;
		for (int i = 0; i < graphics.Length; i++)
			graphics[i].color = defaultColors[i];
	}
}