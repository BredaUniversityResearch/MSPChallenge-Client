using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class LayerTextInfo
{
    public readonly Dictionary<ETextState, string> propertyPerState;
    public readonly Color textColor;
    public readonly ETextSize textSize;
    //public readonly Vector2 deltaPosition;
    public readonly float zoomCutoff;
    public readonly Vector3 textOffset;

	public bool UseInverseScale
	{
		get;
		set;
	}

	public LayerTextInfo(LayerTextInfoObject obj)
    {
        if (obj == null)
            return;

        propertyPerState = obj.property_per_state;
        textColor = Util.HexToColor(obj.text_color);
        textSize = obj.text_size;
        zoomCutoff = obj.zoom_cutoff;
        textOffset = new Vector3(obj.x, obj.y, obj.z);
    }

	public bool TryGetPropertyNameAtState(ETextState state, out string propertyName)
    {
        if (propertyPerState == null)
        {
            propertyName = "";
            return false;
        }

        return propertyPerState.TryGetValue(state, out propertyName);
    }

    public int GetTextSize()
    {
        switch(textSize)
        {
            case ETextSize.XS:
                return 24;
            case ETextSize.S:
                return 34;
            case ETextSize.M:
                return 44;
            case ETextSize.L:
                return 54;
            default:
                return 64;
        }
    }
}

public enum ETextSize
{
    XS = 0, 
    S = 1,
    M = 2,
    L = 3,
    XL = 4
}

public enum ETextState
{
    Current = 0,
    View = 1,
    Edit = 2
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class LayerTextInfoObject
{
    public Dictionary<ETextState, string> property_per_state;
    public string text_color;
    [JsonConverter(typeof(StringEnumConverter))]
    public ETextSize text_size;
    public float zoom_cutoff;
    public float x;
    public float y;
    public float z;
}

