using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using RoundingEditorUtility;
#endif //Unity_editor

namespace MSP2050.Scripts
{
	[CreateAssetMenu]
	[System.Serializable]
	public class RoundingAssetDatabase : SerializedScriptableObject
	{
		public enum ESliceSection
		{
			Full = 0,
			Left = 1,
			Top = 2,
			Right = 3,
			Bottom = 4,
			TopLeft = 5,
			TopRight = 6,
			BottomRight = 7,
			BottomLeft = 8
		}

		//private int[] m_roundingSizes = {4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 68, 72, 76, 80 };

		[SerializeField] private Dictionary<ESliceSection, List<Sprite>> m_roundingSprites;
		[SerializeField] private List<int[]> m_roundingIndexList; //Per m_roundingSizes entry, contains 8 entries for the index of the sprite m_roundingSprites at {0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2} scale

		public Sprite[] GetSprites(int a_roundingIndex, ESliceSection a_slice)
		{
			Sprite[] result = new Sprite[8];
			for (int i = 0; i < 8; i++)
			{
				result[i] = m_roundingSprites[a_slice][m_roundingIndexList[a_roundingIndex][i]];
			}
			return result;
		}
#if UNITY_EDITOR
		[Button("Generate sprites")]
		public void GenerateSprites()
		{
			int[] roundingSizes = { 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 68, 72, 76, 80 };
			m_roundingSprites = new Dictionary<ESliceSection, List<Sprite>>();
			m_roundingIndexList = new List<int[]>(roundingSizes.Length);
			ESliceSection[] slices = (ESliceSection[]) Enum.GetValues(typeof(ESliceSection));
			string[] sliceNames = Enum.GetNames(typeof(ESliceSection));
			int nextIndex = 0;

			foreach (ESliceSection slice in slices)
			{
				m_roundingSprites.Add(slice, new List<Sprite>(60));
			}

			List<AlphaSettingLayer> layers = new List<AlphaSettingLayer>() {null};

			List<int> generatedSizes = new List<int>(60);
			foreach (int rounding in roundingSizes)
			{
				int[] indices = new int[8];
				int offset = rounding / 4;
				for (int i = 1; i <= 8; i++)
				{
					int newSize = offset * i;
					int existingIndex = generatedSizes.IndexOf(newSize);
					if (existingIndex > -1)
					{
						//Size already exists
						indices[i-1] = existingIndex;
					}
					else
					{
						//Generate sprite for size and add all slices to the dict
						layers[0] = new AlphaCircleLayer()
						{
							m_blendType = EAlphaBlendType.Add,
							m_sectors = ESectors.All,
							m_circleFadeEnd = newSize,
							m_circleFadeStart = newSize - 1
						};
						bool success = RoundingTextureWindow.CreateTexture(newSize, 0f, layers, RoundingEditorUtility.ESliceSection.Corners | RoundingEditorUtility.ESliceSection.Full | RoundingEditorUtility.ESliceSection.Sides, $"Assets/Resources/Rounding/rounded_{newSize}px.png", true);
						if (!success)
						{
							Debug.LogError("Rounding sprite generation aborted");
							return;
						}

						//get resulting sprites and add to dict
						Sprite[] sprites = Resources.LoadAll<Sprite>($"Rounding/rounded_{newSize}px");
						foreach (Sprite sprite in sprites)
						{
							string[] name = sprite.name.Split('_');
							for (int s = 0; s < sliceNames.Length; s++)
							{
								if (name[2] == sliceNames[s])
								{
									m_roundingSprites[(ESliceSection) s].Add(sprite);
									break;
								}
							}
						}

						indices[i - 1] = nextIndex;
						nextIndex++;
						generatedSizes.Add(newSize);
					}
				}
				m_roundingIndexList.Add(indices);
			}
		}
#endif //Unity_editor
	}
}
