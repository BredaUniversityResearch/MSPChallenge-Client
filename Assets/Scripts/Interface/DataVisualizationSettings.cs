using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DrawModeSettings
{
    public string DrawModeName;
    public Color Color;
    public bool DrawPolygon;
    public bool DrawLines;
	public bool DrawPoints;

    public DrawModeSettings(string drawModeName)
    {
        DrawModeName = drawModeName;
        Color = Color.white;
        DrawPolygon = true;
        DrawLines = true;
		DrawPoints = true;
    }

    public SubEntityDrawSettings GetSubEntityDrawSettings(SubEntityDrawSettings defaultDrawSettings)
    {
        return new SubEntityDrawSettings(DrawPolygon, Color, defaultDrawSettings.PolygonPatternName, 
                                         false, defaultDrawSettings.InnerGlowRadius, defaultDrawSettings.InnerGlowIterations, defaultDrawSettings.InnerGlowMultiplier, defaultDrawSettings.InnerGlowPixelSize,
										 DrawLines, Color, defaultDrawSettings.LineWidth, defaultDrawSettings.LineIcon, defaultDrawSettings.LineIconColor, defaultDrawSettings.LineIconCount, defaultDrawSettings.LinePatternType,
										 DrawPoints, Color, defaultDrawSettings.PointSize, defaultDrawSettings.PointSprite,
                                         DrawModeName != SubEntityDrawMode.BeingCreated.ToString() && DrawModeName != SubEntityDrawMode.BeingCreatedInvalid.ToString());
    }
}

[System.Serializable]
public class LODSettings
{
    public float MinScale;
    public float SimplificationTolerance;
    public float MinPolygonArea;

    public LODSettings(float minScale, float simplificationTolerance, float minPolygonArea)
    {
        MinScale = minScale;
        SimplificationTolerance = simplificationTolerance;
        MinPolygonArea = minPolygonArea;
    }
}

[CreateAssetMenu]
[System.Serializable]
public class DataVisualizationSettings : ScriptableObject
{
    public float InnerGlowPixelSize = 2f;

    public List<LODSettings> LODs = new List<LODSettings>();

    [SerializeField]
    public List<DrawModeSettings> DrawModeSettings;

	[SerializeField]
	private ValueConversionCollection valueConversions = null;

	public ValueConversionCollection ValueConversions
	{
		get 
		{
			return valueConversions;
		}
	}

	public bool ContainsDrawMode(string drawModeName)
    {
        foreach (DrawModeSettings settings in DrawModeSettings)
        {
            if (settings.DrawModeName == drawModeName) { return true; }
        }
        return false;
    }

    public DrawModeSettings GetDrawModeSettings(SubEntityDrawMode drawMode)
    {
        foreach (DrawModeSettings settings in DrawModeSettings)
        {
            if (settings.DrawModeName == drawMode.ToString()) { return settings; }
        }
        return null;
    }
}
