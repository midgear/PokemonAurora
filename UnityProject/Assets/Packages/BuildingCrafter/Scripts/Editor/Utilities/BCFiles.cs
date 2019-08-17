using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System;

namespace BuildingCrafter
{
	public static class BCFiles
	{
		public static string BCBaseFolder = @"/Packages/BuildingCrafter";
		public static string BCAssetFolder = BCBaseFolder + @"Assets";

		// RESOURCES
		public static string BCResources = BCAssetFolder + @"/Resources";
		public static string BuildingStyles = BCResources + @"/BuildingStyles";
		public static string RoomStyles = BCResources + @"/RoomStyles";

	//	public static string ImportedMeshes = BCResources + @"/ImportedMeshes";
		public static string ImportedTextures = BCResources + @"/ImportedTextures";
		public static string ImportedMaterials = BCResources + @"/ImportedMaterials";


		// RESOUCES - PREFABS
		public static string Prefabs = BCResources + @"/Prefabs";
		public static string StairPrefabs = Prefabs + @"/Stairs";
		public static string DoorPrefabs = Prefabs + @"/Doors";

		// SAVED BUILDINGS
		public static string SavedBuildings = BCAssetFolder + @"/SavedBuildings";

		// SCRIPTS
		public static string Scripts = BCAssetFolder + @"/Scripts";
		public static string BuildingScripts = Scripts + @"/BuildingScripts";

		// ATLASES
//		public static string Atlases = BCAssetFolder + @"/Atlases";


		/// <summary>
		/// Creates the proper layout for the Building Crafter Assets
		/// </summary>
		public static void CreateBuildingCrafterAssetDirectories()
		{
			string baseFolder = Application.dataPath;

//			Debug.Log(baseFolder);

			// Creates the director for the base apps
			if(Directory.Exists(baseFolder + BCFiles.BCAssetFolder) == false) Directory.CreateDirectory(baseFolder + BCFiles.BCAssetFolder);
			if(Directory.Exists(baseFolder + BCFiles.BuildingStyles) == false) Directory.CreateDirectory(baseFolder + BCFiles.BuildingStyles);

			if(Directory.Exists(baseFolder + BCFiles.RoomStyles) == false) Directory.CreateDirectory(baseFolder + BCFiles.RoomStyles);
			if(Directory.Exists(baseFolder + BCFiles.SavedBuildings) == false) Directory.CreateDirectory(baseFolder + BCFiles.SavedBuildings);

			if(Directory.Exists(baseFolder + BCFiles.ImportedTextures) == false) Directory.CreateDirectory(baseFolder + BCFiles.ImportedTextures);
			if(Directory.Exists(baseFolder + BCFiles.ImportedMaterials) == false) Directory.CreateDirectory(baseFolder + BCFiles.ImportedMaterials);

			if(Directory.Exists(baseFolder + BCFiles.Prefabs) == false) Directory.CreateDirectory(baseFolder + BCFiles.Prefabs);
			if(Directory.Exists(baseFolder + BCFiles.BCResources) == false) Directory.CreateDirectory(baseFolder + BCFiles.BCResources);

			if(Directory.Exists(baseFolder + BCFiles.StairPrefabs) == false) Directory.CreateDirectory(baseFolder + BCFiles.StairPrefabs);
			if(Directory.Exists(baseFolder + BCFiles.DoorPrefabs) == false) Directory.CreateDirectory(baseFolder + BCFiles.DoorPrefabs);

			// Removed because Atlases go into subfolders beside their BuildingStyles
//			if(Directory.Exists(baseFolder + BCFiles.Atlases) == false) Directory.CreateDirectory(baseFolder + BCFiles.Atlases);

			// Removed because the scripts are not being transfer
	//		if(Directory.Exists(BCFiles.Scripts) == false) Directory.CreateDirectory(BCFiles.Scripts);
	//		if(Directory.Exists(BCFiles.BuildingScripts) == false) Directory.CreateDirectory(BCFiles.BuildingScripts);
	//		if(Directory.Exists(BCFiles.FurnitureScripts) == false) Directory.CreateDirectory(BCFiles.FurnitureScripts);
		}
	}
}
