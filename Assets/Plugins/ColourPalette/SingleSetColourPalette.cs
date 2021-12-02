using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace ColourPalette
{
    [CreateAssetMenu(fileName = "NewColourPalette", menuName = "ColourPalette/SingleSetColourPalette", order = 101)]
    public class SingleSetColourPalette : SerializedScriptableObject
    {
        [SerializeField]
        Dictionary<ColourAsset, Color> colourAssets;
        
        [Button("Activate")]
        public void ActivateColorSet()
        {
            if (colourAssets == null)
                return;
            foreach(var kvp in colourAssets)
                kvp.Key?.SetValue(kvp.Value);
        }

		[Button("Activate in scene")]
		public void ForceActivateSet()
		{
			if (colourAssets == null)
				return;
			foreach (var kvp in colourAssets)
				kvp.Key?.SetValue(kvp.Value);

			foreach (CustomImage colourHolder in GameObject.FindObjectsOfType<CustomImage>())
				colourHolder.ColourPaletteChanged();
			foreach (CustomText colourHolder in GameObject.FindObjectsOfType<CustomText>())
				colourHolder.ColourPaletteChanged();
		}
	}
}
