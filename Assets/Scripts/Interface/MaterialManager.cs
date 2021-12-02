using UnityEngine;
using System.Collections.Generic;

public class MaterialManager : MonoBehaviour
{
	private class InnerGlowPatternMaterials
	{
		Texture2D innerGlowTexture;
		Dictionary<Texture2D, PatternMaterials> patternMaterials = new Dictionary<Texture2D, PatternMaterials>();

		public InnerGlowPatternMaterials(Texture2D innerGlowTexture)
		{
			this.innerGlowTexture = innerGlowTexture;
		}

		public Material GetMaterial(Texture2D pattern, Color color)
		{
			if (!patternMaterials.ContainsKey(pattern))
			{
				patternMaterials.Add(pattern, new PatternMaterials(pattern));
			}
			return patternMaterials[pattern].GetMaterial(color, polygonMaterialInnerGlow, true, innerGlowTexture);
		}

		public void UpdateScales()
		{
			foreach (var kvp in patternMaterials)
			{
				kvp.Value.UpdateScales();
			}
		}

		public void UpdateOffsets(Vector4 offset)
		{
			foreach (var kvp in patternMaterials)
			{
				kvp.Value.UpdateOffsets(offset);
			}
		}
	}

	private class PatternMaterials
	{
		Texture2D pattern;
		Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

		public PatternMaterials(Texture2D pattern)
		{
			this.pattern = pattern;
		}

		public Material GetMaterial(Color color, Material materialPrefab, bool innerGlowEnabled, Texture2D innerGlowTexture = null)
		{
			if (!materials.ContainsKey(color))
			{
				Material material = Material.Instantiate<Material>(materialPrefab);
				material.SetTexture("_Pattern", pattern);
				if (innerGlowEnabled) { material.SetTexture("_InnerGlow", innerGlowTexture); }
				material.color = color;
				materials.Add(color, material);

				UpdateScales();
			}

			return materials[color];
		}

		public void UpdateScales()
		{
			Vector2 scale;
			scale.x = Screen.width / (pattern.width * 2f);
			scale.y = Screen.height / (pattern.height * 2f);

			foreach (var kvp in materials)
			{
				kvp.Value.SetVector("_Scale", scale);
			}
		}

		public void UpdateOffsets(Vector4 offset)
		{
			foreach (var kvp in materials)
			{
				kvp.Value.SetVector("_Offset", offset);
			}
		}
	}

	private static Camera cam;
    private static Vector3 previousCamPosition;
    private static int previousScreenWidth;
    private static int previousScreenHeight;

    private static Vector3 patternAnchor = Vector3.zero;
    private static Vector3 patternAnchorOffset = Vector3.zero;

    private static Material polygonMaterialDefault;
    private static Material polygonMaterialInnerGlow;

    private static Dictionary<PolygonLayer, Texture2D> innerGlowTextures = new Dictionary<PolygonLayer, Texture2D>();
    private static Dictionary<PolygonLayer, Rect> innerGlowTextureBounds = new Dictionary<PolygonLayer, Rect>();

    private static Dictionary<Texture2D, PatternMaterials> patternMaterials = new Dictionary<Texture2D, PatternMaterials>(); // pattern texture is used as the key
    private static Dictionary<Texture2D, InnerGlowPatternMaterials> patternMaterialsWithInnerGlow = new Dictionary<Texture2D, InnerGlowPatternMaterials>(); // inner glow texture is used as the key

    private static Dictionary<string, Texture2D> patterns = new Dictionary<string, Texture2D>(32);
	private static /*readonly*/ Texture2D defaultPattern = null;

    void Awake()
    {
        cam = Camera.main;
        polygonMaterialDefault = Resources.Load<Material>("Polygon Material");
        polygonMaterialInnerGlow = Resources.Load<Material>("Polygon Material Inner Glow");

        Texture2D[] patternAssets = Resources.LoadAll<Texture2D>("patterns/");
        foreach(Texture2D pattern in patternAssets)
        {
			patterns[pattern.name] = pattern;
        }

		if (!patterns.TryGetValue("Default", out defaultPattern))
		{
			Debug.LogError("Could not find pattern with name \"Default\" for fallback pattern. Please provide a pattern with name \"Default\" in the \"patterns\" folder");
		}
	}

	private static void UpdateMaterialScales()
    {
        foreach (var kvp in patternMaterials)
        {
            kvp.Value.UpdateScales();
        }

        foreach (var kvp in patternMaterialsWithInnerGlow)
        {
            kvp.Value.UpdateScales();
        }
    }

	private static void UpdateMaterialOffsets(Vector4 offset)
    {
        foreach (var kvp in patternMaterials)
        {
            kvp.Value.UpdateOffsets(offset);
        }

        foreach (var kvp in patternMaterialsWithInnerGlow)
        {
            kvp.Value.UpdateOffsets(offset);
        }
    }

	public static Texture2D GetPatternOrDefault(string patternName)
	{
		Texture2D result;
		if (patternName == null || !patterns.TryGetValue(patternName, out result))
		{
			Debug.LogWarning("Could not find pattern with name \"" + (patternName ?? "NULL") + "\". Defaulting the the default pattern provided");
			result = defaultPattern;
		}
		return result;
	}

	public static Material GetDefaultPolygonMaterial(string patternName, Color color)
    {
        Texture2D pattern = GetPatternOrDefault(patternName);

        if (!patternMaterials.ContainsKey(pattern))
        {
            patternMaterials.Add(pattern, new PatternMaterials(pattern));
        }
        return patternMaterials[pattern].GetMaterial(color, polygonMaterialDefault, false);
    }

    public static Material GetInnerGlowPolygonMaterial(Texture2D innerGlowTexture, string patternName, Color color)
    {
		Texture2D pattern = GetPatternOrDefault(patternName);

		if (!patternMaterialsWithInnerGlow.ContainsKey(innerGlowTexture))
        {
            patternMaterialsWithInnerGlow.Add(innerGlowTexture, new InnerGlowPatternMaterials(innerGlowTexture));
        }
        return patternMaterialsWithInnerGlow[innerGlowTexture].GetMaterial(pattern, color);
    }

    public static void Update()
    {
        if (cam.transform.position != previousCamPosition)
        {
            Vector3 offset = -2 * cam.WorldToViewportPoint(patternAnchor) + patternAnchorOffset;
            if (CameraZoom.LastZoomLocation != patternAnchor)
            {
                patternAnchor = CameraZoom.LastZoomLocation;
                patternAnchorOffset = offset - -2 * cam.WorldToViewportPoint(patternAnchor);
            }

            UpdateMaterialOffsets(offset);

            previousCamPosition = cam.transform.position;
        }

        if (Screen.width != previousScreenWidth || Screen.height != previousScreenHeight)
        {
            UpdateMaterialScales();

            previousScreenWidth = Screen.width;
            previousScreenHeight = Screen.height;
        }
    }

    public static Texture2D GetInnerGlowTexture(PolygonLayer layer, int radius, int iterations, float multiplier, float pixelSize)
    {
        if (!innerGlowTextures.ContainsKey(layer))
        {
            CalculateInnerGlowTextureData(layer, radius, iterations, multiplier, pixelSize);
        }
        return innerGlowTextures[layer];
    }

    public static Rect GetInnerGlowTextureBounds(PolygonLayer layer, int radius, int iterations, float multiplier, float pixelSize)
    {
        if (!innerGlowTextureBounds.ContainsKey(layer))
        {
            CalculateInnerGlowTextureData(layer, radius, iterations, multiplier, pixelSize);
        }
        return innerGlowTextureBounds[layer];
    }

    public static void CalculateInnerGlowTextureData(PolygonLayer layer, int radius, int iterations, float multiplier, float pixelSize)
    {
        //float pixelSize = VisualizationUtil.VisualizationSettings.InnerGlowPixelSize;
        Rect textureBounds = layer.GetLayerBounds();
        int w = Mathf.CeilToInt(textureBounds.size.x / pixelSize);
        int h = Mathf.CeilToInt(textureBounds.size.y / pixelSize);

        // set width and height to the smallest power of two that is larger than their current values
        w = Mathf.RoundToInt(Mathf.Pow(2, Mathf.Ceil(Mathf.Log(w, 2))));
        h = Mathf.RoundToInt(Mathf.Pow(2, Mathf.Ceil(Mathf.Log(h, 2))));

        textureBounds.size = new Vector2(w * pixelSize, h * pixelSize);

        Texture2D innerGlowTexture = new Texture2D(w, h, TextureFormat.Alpha8, false);
        int[,] raster = new int[w, h];

        int entityCount = layer.GetEntityCount();
        for (int i = 0; i < entityCount; ++i)
        {
            ((PolygonEntity)layer.GetEntity(i)).Rasterize(1, raster, textureBounds);
        }

        byte[] pixelBuffer = new byte[w * h];

        for (int y = 0; y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
            {
                pixelBuffer[y * w + x] = raster[x, y] == 1 ? (byte)255 : (byte)0;
            }
        }

        innerGlowTexture.LoadRawTextureData(pixelBuffer);

        innerGlowTexture = BlurUtil.FastBlur(innerGlowTexture, radius, iterations);
        MultiplyAlpha(innerGlowTexture, multiplier);

        innerGlowTexture.Apply();

        innerGlowTextures[layer] = innerGlowTexture;
        innerGlowTextureBounds[layer] = textureBounds;
    }

    private static void MultiplyAlpha(Texture2D texture, float factor)
    {
        byte[] pixelBuffer = texture.GetRawTextureData();
        int pixelCount = pixelBuffer.Length;
        for (int i = 0; i < pixelCount; ++i)
        {
            pixelBuffer[i] = (byte)Mathf.Min(255, (pixelBuffer[i] * factor));
        }
        texture.LoadRawTextureData(pixelBuffer);
        texture.Apply();
    }
}
