#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;

namespace RoundingEditorUtility
{
	[System.Flags]
	public enum ESliceSection
	{
		Sides = (1 << 1) + (1 << 2) + (1 << 3) + (1 << 4),
		Corners = (1 << 5) + (1 << 6) + (1 << 7) + (1 << 8),
		Full = (1 << 0),
		Left = (1 << 1),
		Top = (1 << 2),
		Right = (1 << 3),
		Bottom = (1 << 4),
		TopLeft = (1 << 5),
		TopRight = (1 << 6),
		BottomRight = (1 << 7),
		BottomLeft = (1 << 8),
	}

	public class SpriteSlicerWindow : OdinEditorWindow
	{
		[SerializeField] Texture2D m_targetSprite;
		[SerializeField] int m_borderLeft;
		[SerializeField] int m_borderRight;
		[SerializeField] int m_borderTop;
		[SerializeField] int m_borderBottom;
		[SerializeField] ESliceSection m_slices;

		[Button("Slice")]
		public void Slice()
		{
			if (m_targetSprite == null)
			{
				EditorUtility.DisplayDialog("No sprite selected", "No sprite was selected for slicing.", "Confirm");
				return;
			}
			if (m_slices == 0)
			{
				EditorUtility.DisplayDialog("No slice selected", "A sprite cannot be selected without any selected slices,", "Confirm");
				return;
			}

			string path = AssetDatabase.GetAssetPath(m_targetSprite);
			TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
			ti.isReadable = true;
			List<SpriteMetaData> newData = new List<SpriteMetaData>();
			SpriteMetaData smd;

			if (ti.spriteImportMode == SpriteImportMode.Multiple)
			{
				// Bug? Need to convert to single then back to multiple in order to make changes when it's already sliced
				ti.spriteImportMode = SpriteImportMode.Single;
				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			}
			ti.spriteImportMode = SpriteImportMode.Multiple;

			if ((m_slices & ESliceSection.Full) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_borderLeft, m_borderBottom, m_borderRight, m_borderTop);
				smd.rect = Rect.MinMaxRect(0, 0, m_targetSprite.width, m_targetSprite.height);
				smd.name = m_targetSprite.name + "_Full";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Top) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_borderLeft, 0, m_borderRight, m_borderTop);
				smd.rect = Rect.MinMaxRect(0, m_borderBottom, m_targetSprite.width, m_targetSprite.height);
				smd.name = m_targetSprite.name + "_Top";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Bottom) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_borderLeft, m_borderBottom, m_borderRight, 0);
				smd.rect = Rect.MinMaxRect(0, 0, m_targetSprite.width, m_targetSprite.height - m_borderTop);
				smd.name = m_targetSprite.name + "_Bottom";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Left) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_borderLeft, m_borderBottom, 0, m_borderTop);
				smd.rect = Rect.MinMaxRect(0, 0, m_targetSprite.width - m_borderRight, m_targetSprite.height);
				smd.name = m_targetSprite.name + "_Left";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Right) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(0, m_borderBottom, m_borderRight, m_borderTop);
				smd.rect = Rect.MinMaxRect(m_borderLeft, 0, m_targetSprite.width, m_targetSprite.height);
				smd.name = m_targetSprite.name + "_Right";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.TopRight) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(0, 0, m_borderRight, m_borderTop);
				smd.rect = Rect.MinMaxRect(m_borderLeft, m_borderBottom, m_targetSprite.width, m_targetSprite.height);
				smd.name = m_targetSprite.name + "_TopRight";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.BottomRight) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(0, m_borderBottom, m_borderRight, 0);
				smd.rect = Rect.MinMaxRect(m_borderLeft, 0, m_targetSprite.width, m_targetSprite.height - m_borderTop);
				smd.name = m_targetSprite.name + "_BottomRight";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.BottomLeft) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_borderLeft, m_borderBottom, 0, 0);
				smd.rect = Rect.MinMaxRect(0, 0, m_targetSprite.width - m_borderRight, m_targetSprite.height - m_borderTop);
				smd.name = m_targetSprite.name + "_BottomLeft";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.TopLeft) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_borderLeft, 0, 0, m_borderTop);
				smd.rect = Rect.MinMaxRect(0, m_borderBottom, m_targetSprite.width - m_borderRight, m_targetSprite.height);
				smd.name = m_targetSprite.name + "_TopLeft";
				newData.Add(smd);
			}

			ti.spritesheet = newData.ToArray();
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		}

		[MenuItem("Tools/SpriteSlicer")]
		private static void OpenWindow()
		{
			var window = GetWindow<SpriteSlicerWindow>();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(400, 200);
		}
	}
}
#endif