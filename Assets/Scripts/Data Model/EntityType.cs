using UnityEngine;
using System.Collections.Generic;
using System;

public class EntityType
{
	public string Name { get; set; }

	public SubEntityDrawSettings DrawSettings { get; set; }

	public long capacity = 1; //Per km2 for areas
	public float investmentCost = 1f;
	public string description = "temporary text";
	public int availabilityDate = -10;
	public int value = 0; // In EEZLayers these are country ID's
	public string media;
	public EApprovalType requiredApproval = EApprovalType.NotDependent;

	//public Dictionary<EnergyKPI.PressureType, float> pressureValues = new Dictionary<EnergyKPI.PressureType, float>(){
	//    { EnergyKPI.PressureType.ArtificialSubstrate, 1f },
	//    { EnergyKPI.PressureType.BottomDisturbance, 1f },
	//    { EnergyKPI.PressureType.Noise, 1f },
	//    { EnergyKPI.PressureType.SurfaceDisturbance, 1f }};

	public EntityType()
	{
		Name = "entity type";
		DrawSettings = new SubEntityDrawSettings();
	}

	public EntityType(string name, string description, long capacity, float investmentCost, int availability, SubEntityDrawSettings drawSettings, string media, int value = 0, EApprovalType requiredApproval = EApprovalType.NotDependent)
	{
		Name = name;
		if (description != null)
			this.description = description.Replace("|", Environment.NewLine);
		this.capacity = capacity;
		this.investmentCost = investmentCost;
		this.availabilityDate = availability;
		this.value = value;
		this.media = media;
		this.requiredApproval = requiredApproval;
		DrawSettings = drawSettings;
	}

	public EntityType GetClone()
	{
		return GetClone(Name);
	}

	public EntityType GetClone(string newName)
	{
		return new EntityType(newName, description, capacity, investmentCost, availabilityDate, DrawSettings.GetClone(), media, value, requiredApproval);
	}

	public void SetColors(Color polyColor, Color lineColor, Color pointColor)
	{
		DrawSettings.PolygonColor = polyColor;
		DrawSettings.LineColor = lineColor;
		DrawSettings.PointColor = pointColor;
	}

	public void CopyEntityTypeValues(EntityTypeValues type)
	{
		type.displayName = Name;
		type.value = value;

		type.description = description;
		type.capacity = capacity;
		type.investmentCost = investmentCost;
		type.availability = availabilityDate;
		type.value = value;
		type.approval = requiredApproval;
	}
}

public class SubEntityDrawSettings
{
	public bool DisplayPolygon { get; set; }
	public Color PolygonColor { get; set; }
	public string PolygonPatternName { get; set; }

	public bool InnerGlowEnabled { get; set; }
	public int InnerGlowRadius { get; set; }
	public int InnerGlowIterations { get; set; }
	public float InnerGlowMultiplier { get; set; }
	public float InnerGlowPixelSize { get; set; }

	public bool DisplayLines { get; set; }
	public Color LineColor { get; set; }
	public float LineWidth { get; set; }
	public string LineIcon { get; set; }
	public Color LineIconColor { get; set; }
	public int LineIconCount { get; set; } //Desired number of line icons we want evenly spaced out. -1 for one on each segment.
	public bool FixedWidth { get; set; } //The LineWidth is specified in real world KM and shouldn't scale
	public ELinePatternType LinePatternType { get; private set; }

	public bool DisplayPoints { get; set; }
	public Color PointColor { get; set; }
	public float PointSize { get; set; }
	public Sprite PointSprite { get; set; }

	//internal variable only used for special cases: no need to show it in an interface
	public bool DrawLineFromEndToStart { get; set; }


	public SubEntityDrawSettings()
	{
		DisplayPolygon = true;
		PolygonColor = Color.magenta;
		PolygonPatternName = "Default";

		InnerGlowEnabled = false;
		InnerGlowRadius = 0;
		InnerGlowIterations = 0;
		InnerGlowMultiplier = 0;
		InnerGlowPixelSize = 0;

		DisplayLines = true;
		LineColor = Color.yellow;
		LineWidth = 1.0f;
		LineIcon = null;
		LineIconColor = Color.white;
		LineIconCount = -1;

		DisplayPoints = true;
		PointColor = Color.magenta;
		PointSize = 1.0f;
		PointSprite = null;

		DrawLineFromEndToStart = true;
	}

	public SubEntityDrawSettings(bool displayPolygon, Color polygonColor, string polygonPatternName,
								 bool innerGlowEnabled, int innerGlowRadius, int innerGlowIterations, float innerGlowMultiplier, float innerGlowPixelSize,
								 bool displayLines, Color lineColor, float lineWidth, string lineIcon, Color lineIconColor, int lineIconCount, ELinePatternType linePatternType,
								 bool displayPoints, Color pointColor, float pointSize, Sprite pointSprite,
								 bool drawLineFromEndToStart = true)
	{
		DisplayPolygon = displayPolygon;
		PolygonColor = polygonColor;
		PolygonPatternName = polygonPatternName;

		InnerGlowEnabled = innerGlowEnabled;
		InnerGlowRadius = innerGlowRadius;
		InnerGlowIterations = innerGlowIterations;
		InnerGlowMultiplier = innerGlowMultiplier;
		InnerGlowPixelSize = innerGlowPixelSize;

		DisplayLines = displayLines;
		LineColor = lineColor;
		LineWidth = lineWidth;
		LineIcon = lineIcon;
		LineIconColor = lineIconColor;
		LineIconCount = -1;
        LinePatternType = linePatternType;

        DisplayPoints = displayPoints;
		PointColor = pointColor;
		PointSize = pointSize;
		PointSprite = pointSprite;

		DrawLineFromEndToStart = drawLineFromEndToStart;
	}

	public SubEntityDrawSettings GetClone()
	{
		return new SubEntityDrawSettings(DisplayPolygon, PolygonColor, PolygonPatternName, InnerGlowEnabled, InnerGlowRadius, InnerGlowIterations, InnerGlowMultiplier, InnerGlowPixelSize, 
			DisplayLines, LineColor, LineWidth, LineIcon, LineIconColor, LineIconCount, LinePatternType,
			DisplayPoints, PointColor, PointSize, PointSprite);
	}

	public bool Equals(SubEntityDrawSettings other)
	{
		if (DisplayPolygon != other.DisplayPolygon ||
			PolygonColor != other.PolygonColor ||
			PolygonPatternName != other.PolygonPatternName ||

			InnerGlowEnabled != other.InnerGlowEnabled ||
			InnerGlowRadius != other.InnerGlowRadius ||
			InnerGlowIterations != other.InnerGlowIterations ||
			InnerGlowMultiplier != other.InnerGlowMultiplier ||

			DisplayLines != other.DisplayLines ||
			LineColor != other.LineColor ||
			LineWidth != other.LineWidth ||
			LineIcon != other.LineIcon ||
			LineIconColor != other.LineIconColor ||
			LineIconCount != other.LineIconCount ||

			DisplayPoints != other.DisplayPoints ||
			PointColor != other.PointColor ||
			PointSize != other.PointSize ||
			PointSprite != other.PointSprite)
		{
			return false;
		}

		return true;
	}

	public EntityTypeValues ToEntityTypeValues()
	{
		EntityTypeValues result = new EntityTypeValues();
		result.displayPolygon = DisplayPolygon;
		result.polygonColor = "#" + Util.ColorToHex(PolygonColor);
		result.polygonPatternName = PolygonPatternName;

		result.innerGlowEnabled = InnerGlowEnabled;
		result.innerGlowRadius = InnerGlowRadius;
		result.innerGlowIterations = InnerGlowIterations;
		result.innerGlowMultiplier = InnerGlowMultiplier;

		result.displayLines = DisplayLines;
		result.lineColor = "#" + Util.ColorToHex(LineColor);
		result.lineWidth = LineWidth;
		result.lineIcon = LineIcon;
		//LineIconColor PdG 2019-01-31: This does not seem relevant at this time since it is a runtime setting.
		//LineIconCount

		result.displayPoints = DisplayPoints;
		result.pointColor = "#" + Util.ColorToHex(PointColor);
		result.pointSize = PointSize;
		result.pointSpriteName = PointSprite.name;
		return result;
	}
}
