using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "RegionSettingsAsset", menuName = "MSP2050/RegionSettingsAsset")]
public class RegionSettingsAsset : SerializedScriptableObject
{
	[SerializeField]
	private Dictionary<string, RegionInfo> regionInfo;
	[SerializeField]
	private RegionInfo defaultRegionInfo;

	public RegionInfo GetRegionInfo(string region)
	{
		if (regionInfo.TryGetValue(region, out var result))
			return result;
		return defaultRegionInfo;
	}
}

public class RegionInfo
{
	public string name;
	public string editionPostFix;
	public string letter;
	public Sprite sprite;
	public Color colour;
}
