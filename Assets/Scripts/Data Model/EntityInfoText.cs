using TMPro;
using UnityEngine;

namespace MSP2050.Scripts
{
	public class EntityInfoText
	{
		private readonly GameObject rootTextObject;
		private readonly TextMeshPro textMesh;
		private readonly Renderer textRenderer;
		private readonly SubEntity ownerSubEntity;
		private readonly EntityInfoTextInstance instance;

		private readonly GameObject highlightBackground;
		private readonly SpriteRenderer highlightBackgroundRenderer;

		private float zOffset = 0.0f;
		private float lastTextScale = 1.0f;

		public EntityInfoText(SubEntity ownerSubEntity, LayerTextInfo textInfo, Transform parentTransform)
		{
			this.ownerSubEntity = ownerSubEntity;
			LayerTextInfo info = textInfo;

			GameObject textPrefab = Resources.Load<GameObject>("EntityTextInstance/EntityInfoTextInstance");

			rootTextObject = GameObject.Instantiate(textPrefab, parentTransform);

			instance = rootTextObject.GetComponent<EntityInfoTextInstance>();

			SetPosition(rootTextObject.transform.position, true);

			textMesh = instance.m_text;
			textMesh.alignment = TextAlignmentOptions.Center;
			textMesh.text = GetTextTypeText();
			textMesh.fontSize = info.GetTextSize();
			textMesh.color = info.textColor;
			textRenderer = rootTextObject.GetComponent<Renderer>();
			textRenderer.sortingOrder = 11;

			highlightBackground = new GameObject("HighlightBackground");
			highlightBackground.transform.SetParent(rootTextObject.transform, false);
			highlightBackground.transform.localPosition = Vector3.zero; // Needs -7 to avoid being behind cables
			highlightBackground.transform.localScale = new Vector3(instance.m_backgroundScale, instance.m_backgroundScale, 1.0f); // Scale it a bit up so it looks neater.

			highlightBackgroundRenderer = highlightBackground.AddComponent<SpriteRenderer>();
			highlightBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;
			highlightBackgroundRenderer.sprite = instance.m_background.sprite;
			highlightBackgroundRenderer.sortingOrder = 10;
		}

		public void Destroy()
		{
			Object.Destroy(rootTextObject);
			Object.Destroy(highlightBackground);
		}

		public void SetPosition(Vector3 position, bool isWorldPosition)
		{
			position.z = -7.0f; // Needs -7 to avoid being behind cables
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
			if (ownerSubEntity.m_entity.Layer.m_textInfo.TryGetPropertyNameAtState(state, out propertyName))
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
			float baseScale = VisualizationUtil.Instance.DisplayScale * VisualizationUtil.Instance.textResolutionScale;
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
			Vector3 unscaledExtents = (textRenderer.bounds.extents / textScale) / (instance.m_backgroundScale / 2.0f);
			highlightBackgroundRenderer.size = new Vector2(unscaledExtents.x, unscaledExtents.y) + new Vector2(instance.m_backgroundExtrude, instance.m_backgroundExtrude);
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
}