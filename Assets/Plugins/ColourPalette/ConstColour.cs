using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace ColourPalette
{
    [Serializable]
    public class ConstColour : IColourContainer
    {
        [SerializeField, HideLabel] Color m_colour;

        public ConstColour(Color a_color)
        {
            m_colour = a_color;
        }

        public Color GetColour()
        {
            return m_colour;
        }

        public void SubscribeToChanges(UnityAction<Color> a_callback)
        {
        }

        public void UnSubscribeFromChanges(UnityAction<Color> a_callback)
        {
        }
    }
}
