using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingCrafter
{

	public class BuildingCrafterMenus : Editor 
	{
		[MenuItem("GameObject/Create Building Crafter GameObject", false, 20)]
		[MenuItem("Assets/BuildingCrafter/Create Building Crafter GameObject", false, 60)]
		public static void CreateBuildingCrafter()
		{
			BuildingCrafterGenerator buildingCrafter = BCEditUtils.GetBuildingCrafter();

			Selection.activeGameObject = buildingCrafter.gameObject;
		}

		[MenuItem("Assets/BuildingCrafter/Export Building Crafter Script")]
		public static void ExportBuildingCrafterScripts()
		{
			EditorUtility.DisplayProgressBar("Exporting Building Crafter Scripts", "This will take a while", 0f);

			List<string> allAssetPaths = new List<string>();

			{ // Adds all Building Crafter Stuff
				string[] searchFolder = new string[1] { "Assets/Packages/BuildingCrafter"};
				string[] dataModelGuids = AssetDatabase.FindAssets("", searchFolder);
				
				for(int i = 0; i < dataModelGuids.Length; i++)
					allAssetPaths.Add(AssetDatabase.GUIDToAssetPath(dataModelGuids[i]));
			}

			{ // Adds all Building Crafter Assets
				string[] searchFolder = new string[1] { "Assets/Packages/BuildingCrafterAssets"};
				string[] dataModelGuids = AssetDatabase.FindAssets("", searchFolder);
				
				for(int i = 0; i < dataModelGuids.Length; i++)
				{
					EditorUtility.DisplayProgressBar("Exporting Building Crafter Scripts", "This will take a while", (float)i / (float)dataModelGuids.Length);
					allAssetPaths.Add(AssetDatabase.GUIDToAssetPath(dataModelGuids[i]));
				}
					
			}

			EditorUtility.DisplayProgressBar("Exporting Building Crafter Scripts", "This will take a while", 0.75f);
			AssetDatabase.ExportPackage(allAssetPaths.ToArray<string>(), "Assets/BuildingCrafter.unitypackage");
			AssetDatabase.Refresh();

			EditorUtility.ClearProgressBar();
		}

		[MenuItem("Assets/BuildingCrafter/Regenerate All Buildings")]
		public static void RegenerateAllBuildings()
		{
			BuildingBlueprint[] allBlueprints = GameObject.FindObjectsOfType<BuildingBlueprint>();

			int totalBuildings = allBlueprints.Length;

			// TODO - Add undo to this

			if(EditorUtility.DisplayDialog("Warning", "You are about to generate " + totalBuildings + " and this may take a while. There is no Undo. Are you sure?", "Continue", "Cancel"))
			{
				EditorUtility.DisplayCancelableProgressBar("Generate All Buildings", "Generating Buildings", 0);
				                                           
	            for(int i = 0; i < allBlueprints.Length; i++)
	            {
					if(EditorUtility.DisplayCancelableProgressBar("Generate All Buildings", "Generating Buildings", (float)i / (float)totalBuildings))
					{
						EditorUtility.ClearProgressBar();
						return;
					}
					BCGenerator.GenerateFullBuilding(allBlueprints[i]);
				}
				
				EditorUtility.ClearProgressBar();
			}
		}

		[MenuItem("Assets/BuildingCrafter/Versioning/Flip Building Fancy Faces")]
		public static void TurnOnAtlases()
		{
			BuildingBlueprint[] allBlueprints = GameObject.FindObjectsOfType<BuildingBlueprint>();
			
			int totalBuildings = allBlueprints.Length;
			
			if(EditorUtility.DisplayDialog("Warning", "You are about to flip the faces " + totalBuildings + " and this may take a while. There is no Undo. Are you sure?", "Continue", "Cancel"))
			{
				EditorUtility.DisplayCancelableProgressBar("Flipping Faces", "Looking good doing it too", 0);
				
				for(int i = 0; i < allBlueprints.Length; i++)
				{
					BuildingBlueprint buildingBp = allBlueprints[i];

					bool oldFront = buildingBp.FancyFront;
					bool oldBack = buildingBp.FancyBack;
					bool oldLeft = buildingBp.FancyLeftSide;
					bool oldRight = buildingBp.FancyRightSide;

					allBlueprints[i].FancyFront = oldBack;
					allBlueprints[i].FancyBack = oldFront;
					allBlueprints[i].FancyLeftSide = oldRight;
					allBlueprints[i].FancyRightSide = oldLeft;

					if(EditorUtility.DisplayCancelableProgressBar("Flipping Faces", "Looking good doing it too", (float)i / (float)totalBuildings))
					{
						EditorUtility.ClearProgressBar();
						return;
					}
				}
				
				EditorUtility.ClearProgressBar();
			}
		}

		[MenuItem("Assets/BuildingCrafter/Versioning/Update All Buildings To Use Atlases")]
		public static void TurnOffAtlases()
		{
			BuildingBlueprint[] allBlueprints = GameObject.FindObjectsOfType<BuildingBlueprint>();

			int totalBuildings = allBlueprints.Length;

			string building = "building";
			if(totalBuildings > 1)
				building += "s";

			if(EditorUtility.DisplayDialog("Warning", "You are above to set " + totalBuildings + " " + building + " to use Atlases. There is no Undo. Are you sure?", "Continue", "Cancel"))
			{
				for(int i = 0; i < allBlueprints.Length; i++)
				{
					BuildingBlueprint buildingBp = allBlueprints[i];
					buildingBp.UseAtlases = true;
				}
			}
		}

		[MenuItem("Assets/BuildingCrafter/Versioning/Remove All Buildings using Atlases")]
		public static void FlipBuildingFancyFaces()
		{
			BuildingBlueprint[] allBlueprints = GameObject.FindObjectsOfType<BuildingBlueprint>();

			int totalBuildings = allBlueprints.Length;

			string building = "building";
			if(totalBuildings > 1)
				building += "s";

			if(EditorUtility.DisplayDialog("Warning", "You are turn off Atalasing for " + totalBuildings + " " + building + ". There is no Undo. Are you sure?", "Continue", "Cancel"))
			{
				for(int i = 0; i < allBlueprints.Length; i++)
				{
					BuildingBlueprint buildingBp = allBlueprints[i];
					buildingBp.UseAtlases = false;
				}
			}
		}

		[MenuItem("Assets/BuildingCrafter/Versioning/Update Windows and Doors")]
		public static void UpdateWindowsAndDoorsTo0p8Flags()
		{
			BuildingBlueprint[] buildingBps = GameObject.FindObjectsOfType<BuildingBlueprint>();

			string doorBuildings = "";
			string windowBuildings = "";

			for(int i = 0; i < buildingBps.Length; i++)
			{
				if(BCValidator.DoorsHaveBeenUpdate(buildingBps[i]))
					doorBuildings += buildingBps[i].gameObject.name + ", ";

				if(BCValidator.WindowsHaveBeenUpdate(buildingBps[i]))
					windowBuildings += buildingBps[i].gameObject.name + ", ";
			}

			bool updateBuildings = true;

			if(doorBuildings.Length > 1 || windowBuildings.Length > 1)
			{
				string title = "Some buildings have already been updated";

				string message = "";
				if(doorBuildings.Length > 1)
				{
					message += "Doors for buildings " + doorBuildings.Remove(doorBuildings.LastIndexOf(','), 1) + "have already been updated";
					if(windowBuildings.Length > 1)
						message += " and ";
				}
					
				if(windowBuildings.Length > 1)
				{
					message += "Windows for buildings " + windowBuildings.Remove(windowBuildings.LastIndexOf(','), 1) + " have already been updated";
				}

				message += ". If you run the update it may change the buildings already updated. If you update, check those buildings for proper openings.";

				updateBuildings = EditorUtility.DisplayDialog(title, message, "Update", "Don't Update");
			}

			if(updateBuildings)
			{
				BCValidator.UpdateAllWindowsFromOlderVersion();
				BCValidator.UpdateAllDoorsFromOlderVersion();
				EditorUtility.DisplayDialog("Update Complete", "Building's windows and doors have been updated", "Okay");
			}
		}
	}
}
