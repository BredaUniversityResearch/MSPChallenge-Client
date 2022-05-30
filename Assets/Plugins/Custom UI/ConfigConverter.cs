#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System;
using HEBGraph;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.Utilities;
using Newtonsoft.Json;

namespace ConfigConverter
{
	public class ConfigConverter : OdinEditorWindow
	{
		[SerializeField] string m_inputFilePath;
		[Button("Select input File")]
		public void SelectInput()
		{
			m_inputFilePath = EditorUtility.OpenFilePanel("Select niput file", Application.dataPath, "json");
		}

		[Button("Convert")]
		public void Convert()
		{

			if (!File.Exists(m_inputFilePath))
			{
				Debug.LogError("Invalid input file");
				return;
			}

			DependenciesData inputData;
			using (StreamReader sr = new StreamReader(m_inputFilePath))
			{
				inputData = JsonConvert.DeserializeObject<DependenciesData>(sr.ReadToEnd());
			}

			if (inputData == null) 
			{
				Debug.LogError("Parsing config file failed");
				return;
			}

			HEBGraphData outputData = new HEBGraphData();
			Dictionary<int, int> groupIdToIndex = new Dictionary<int, int>();

			outputData.groups = new HEBGraphDataGroup[inputData.categories.Count];
			for (int i = 0; i < inputData.categories.Count; i++)
			{
				outputData.groups[i] = new HEBGraphDataGroup
				{
					name = inputData.categories[i].name,
					entries = new List<HEBGraphDataEntry>()
				};
				groupIdToIndex.Add(inputData.categories[i].id, i);
			}

			foreach (DependenciesItems item in inputData.items)
			{
				HEBGraphDataEntry entry = new HEBGraphDataEntry
				{
					name = item.name,
					id = item.id
				};
				outputData.groups[groupIdToIndex[item.category]].entries.Add(entry);
			}

			outputData.links = new HEBGraphDataLink[inputData.links.Count];
			int j = 0;
			foreach (DependenciesLinks link in inputData.links)
			{
				HEBGraphDataLink newLink = new HEBGraphDataLink
				{
					fromId = link.fromId,
					toId = link.toId,
				};
				if (link.lines[0].impactId == 1)
				{
					newLink.severity = ((int)link.lines[0].thickness) + 1;
				}
				else
				{
					newLink.severity = (-(int)link.lines[0].thickness) - 1;
				}

				int linkStartIndex = link.lines[0].description.IndexOf('[');
				newLink.description = link.lines[0].description.Substring(0, linkStartIndex);
				outputData.links[j] = newLink;
				j++;
			}

			string outputFile = m_inputFilePath.Substring(0,m_inputFilePath.Length-5) + "_Converted.json";
			using (StreamWriter sw = new StreamWriter(outputFile))
			{
				sw.Write(JsonConvert.SerializeObject(outputData));
			}
			Debug.Log("Conversion completed. Written to: " + outputFile);
		}

		[MenuItem("Tools/ConfigConverter")]
		private static void OpenWindow()
		{
			var window = GetWindow<ConfigConverter>();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(400, 400);
		}
	}
}

	public enum ELowMediumHigh {low, medium, high};

[Serializable]
public class DependenciesData
{
    public List<DependenciesCategories> categories;
    public List<DependenciesItems> items;
    public List<DependenciesLinks> links;
    public List<DependenciesImpactTypes> impactTypes;
}

[Serializable]
public class DependenciesCategories
{
    public int id;
    public string name;
}

[Serializable]
public class DependenciesItems
{
    public int id;
    public int category;
    public string name;
    public string icon;
}

[Serializable]
public class DependenciesLinks
{
    public int fromId;
    public int toId;
    public List<DependenciesLinksLines> lines;
}

[Serializable]
public class DependenciesLinksLines
{
	public int impactId;
    public ELowMediumHigh thickness = ELowMediumHigh.low;
    public string title;
    public string description;
}

[Serializable]
public class DependenciesImpactTypes
{
    public int id;
    public string type;
    public string iconIndicator;
    public int iconAmount;
}
#endif
