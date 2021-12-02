using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CradleImpactTool
{
	public class SaveFile
	{
		static readonly string fileName = "cradle_impact_save.json";

		public Dictionary<string, ImpactSave> impactSaves = new Dictionary<string, ImpactSave>();

		public bool Validate()
		{
			bool isValid = true;

			if (impactSaves == null)
			{
				Debug.LogError($"SaveFile cannot have an impactSaves dictionary that is null.");
				isValid = false;
			}

			return isValid;
		}

		public static SaveFile Load()
		{
			// note MH: disabled loading from file for now.

			// if (File.Exists(fileName))
			// {
			// 	string text = File.ReadAllText(fileName);
			// 	SaveFile file = JsonConvert.DeserializeObject<SaveFile>(text);
			// 	if (file != null)
			// 	{
			// 		file.Validate();
			// 		return file;
			// 	}
			// }

			return new SaveFile();
		}

		public void Save()
		{
			// note MH: disabled saving to file for now.

			// string text = JsonConvert.SerializeObject(this);
			// File.WriteAllText(fileName, text);
		}
	}
}
