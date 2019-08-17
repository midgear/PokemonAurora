using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BuildingCrafter
{
	public class BCJsonExporterImporterMenu
	{
		[MenuItem("CONTEXT/BuildingBlueprint/Export to JSON")]
		public static void ExportToJson(MenuCommand command)
		{
			BuildingBlueprint bp = (BuildingBlueprint)command.context;
			ExportToJson(bp);
		}

		public static void ExportToJson(BuildingBlueprint bp)
		{
			string path = EditorUtility.SaveFilePanel("Save Blueprint to JSON", Application.dataPath, bp.name, "txt");
			BCJsonExporterImporter.ExportJsonFile(bp, path);
		}

		[MenuItem("CONTEXT/BuildingBlueprint/Import from JSON")]
		public static void ImportJson(MenuCommand command)
		{
			int index = -1;
			BuildingBlueprint[] bluePrints = GameObject.FindObjectsOfType<BuildingBlueprint>();
			for(int i = 0; i < bluePrints.Length; i++)
			{
				if(bluePrints[i] == (BuildingBlueprint)command.context)
				{
					index = i;
					break;
				}
			}

			if(index > -1)
				ImportJson(ref bluePrints[index]);
		}

		public static void ImportJson(ref BuildingBlueprint bp)
		{
			if(bp == null)
				return;

			string path = EditorUtility.OpenFilePanel("Load Blueprint from JSON (WILL OVERWRITE CURRENT BLUEPRINT)", Application.dataPath, "txt");
			BuildingBpSerialized newBp = BCJsonExporterImporter.ImportJsonFile(path);

			newBp.WriteToBp(ref bp);
		}
	}
}