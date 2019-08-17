using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace BuildingCrafter
{	
	public class BCJsonExporterImporter
	{
		public static void ExportJsonFile(BuildingBlueprint buildingBp, string path)
		{
			BuildingBpSerialized bpSerialized = new BuildingBpSerialized(buildingBp);

			string jsonData = JsonUtility.ToJson(bpSerialized, true);

			if(File.Exists(path))
				File.Delete(path);

			File.WriteAllText(path, jsonData);
		}

		public static BuildingBpSerialized ImportJsonFile(string path)
		{
			string jsonFile = File.ReadAllText(path);

			BuildingBpSerialized blueprint = JsonUtility.FromJson<BuildingBpSerialized>(jsonFile);

			return blueprint;
		}
	}
}