using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColourPalette
{
    public interface IColourHolder
    {
        void ColourPaletteChanged();
        void SetColourAsset(ColourAsset a_newAsset);
    }
}
