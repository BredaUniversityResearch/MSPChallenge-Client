using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace ColourPalette
{
    [System.Serializable]
    public class ColourSet
    {
        [HideLabel]
        public string name;

        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, NumberOfItemsPerPage = 30)]
        public NamedColour[] colours;

        [SerializeField, HideInInspector]
        ColourPalette palette;

        public ColourSet(int size, ColourPalette palette)
        {
            colours = new NamedColour[size];
            this.palette = palette;
        }

        public void UpdateSize(int newSize)
        {
            if (colours.Length != newSize)
            {
                NamedColour[] newColours = new NamedColour[newSize];
                for (int i = 0; i < colours.Length && i < newSize; i++)
                    newColours[i] = colours[i];
                colours = newColours;
            }
        }

        [Button(Name = "Activate Set")]
        public void ActivateSet()
        {
            if (palette != null)
            {
                palette.ActivateColorSet(this);
            }
        }

        [Button(Name = "Activate Set And Force Colour")]
        public void ForceActivateSet()
        {
            if (palette != null)
            {
                palette.ForceActivateColorSet(this);
            }
        }

        public void ActivateSet(List<ColourAsset> colourAssets)
        {
            if (colourAssets == null)
                return;

            for (int i = 0; i < colourAssets.Count && i < colours.Length; i++)
                colourAssets[i].SetValue(colours[i].colour);
        }

        public void ForceActivateSet(List<ColourAsset> colourAssets)
        {
            if (colourAssets == null)
                return;
            ActivateSet(colourAssets);

            foreach (CustomImage colourHolder in GameObject.FindObjectsOfType<CustomImage>())
                colourHolder.ColourPaletteChanged();
            foreach (CustomText colourHolder in GameObject.FindObjectsOfType<CustomText>())
                colourHolder.ColourPaletteChanged();
        }
    }

    [System.Serializable]
    public struct NamedColour
    {
        [HideLabel]
        public string name;
        [HideLabel]
        public Color colour;
        public NamedColour(string name, Color colour)
        {
            this.name = name;
            this.colour = colour;
        }
    }
}
