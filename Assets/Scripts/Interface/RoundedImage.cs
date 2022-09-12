using System;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using MSP2050.Scripts;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

//namespace MSP2050.Scripts
namespace UnityEngine.UI
{
	/// <summary>
	/// Image is a textured element in the UI hierarchy.
	/// </summary>

	[AddComponentMenu("UI/RoundedImage", 11)]
	public class RoundedImage : MaskableGraphic, ILayoutElement, ICanvasRaycastFilter, IUIScaleChangeReceiver
	{
		[SerializeField] private bool m_FillCenter = true;

		public bool fillCenter
		{
			get { return m_FillCenter; }
			set
			{
				m_FillCenter = value;
				SetVerticesDirty();
			}
		}

		[SerializeField] private RoundingAssetDatabase.ESliceSection m_slice;
		[SerializeField] private int m_rounding;

		private Vector4 m_borderDisplaySize;
		private Sprite[] m_sprites;

		private Sprite CurrentSprite
		{
			get
			{
				if (m_sprites == null)
					m_sprites = RoundingManager.RoundingAssetDatabase.GetSprites(m_rounding / 4 - 1, m_slice);

				return m_sprites[RoundingManager.UIScale];
			}
		}

		// Not serialized until we support read-enabled sprites better.
		private float m_EventAlphaThreshold = 1;
		public float eventAlphaThreshold { get { return m_EventAlphaThreshold; } set { m_EventAlphaThreshold = value; } }

		protected override void Start()
		{
			base.Start();
			RoundingManager.RegisterUIScaleChangeReceiver(this);
			switch (m_slice)
			{
				case RoundingAssetDatabase.ESliceSection.Full:
					m_borderDisplaySize = new Vector4(m_rounding, m_rounding, m_rounding, m_rounding);
					break;
				case RoundingAssetDatabase.ESliceSection.Left:
					m_borderDisplaySize = new Vector4(m_rounding, 0f, 0f, 0f);
					break;
				case RoundingAssetDatabase.ESliceSection.Top:
					m_borderDisplaySize = new Vector4(0f, m_rounding, 0f, 0f);
					break;
				case RoundingAssetDatabase.ESliceSection.Right:
					m_borderDisplaySize = new Vector4(0f, 0f, m_rounding, 0f);
					break;
				case RoundingAssetDatabase.ESliceSection.Bottom:
					m_borderDisplaySize = new Vector4(0f, 0f, 0f, m_rounding);
					break;
				case RoundingAssetDatabase.ESliceSection.TopLeft:
					m_borderDisplaySize = new Vector4(m_rounding, m_rounding, 0f, 0f);
					break;
				case RoundingAssetDatabase.ESliceSection.TopRight:
					m_borderDisplaySize = new Vector4(0f, m_rounding, m_rounding, 0f);
					break;
				case RoundingAssetDatabase.ESliceSection.BottomRight:
					m_borderDisplaySize = new Vector4(0f, 0f, m_rounding, m_rounding);
					break;
				default:
					m_borderDisplaySize = new Vector4(m_rounding, 0f, 0f, m_rounding);
					break;
			}
			SetAllDirty();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			RoundingManager.UnRegisterUIScaleChangeReceiver(this);
		}

		protected RoundedImage()
		{
			useLegacyMeshGeneration = false;
		}

		public override Texture mainTexture => CurrentSprite.texture;

		public float pixelsPerUnit
		{
			get
			{
				float spritePixelsPerUnit = 100;
				if (CurrentSprite)
					spritePixelsPerUnit = CurrentSprite.pixelsPerUnit;

				float referencePixelsPerUnit = 100;
				if (canvas)
					referencePixelsPerUnit = canvas.referencePixelsPerUnit;

				return spritePixelsPerUnit / referencePixelsPerUnit;
			}
		}

		/// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
		private Vector4 GetDrawingDimensions()
		{
			var padding = CurrentSprite == null ? Vector4.zero : Sprites.DataUtility.GetPadding(CurrentSprite);
			var size = CurrentSprite == null ? Vector2.zero : new Vector2(CurrentSprite.rect.width, CurrentSprite.rect.height);

			Rect r = GetPixelAdjustedRect();
			// Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

			int spriteW = Mathf.RoundToInt(size.x);
			int spriteH = Mathf.RoundToInt(size.y);

			var v = new Vector4(
					padding.x / spriteW,
					padding.y / spriteH,
					(spriteW - padding.z) / spriteW,
					(spriteH - padding.w) / spriteH);

			v = new Vector4(
					r.x + r.width * v.x,
					r.y + r.height * v.y,
					r.x + r.width * v.z,
					r.y + r.height * v.w
					);

			return v;
		}

		public override void SetNativeSize()
		{
			float w = CurrentSprite.rect.width / pixelsPerUnit;
			float h = CurrentSprite.rect.height / pixelsPerUnit;
			rectTransform.anchorMax = rectTransform.anchorMin;
			rectTransform.sizeDelta = new Vector2(w, h);
			SetAllDirty();
		}

		static readonly Vector2[] s_VertScratch = new Vector2[4];
		static readonly Vector2[] s_UVScratch = new Vector2[4];
		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			Vector4 outer = Sprites.DataUtility.GetOuterUV(CurrentSprite);
			Vector4 inner = Sprites.DataUtility.GetInnerUV(CurrentSprite);
			Vector4 padding = Sprites.DataUtility.GetPadding(CurrentSprite);


			Rect rect = GetPixelAdjustedRect();
			Vector4 border = GetAdjustedBorders(m_borderDisplaySize / pixelsPerUnit, rect);
			padding = padding / pixelsPerUnit;

			s_VertScratch[0] = new Vector2(padding.x, padding.y);
			s_VertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);

			s_VertScratch[1].x = border.x;
			s_VertScratch[1].y = border.y;
			s_VertScratch[2].x = rect.width - border.z;
			s_VertScratch[2].y = rect.height - border.w;

			for (int i = 0; i < 4; ++i)
			{
				s_VertScratch[i].x += rect.x;
				s_VertScratch[i].y += rect.y;
			}

			s_UVScratch[0] = new Vector2(outer.x, outer.y);
			s_UVScratch[1] = new Vector2(inner.x, inner.y);
			s_UVScratch[2] = new Vector2(inner.z, inner.w);
			s_UVScratch[3] = new Vector2(outer.z, outer.w);

			toFill.Clear();

			for (int x = 0; x < 3; ++x)
			{
				int x2 = x + 1;

				for (int y = 0; y < 3; ++y)
				{
					if (!m_FillCenter && x == 1 && y == 1)
						continue;

					int y2 = y + 1;

					AddQuad(toFill,
						new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
						new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
						color,
						new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
						new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
				}
			}
		}

		static void AddQuad(VertexHelper vertexHelper, Vector3[] quadPositions, Color32 color, Vector3[] quadUVs)
		{
			int startIndex = vertexHelper.currentVertCount;

			for (int i = 0; i < 4; ++i)
				vertexHelper.AddVert(quadPositions[i], color, quadUVs[i]);

			vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax)
		{
			int startIndex = vertexHelper.currentVertCount;

			vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
			vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
			vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
			vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

			vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
		{
			for (int axis = 0; axis <= 1; axis++)
			{
				// If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
				// In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
				float combinedBorders = border[axis] + border[axis + 2];
				if (rect.size[axis] < combinedBorders && combinedBorders != 0)
				{
					float borderScaleRatio = rect.size[axis] / combinedBorders;
					border[axis] *= borderScaleRatio;
					border[axis + 2] *= borderScaleRatio;
				}
			}
			return border;
		}

		public virtual void CalculateLayoutInputHorizontal() { }
		public virtual void CalculateLayoutInputVertical() { }

		public virtual float minWidth { get { return 0; } }

		public virtual float preferredWidth
		{
			get
			{
				return Sprites.DataUtility.GetMinSize(CurrentSprite).x / pixelsPerUnit;
			}
		}

		public virtual float flexibleWidth { get { return -1; } }

		public virtual float minHeight { get { return 0; } }

		public virtual float preferredHeight
		{
			get
			{
				return Sprites.DataUtility.GetMinSize(CurrentSprite).y / pixelsPerUnit;
			}
		}

		public virtual float flexibleHeight { get { return -1; } }

		public virtual int layoutPriority { get { return 0; } }

		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			if (m_EventAlphaThreshold >= 1)
				return true;

			Sprite sprite = this.CurrentSprite;
			if (sprite == null)
				return true;

			Vector2 local;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local);

			Rect rect = GetPixelAdjustedRect();

			// Convert to have lower left corner as reference point.
			local.x += rectTransform.pivot.x * rect.width;
			local.y += rectTransform.pivot.y * rect.height;

			local = MapCoordinate(local, rect);

			// Normalize local coordinates.
			Rect spriteRect = sprite.textureRect;
			Vector2 normalized = new Vector2(local.x / spriteRect.width, local.y / spriteRect.height);

			// Convert to texture space.
			float x = Mathf.Lerp(spriteRect.x, spriteRect.xMax, normalized.x) / sprite.texture.width;
			float y = Mathf.Lerp(spriteRect.y, spriteRect.yMax, normalized.y) / sprite.texture.height;

			try
			{
				return sprite.texture.GetPixelBilinear(x, y).a >= m_EventAlphaThreshold;
			}
			catch (UnityException e)
			{
				Debug.LogError("Using clickAlphaThreshold lower than 1 on Image whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
				return true;
			}
		}

		private Vector2 MapCoordinate(Vector2 local, Rect rect)
		{
			Rect spriteRect = CurrentSprite.rect;

			Vector4 adjustedBorder = GetAdjustedBorders(m_borderDisplaySize / pixelsPerUnit, rect);

			for (int i = 0; i < 2; i++)
			{
				if (local[i] <= adjustedBorder[i])
					continue;

				if (rect.size[i] - local[i] <= adjustedBorder[i + 2])
				{
					local[i] -= (rect.size[i] - spriteRect.size[i]);
					continue;
				}
				
				float lerp = Mathf.InverseLerp(adjustedBorder[i], rect.size[i] - adjustedBorder[i + 2], local[i]);
				local[i] = Mathf.Lerp(m_borderDisplaySize[i], spriteRect.size[i] - m_borderDisplaySize[i + 2], lerp);
			}

			return local;
		}

		public void OnUIScaleChange(int a_newScale)
		{
			SetMaterialDirty();
		}
	}
}
