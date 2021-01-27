using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColourPalette
{
    public class CustomText : Text, IColourHolder
    {
        [SerializeField]
        ColourAsset colourAsset;

        public ColourAsset ColourAsset
        {
            get { return colourAsset; }
            set
            {
                UnSubscribeFromAssetChange();
                colourAsset = value;
                SubscribeToAssetChange();
            }
        }

        protected override void Start()
        {
            base.Start();
            if (colourAsset != null)
                color = colourAsset.GetColour();
            SubscribeToAssetChange();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnSubscribeFromAssetChange();
        }

        void OnColourAssetChanged(Color newColour)
        {
            color = newColour;
        }

        void SubscribeToAssetChange()
        {
            if (colourAsset != null && Application.isPlaying)
            {
                colourAsset.valueChangedEvent.AddListener(OnColourAssetChanged);
                OnColourAssetChanged(colourAsset.GetColour());
            }
        }

        void UnSubscribeFromAssetChange()
        {
            if (colourAsset != null && Application.isPlaying)
            {
                colourAsset.valueChangedEvent.RemoveListener(OnColourAssetChanged);
            }
        }

        public void ColourPaletteChanged()
        {
            if (colourAsset != null)
                color = colourAsset.GetColour();
        }

        public void SetColourAsset(ColourAsset a_newAsset)
        {
            ColourAsset = a_newAsset;
        }
    }
}
