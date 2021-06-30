using System;
using UnityEngine;
using UnityEngine.Events;

namespace ColourPalette
{
    public interface IColourContainer
    {
        Color GetColour();
        void SubscribeToChanges(UnityAction<Color> a_callback);
        void UnSubscribeFromChanges(UnityAction<Color> a_callback);
    }
}
