using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace ColourPalette
{
    public class CustomImage : Image, IColourHolder
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

        void OnColourAssetChanged(Color newColour)
        {
            color = newColour;
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

        //void OnUpdatePaletteChange()
        //{
        //    if (Application.isPlaying)
        //        return;

        //    if (updatePaletteInEditor)
        //    {
        //        if(colourAsset != null)
        //            SubscribeToAssetChange();
        //    }
        //    else
        //    {
        //        if(colourAsset != null)
        //            colourAsset.valueChangedEvent.RemoveListener(OnColourAssetChanged);
        //    }
        //}

        //void OnColourAssetChange()
        //{

        //}
    }
}
