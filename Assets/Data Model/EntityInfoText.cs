using UnityEngine;

public class EntityInfoText
{
	private readonly EntityInfoTextConfig config;
	private readonly GameObject rootTextObject;
	private readonly TextMesh textMesh;
	private readonly Renderer textRenderer;
	private readonly SubEntity ownerSubEntity;

	private readonly GameObject highlightBackground;
	private readonly SpriteRenderer highlightBackgroundRenderer;

	private float zOffset = 0.0f;
	private float lastTextScale = 1.0f;

	public EntityInfoText(SubEntity ownerSubEntity, LayerTextInfo textInfo, Transform parentTransform)
	{
		config = Resources.Load<EntityInfoTextConfig>("EntityInfoTextConfig");
		if (config == null)
		{
			Debug.LogError("Could not find Config for EntityInfoText at \"EntityInfoTextConfig\". Please verify that this asset exists in a resources folder");
		}

		this.ownerSubEntity = ownerSubEntity;
		LayerTextInfo info = textInfo;

		rootTextObject = new GameObject("TextObject");
		rootTextObject.transform.SetParent(parentTransform, false);

		textMesh = rootTextObject.AddComponent<TextMesh>();
		textMesh.anchor = TextAnchor.MiddleCenter;
		textMesh.alignment = TextAlignment.Center;
		textMesh.text = GetTextTypeText();
		textMesh.font = config.TextFont;
		textMesh.fontSize = info.GetTextSize();
		textMesh.color = info.textColor;
		textRenderer = rootTextObject.GetComponent<Renderer>();
		textRenderer.sortingOrder = 10;
		textRenderer.sharedMaterial = config.TextFont.material;

		highlightBackground = new GameObject("HighlightBackground");
		highlightBackground.transform.SetParent(rootTextObject.transform, false);
		highlightBackground.transform.localPosition = new Vector3(0.0f, 0.0f, 0.025f);
		highlightBackground.transform.localScale = new Vector3(config.BackgroundScale, config.BackgroundScale, 1.0f); //Scale it a bit up so it looks neater.

		highlightBackgroundRenderer = highlightBackground.AddComponent<SpriteRenderer>();
		highlightBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;
		highlightBackgroundRenderer.sprite = config.BackgroundSprite;
		highlightBackgroundRenderer.sortingOrder = 10;
	}

	public void Destroy()
	{
		Object.Destroy(rootTextObject);
		Object.Destroy(highlightBackground);
	}

	public void SetPosition(Vector3 position, bool isWorldPosition)
	{
		if (isWorldPosition)
			rootTextObject.transform.position = position;
		else
			rootTextObject.transform.localPosition = position;
	}

	public void SetVisibility(bool visibility)
	{
		rootTextObject.SetActive(visibility);
	}

	public void SetBackgroundVisibility(bool visibility)
	{
		highlightBackground.gameObject.SetActive(visibility);
	}

	public void UpdateTextMeshText()
	{
		textMesh.text = GetTextTypeText();
		UpdateBackgroundSize(lastTextScale);
	}

	private string GetTextTypeText()
	{
		ETextState state = Main.GetTextState();
		string propertyName = "";
		if (ownerSubEntity.Entity.Layer.textInfo.TryGetPropertyNameAtState(state, out propertyName))
		{
			return ownerSubEntity.GetProperty(propertyName);
		}

		return "";
	}

	/// <summary>
	/// scaleWithDisplay determines whether the current display scale is used for characterSize. 
	/// This should not be used if the textmesh gameobject has already been scaled (like for points)
	/// </summary>
	public void UpdateTextMeshScale(bool inverseScaleTextMesh, float parentScale)
	{
		//textMesh.characterSize = scaleWithDisplay ? DisplayScale * textResolutionScale : textResolutionScale;
		float baseScale = VisualizationUtil.DisplayScale * VisualizationUtil.textResolutionScale;
		float scale = baseScale;
		if (inverseScaleTextMesh)
		{
			scale = (1f / parentScale) * scale;
		}
		textMesh.transform.localScale = new Vector3(scale, scale, 1.0f);

		lastTextScale = baseScale;
		UpdateBackgroundSize(lastTextScale);
	}

	private void UpdateBackgroundSize(float textScale)
	{
		Vector3 unscaledExtents = (textRenderer.bounds.extents / textScale) / (config.BackgroundScale / 2.0f);
		highlightBackgroundRenderer.size = new Vector2(unscaledExtents.x, unscaledExtents.y) + config.BackgroundExtrude;
	}

	public void SetZOffset(float newZOffset)
	{
		zOffset = newZOffset;
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		Vector3 localPosition = rootTextObject.transform.localPosition;
		localPosition.z = zOffset;
		rootTextObject.transform.localPosition = localPosition;
	}
}