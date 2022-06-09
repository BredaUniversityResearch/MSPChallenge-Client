using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using RoundingEditorUtility;

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

		private int[] m_roundingSizes = { 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 68, 72, 76, 80 };

		private Dictionary<ESliceSection, List<Sprite>> m_roundingSprites;
		private List<int[]> m_roundingIndexList; //Per m_roundingSizes entry, contains 5 entries for the index of the sprite m_roundingSprites at {1, 1.25, 1.5, 1.75, 2} scale

		void UpdateSprites()
		{
			string prefix = "rounding_";

		}

		void GenerateSprites()
		{
			ESliceSection[] slices = (ESliceSection[])Enum.GetValues(typeof(ESliceSection));
			m_roundingSprites = new Dictionary<ESliceSection, List<Sprite>>();
			foreach (ESliceSection slice in slices)
			{
				m_roundingSprites.Add(slice, new List<Sprite>(60));
			}

			List<AlphaSettingLayer> layers = new List<AlphaSettingLayer>() {null};

			List<int> generatedSizes = new List<int>(60);
			foreach (int rounding in m_roundingSizes)
			{
				int[] indices = new int[5];
				int offset = rounding / 4;
				for (int i = 0; i < 5; i++)
				{
					int newSize = rounding + offset * i;
					int existingIndex = generatedSizes.IndexOf(newSize);
					if (existingIndex > -1)
					{
						//Size already exists
						indices[i] = existingIndex;
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
						//TODO: get resulting sprites and add to dict
						RoundingTextureWindow.CreateTexture(newSize, 0f, layers, RoundingEditorUtility.ESliceSection.Corners & RoundingEditorUtility.ESliceSection.Full & RoundingEditorUtility.ESliceSection.Sides, $"rounded_{newSize}px.png");

						generatedSizes.Add(newSize);
					}
				}
			}
			
		}
	}
}
