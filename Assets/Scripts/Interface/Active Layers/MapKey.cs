using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapKey : MonoBehaviour
{
	public CustomToggle barToggle;
	public TextMeshProUGUI label;

	public Image pointKey, outlineKey, visibilityImage, lineKey;
	public RawImage areaKey;
	public Sprite visibleSprite, invisibleSprite;

	public AbstractLayer layer;
	public EntityType entityType;

	public int ID;
	bool togglableVisibility = true;
	bool ignoreCallback = false;

	public delegate void ToggleMapKeyDelegate(bool newToggleState);

	void Start()
	{
		layer.OnEntityTypeVisibilityChanged += OnLayerEntityTypeVisibilityChanged;
        barToggle.onValueChanged.AddListener((b) =>
        {
            if (togglableVisibility && !ignoreCallback)
            {
                ignoreCallback = true;
                visibilityImage.sprite = barToggle.isOn ? visibleSprite : invisibleSprite;
                if (layer.SetEntityTypeVisibility(entityType, barToggle.isOn))
                    layer.SetActiveToCurrentPlanAndRedraw();
                ignoreCallback = false;
            }
		});
	}

	private void OnDestroy()
	{
		layer.OnEntityTypeVisibilityChanged -= OnLayerEntityTypeVisibilityChanged;
	}


	private void OnLayerEntityTypeVisibilityChanged(EntityType entityType, bool newVisibilityState)
	{
		if (ignoreCallback)
			return;

		if (entityType == this.entityType && togglableVisibility)
		{
			ignoreCallback = true;
			barToggle.isOn = newVisibilityState;
			visibilityImage.sprite = barToggle.isOn ? visibleSprite : invisibleSprite;
			SetLayerKeyActiveState(newVisibilityState);
			ignoreCallback = false;
		}
	}

    public void SetInteractable(bool value)
    {
        barToggle.interactable = value;
    }

	private void SetLayerKeyActiveState(bool activeState)
	{
		float alphaToSet = activeState ? 1.0f : 0.5f;
		ApplyAlphaTint(label, alphaToSet);
		ApplyAlphaTint(areaKey, alphaToSet);
	}

	private void ApplyAlphaTint(Graphic target, float alphaToSet)
	{
		Color col = target.color;
		col.a = alphaToSet;
		target.color = col;
	}

	public void DisableVisibilityToggle()
	{
		togglableVisibility = false;
		visibilityImage.enabled = false;
	}
}