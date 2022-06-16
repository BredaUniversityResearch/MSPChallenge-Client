#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;

namespace RoundingEditorUtility
{
	public class RoundingTextureWindow : OdinEditorWindow
	{
		[SerializeField] int m_roundingSize;
		[SerializeField] float m_startAlpha;
		[SerializeField] List<AlphaSettingLayer> m_layers;
		[SerializeField] ESliceSection m_slices;
		[SerializeField] bool m_includeCornersInSideSlices = true;

		[SerializeField] string m_outputFilePath;
		[Button("Select output File")]
		public void SelectInput()
		{
			m_outputFilePath = EditorUtility.SaveFilePanel("Select output file", Application.dataPath, "", "png");
			int index = m_outputFilePath.IndexOf("Assets/");
			if (index == -1)
			{
				m_outputFilePath = "";
				EditorUtility.DisplayDialog("Not in assets", "Chosen folder is not within the project's Assets folder", "Confirm");
				return;
			}
			m_outputFilePath = m_outputFilePath.Substring(index);
		}

		[Button("Generate File")]
		public void Generate()
		{
			CreateTexture(m_roundingSize, m_startAlpha, m_layers, m_slices, m_outputFilePath, m_includeCornersInSideSlices);
		}

		public static bool CreateTexture(int m_roundingSize, float m_startAlpha, List<AlphaSettingLayer> m_layers, ESliceSection m_slices, string m_outputFilePath, bool m_includeCornersInSideSlices)
		{
			if (m_slices == 0)
			{
				EditorUtility.DisplayDialog("No slice selected", "A sprite cannot be selected without any selected slices", "Confirm");
				return false;
			}
			bool overwriting = false;
			if (File.Exists(m_outputFilePath))
			{
				if (EditorUtility.DisplayDialog("Overwrite file", "The specified output file already exists, are you sure you want to overwrite it?", "Overwrite", "Cancel"))
				{
					overwriting = true;
				}
				else
					return false;
			}

			int totalSize = m_roundingSize * 2 + 1;

			//Create alpha array and set base value
			float[,] alphaArray = new float[totalSize, totalSize];
			for (int y = 0; y < totalSize; y++)
			{
				for (int x = 0; x < totalSize; x++)
				{
					alphaArray[x, y] = m_startAlpha;
				}
			}

			//Apply all layers
			foreach (AlphaSettingLayer layer in m_layers)
				layer.SetAlpha(ref alphaArray);

			//Generate texture and set alpha values
			Texture2D texture = new Texture2D(totalSize, totalSize, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
			for (int y = 0; y < totalSize; y++)
			{
				for (int x = 0; x < totalSize; x++)
				{
					texture.SetPixel(x, y, new Color(1f, 1f, 1f, alphaArray[x, y]));
				}
			}

			File.WriteAllBytes(m_outputFilePath, texture.EncodeToPNG());
			AssetDatabase.ImportAsset(m_outputFilePath, ImportAssetOptions.ForceUpdate);
			TextureImporter ti = AssetImporter.GetAtPath(m_outputFilePath) as TextureImporter;
			ti.isReadable = true;
			ti.textureType = TextureImporterType.Sprite;
			ti.filterMode = FilterMode.Point;

			// Bug? Need to convert to single then back to multiple in order to make changes when it's already sliced
			if (overwriting && ti.spriteImportMode == SpriteImportMode.Multiple)
			{
				ti.spriteImportMode = SpriteImportMode.Single;
				AssetDatabase.ImportAsset(m_outputFilePath, ImportAssetOptions.ForceUpdate);
			}
			ti.spriteImportMode = SpriteImportMode.Multiple;
			List<SpriteMetaData> newData = new List<SpriteMetaData>();
			SpriteMetaData smd;

			string fileName = Path.GetFileName(m_outputFilePath);
			fileName = fileName.Remove(fileName.Length - 5);

			if ((m_slices & ESliceSection.Full) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_roundingSize, m_roundingSize, m_roundingSize, m_roundingSize);
				smd.rect = Rect.MinMaxRect(0, 0, totalSize, totalSize);
				smd.name = fileName + "_Full";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Top) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				if (m_includeCornersInSideSlices)
				{
					smd.border = new Vector4(m_roundingSize, 0, m_roundingSize, m_roundingSize);
					smd.rect = Rect.MinMaxRect(0, m_roundingSize, totalSize, totalSize);
				}
				else
				{
					smd.border = new Vector4(0f, 0f, 0f, 1f);
					//smd.border = Vector4.zero;
					smd.rect = Rect.MinMaxRect(m_roundingSize, m_roundingSize, m_roundingSize+1, totalSize);
				}

				smd.name = fileName + "_Top";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Bottom) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				if (m_includeCornersInSideSlices)
				{
					smd.border = new Vector4(m_roundingSize, m_roundingSize, m_roundingSize, 0);
					smd.rect = Rect.MinMaxRect(0, 0, totalSize, totalSize - m_roundingSize);
				}
				else
				{
					smd.border = new Vector4(0f, 1f, 0f, 0f);
					smd.rect = Rect.MinMaxRect(m_roundingSize, 0, m_roundingSize + 1, m_roundingSize+1);
				}
				smd.name = fileName + "_Bottom";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Left) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				if (m_includeCornersInSideSlices)
				{
					smd.border = new Vector4(m_roundingSize, m_roundingSize, 0, m_roundingSize);
					smd.rect = Rect.MinMaxRect(0, 0, totalSize - m_roundingSize, totalSize);
				}
				else
				{
					smd.border = new Vector4(0f, 0f, 1f, 0f);
					//smd.border = Vector4.zero;
					smd.rect = Rect.MinMaxRect(0, m_roundingSize, m_roundingSize + 1, m_roundingSize+1);
				}
				smd.name = fileName + "_Left";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.Right) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				if (m_includeCornersInSideSlices)
				{
					smd.border = new Vector4(0, m_roundingSize, m_roundingSize, m_roundingSize);
					smd.rect = Rect.MinMaxRect(m_roundingSize, 0, totalSize, totalSize);
				}
				else
				{
					smd.border = new Vector4(1f, 0f, 0f, 0f);
					//smd.border = Vector4.zero;
					smd.rect = Rect.MinMaxRect(m_roundingSize, m_roundingSize, totalSize, m_roundingSize+1);
				}
				smd.name = fileName + "_Right";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.TopRight) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(0, 0, m_roundingSize, m_roundingSize);
				smd.rect = Rect.MinMaxRect(m_roundingSize, m_roundingSize, totalSize, totalSize);
				smd.name = fileName + "_TopRight";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.BottomRight) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(0, m_roundingSize, m_roundingSize, 0);
				smd.rect = Rect.MinMaxRect(m_roundingSize, 0, totalSize, totalSize - m_roundingSize);
				smd.name = fileName + "_BottomRight";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.BottomLeft) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_roundingSize, m_roundingSize, 0, 0);
				smd.rect = Rect.MinMaxRect(0, 0, totalSize - m_roundingSize, totalSize - m_roundingSize);
				smd.name = fileName + "_BottomLeft";
				newData.Add(smd);
			}

			if ((m_slices & ESliceSection.TopLeft) != 0)
			{
				smd = new SpriteMetaData();
				smd.alignment = 9;
				smd.pivot = new Vector2(0.5f, 0.5f);
				smd.border = new Vector4(m_roundingSize, 0, 0, m_roundingSize);
				smd.rect = Rect.MinMaxRect(0, m_roundingSize, totalSize - m_roundingSize, totalSize);
				smd.name = fileName + "_TopLeft";
				newData.Add(smd);
			}

			ti.spritesheet = newData.ToArray();
			AssetDatabase.ImportAsset(m_outputFilePath, ImportAssetOptions.ForceUpdate);
			return true;
		}

		[SerializeField, ReadOnly, PreviewField(300f, Sirenix.OdinInspector.ObjectFieldAlignment.Left)] Texture2D m_previewTexture;
		[Button("Generate Preview")]
		public void CreatePreviewTexture()
		{
			int lastIndex = m_roundingSize * 2;
			int totalSize = lastIndex + 1;

			float[,] alphaArray = new float[totalSize, totalSize];
			for (int y = 0; y < totalSize; y++)
			{
				for (int x = 0; x < totalSize; x++)
				{
					alphaArray[x, y] = m_startAlpha;
				}
			}
			foreach (AlphaSettingLayer layer in m_layers)
				layer.SetAlpha(ref alphaArray);

			Texture2D texture = new Texture2D(totalSize, totalSize, UnityEngine.Experimental.Rendering.DefaultFormat.LDR, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
			for (int y = 0; y < totalSize; y++)
			{
				for (int x = 0; x < totalSize; x++)
				{
					float alpha = alphaArray[x, y];
					texture.SetPixel(x, y, new Color(alpha, alpha, alpha, 1f));
				}
			}
			texture.Apply();
			m_previewTexture = texture;
		}

		[MenuItem("Tools/RoundingTextureTool")]
		private static void OpenWindow()
		{
			var window = GetWindow<RoundingTextureWindow>();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(400, 400);
		}
	}

	public enum EAlphaBlendType { Add, Subtract, Multiply }

	[System.Flags]
	public enum ESectors
	{
		All = (1 << 1) + (1 << 2) + (1 << 3) + (1 << 0),
		TopLeft = (1 << 0),
		TopRight = (1 << 1),
		BottomRight = (1 << 2),
		BottomLeft = (1 << 3)
	}

	public abstract class AlphaSettingLayer
	{
		public EAlphaBlendType m_blendType;
		public ESectors m_sectors;

		public abstract void SetAlpha(ref float[,] a_alphaArray);

		protected void SetSectorAlpha(ref float[,] a_alphaArray, int x, int y, float a_alpha)
		{
			int lastIndex = a_alphaArray.GetLength(0) - 1;

			if (m_blendType == EAlphaBlendType.Add)
			{
				if ((m_sectors & ESectors.BottomLeft) != 0)
					a_alphaArray[x, y] += a_alpha;
				if ((m_sectors & ESectors.BottomRight) != 0)
					a_alphaArray[lastIndex - x, y] += a_alpha;
				if ((m_sectors & ESectors.TopRight) != 0)
					a_alphaArray[lastIndex - x, lastIndex - y] += a_alpha;
				if ((m_sectors & ESectors.TopLeft) != 0)
					a_alphaArray[x, lastIndex - y] += a_alpha;
			}
			else if (m_blendType == EAlphaBlendType.Subtract)
			{
				if ((m_sectors & ESectors.BottomLeft) != 0)
					a_alphaArray[x, y] -= a_alpha;
				if ((m_sectors & ESectors.BottomRight) != 0)
					a_alphaArray[lastIndex - x, y] -= a_alpha;
				if ((m_sectors & ESectors.TopRight) != 0)
					a_alphaArray[lastIndex - x, lastIndex - y] -= a_alpha;
				if ((m_sectors & ESectors.TopLeft) != 0)
					a_alphaArray[x, lastIndex - y] -= a_alpha;
			}
			else
			{
				if ((m_sectors & ESectors.BottomLeft) != 0)
					a_alphaArray[x, y] *= a_alpha;
				if ((m_sectors & ESectors.BottomRight) != 0)
					a_alphaArray[lastIndex - x, y] *= a_alpha;
				if ((m_sectors & ESectors.TopRight) != 0)
					a_alphaArray[lastIndex - x, lastIndex - y] *= a_alpha;
				if ((m_sectors & ESectors.TopLeft) != 0)
					a_alphaArray[x, lastIndex - y] *= a_alpha;
			}
		}

		protected void SetCenterLineAlpha(ref float[,] a_alphaArray, int i, float a_alpha)
		{
			int lastIndex = a_alphaArray.GetLength(0) - 1;
			int roundingSize = lastIndex / 2;

			if (m_blendType == EAlphaBlendType.Add)
			{
				a_alphaArray[i, roundingSize] += a_alpha;
				a_alphaArray[lastIndex - i, roundingSize] += a_alpha;
				a_alphaArray[roundingSize, i] += a_alpha;
				a_alphaArray[roundingSize, lastIndex - i] += a_alpha;
			}
			else if (m_blendType == EAlphaBlendType.Subtract)
			{
				a_alphaArray[i, roundingSize] -= a_alpha;
				a_alphaArray[lastIndex - i, roundingSize] -= a_alpha;
				a_alphaArray[roundingSize, i] -= a_alpha;
				a_alphaArray[roundingSize, lastIndex - i] -= a_alpha;
			}
			else
			{
				a_alphaArray[i, roundingSize] *= a_alpha;
				a_alphaArray[lastIndex - i, roundingSize] *= a_alpha;
				a_alphaArray[roundingSize, i] *= a_alpha;
				a_alphaArray[roundingSize, lastIndex - i] *= a_alpha;
			}
		}

		protected void SetCenterAlpha(ref float[,] a_alphaArray, float a_alpha)
		{
			int roundingSize = (a_alphaArray.GetLength(0) - 1) / 2;

			if (m_blendType == EAlphaBlendType.Add)
			{
				a_alphaArray[roundingSize, roundingSize] += a_alpha;
			}
			else if (m_blendType == EAlphaBlendType.Subtract)
			{
				a_alphaArray[roundingSize, roundingSize] -= a_alpha;
			}
			else
			{
				a_alphaArray[roundingSize, roundingSize] *= a_alpha;
			}
		}
	}

	public class AlphaCurveLayer : AlphaSettingLayer
	{
		[SerializeField] AnimationCurve m_fillCurve;
		[SerializeField] bool m_circularDistance;

		public override void SetAlpha(ref float[,] a_alphaArray)
		{
			int roundingSize = (a_alphaArray.GetLength(0) - 1) / 2;
			float roundingSizeF = (float)roundingSize;

			if (m_circularDistance)
			{
				for (int y = 0; y < roundingSize; y++)
				{
					for (int x = 0; x < roundingSize; x++)
					{
						float alpha = m_fillCurve.Evaluate(Mathf.Sqrt((roundingSize - x) * (roundingSize - x) + (roundingSize - y) * (roundingSize - y)) / roundingSizeF);
						SetSectorAlpha(ref a_alphaArray, x, y, alpha);
					}
				}
			}
			else
			{
				for (int y = 0; y < roundingSize; y++)
				{
					for (int x = 0; x < roundingSize; x++)
					{
						float alpha = m_fillCurve.Evaluate(System.Math.Max(roundingSize - x, roundingSize - y) / roundingSizeF);
						SetSectorAlpha(ref a_alphaArray, x, y, alpha);
					}
				}
			}
			for (int i = 0; i < roundingSize; i++)
			{
				float alpha = m_fillCurve.Evaluate((roundingSize - i) / roundingSizeF);
				SetCenterLineAlpha(ref a_alphaArray, i, alpha);
			}

			SetCenterAlpha(ref a_alphaArray, m_fillCurve.Evaluate(0));
		}
	}

	public class AlphaCircleLayer : AlphaSettingLayer
	{
		public float m_circleFadeEnd;
		public float m_circleFadeStart;

		public override void SetAlpha(ref float[,] a_alphaArray)
		{
			int lastIndex = a_alphaArray.GetLength(0) - 1;
			int roundingSize = lastIndex / 2;

			for (int y = 0; y < roundingSize; y++)
			{
				for (int x = 0; x < roundingSize; x++)
				{
					float alpha = GetAlpha(Mathf.Sqrt((roundingSize - x) * (roundingSize - x) + (roundingSize - y) * (roundingSize - y)));
					SetSectorAlpha(ref a_alphaArray, x, y, alpha);
				}
			}
			for (int i = 0; i < roundingSize; i++)
			{
				float alpha = GetAlpha(roundingSize - i);
				SetCenterLineAlpha(ref a_alphaArray, i, alpha);
			}
			SetCenterAlpha(ref a_alphaArray, GetAlpha(0));
		}

		float GetAlpha(float a_distance)
		{
			if (a_distance <= m_circleFadeStart)
				return 1f;
			if (a_distance >= m_circleFadeEnd)
				return 0f;
			return 1f - (a_distance - m_circleFadeStart) / (m_circleFadeEnd - m_circleFadeStart);
		}
	}

	public class AlphaSquareLayer : AlphaSettingLayer
	{
		[SerializeField] float m_squareFadeEnd;
		[SerializeField] float m_squareFadeStart;

		public override void SetAlpha(ref float[,] a_alphaArray)
		{
			int lastIndex = a_alphaArray.GetLength(0) - 1;
			int roundingSize = lastIndex / 2;

			for (int y = 0; y < roundingSize; y++)
			{
				for (int x = 0; x < roundingSize; x++)
				{
					float alpha = GetAlpha(System.Math.Max(roundingSize - x, roundingSize - y));
					SetSectorAlpha(ref a_alphaArray, x, y, alpha);
				}
			}
			for (int i = 0; i < roundingSize; i++)
			{
				float alpha = GetAlpha(roundingSize - i);
				SetCenterLineAlpha(ref a_alphaArray, i, alpha);
			}
			SetCenterAlpha(ref a_alphaArray, GetAlpha(0));
		}

		float GetAlpha(float a_distance)
		{
			if (a_distance <= m_squareFadeStart)
				return 1f;
			if (a_distance >= m_squareFadeEnd)
				return 0f;
			return 1f - (a_distance - m_squareFadeStart) / (m_squareFadeEnd - m_squareFadeStart);
		}
	}
}
#endif
