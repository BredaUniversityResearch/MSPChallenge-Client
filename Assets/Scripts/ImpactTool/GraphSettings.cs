using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace CradleImpactTool
{
	[CreateAssetMenu(fileName = "GraphSettings", menuName = "GraphPlugin/GraphSettings")]
	public class GraphSettings : SerializedScriptableObject
	{
#pragma warning disable UNT0013 // Remove invalid SerializeField attribute
		[SerializeField]
		public Dictionary<string, Sprite> itemIcons;

		[SerializeField]
		public Dictionary<string, Color> lineColors;

		[SerializeField]
		public Dictionary<string, Sprite> lineIndicators;

		[SerializeField]
		public Dictionary<string, int> lineThicknesses;

		[SerializeField]
		public GameObject categoryTextPrefab;

		[SerializeField]
		public GameObject categoryItemPrefab;

		[SerializeField]
		public GameObject linePrefab;

		[SerializeField]
		public GameObject modalPrefab;
#pragma warning restore UNT0013 // Remove invalid SerializeField attribute

		public bool Validate()
		{
			bool isValid = true;

			if (itemIcons != null)
			{
				foreach (var itemIconData in itemIcons)
				{
					if (itemIconData.Key == null)
					{
						Debug.LogError("GraphSettings.itemIcons has an invalid key. Keys cannot be null.");
						isValid = false;
					}

					if (itemIconData.Value == null)
					{
						Debug.LogError("GraphSettings.itemIcons has an invalid value. Values cannot be null.");
						isValid = false;
					}
				}
			}

			if (lineColors != null)
			{
				foreach (var lineColorData in lineColors)
				{
					string name = lineColorData.Key;
					if (lineColorData.Key == null)
					{
						Debug.LogError("GraphSettings.lineColors has an invalid key. Keys cannot be null.");
						name = "<Unkeyed item>"; // Temp name for future errors
						isValid = false;
					}

					if (lineColorData.Value == null)
					{
						Debug.LogError($"GraphSettings.lineColors has an invalid value in \"{name}\". Values cannot be null.");
						isValid = false;
					}
					else if (lineColorData.Value.a < 0.1f)
					{
						Debug.LogWarning($"GraphSettings.lineColors has a color for \"{name}\" that is really hard to see. The alpha channel is set to {lineColorData.Value.a}. It's recommended to set this at least 0.1. Lines might not render as expected.");
					}
				}
			}

			if (lineIndicators != null)
			{
				foreach (var indicatordata in lineIndicators)
				{
					if (indicatordata.Key == null)
					{
						Debug.LogError("GraphSettings.lineIndicators has an invalid key. Keys cannot be null.");
						isValid = false;
					}

					if (indicatordata.Value == null)
					{
						Debug.LogError("GraphSettings.lineIndicators has an invalid value. Values cannot be null.");
						isValid = false;
					}
				}
			}

			if (lineThicknesses != null)
			{
				foreach (var thicknessData in lineThicknesses)
				{
					if (thicknessData.Key == null)
					{
						Debug.LogError("GraphSettings.lineThicknesses has an invalid key. Keys cannot be null.");
						isValid = false;
					}

					if (thicknessData.Value == 0)
					{
						Debug.LogError("GraphSettings.lineThicknesses has an invalid value. Values cannot be 0.");
						isValid = false;
					}
				}
			}

			if (categoryTextPrefab == null)
			{
				Debug.LogError("GraphSettings.categoryTextPrefab cannot be null.");
				isValid = false;
			}

			if (categoryItemPrefab == null)
			{
				Debug.LogError("GraphSettings.categoryItemPrefab cannot be null.");
				isValid = false;
			}

			if (linePrefab == null)
			{
				Debug.LogError("GraphSettings.linePrefab cannot be null.");
				isValid = false;
			}

			if (modalPrefab == null)
			{
				Debug.LogError("GraphSettings.modalPrefab cannot be null.");
				isValid = false;
			}
			else if (modalPrefab.GetComponent<ModalManager>() == null)
			{
				Debug.LogError("GraphSettings.modalPrefab requires to have a ModalManager.");
				isValid = false;
			}

			return isValid;
		}
	}
}
