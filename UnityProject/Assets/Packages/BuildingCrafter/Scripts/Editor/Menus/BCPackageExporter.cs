//using UnityEngine;
//using System.Collections;
//using UnityEditor;
//using System.IO;
//using System.Collections.Generic;
//using Newtonsoft.Json;
//using System.Linq;
//using System.Reflection;
//
//namespace BuildingCrafter
//{
//
//	public class BCPackageExporter : Editor 
//	{
//
//		// Disabled because all of this moving assets around is scary and will possibly make users mad
//	//	[MenuItem("BuildingCrafter/Export Building Crafter Buildings #%d")]
//		public static void ExportBuildingCrafterBuildings()
//		{
//			int startTick = System.Environment.TickCount;
//
//			BCFiles.CreateBuildingCrafterAssetDirectories();
//
//			// In order to export, we are going to quickly move assets around into their own spots, add as 
//
//			// =============== MOVING BUILDING STYLES TO THE CORRECT DIRECTORY ==============================
//			// TODO - Move the files back after saving the asset database
//
//			// We should actually look through all the saved files in the saved Buildings to find which generic importers we need
//			string[] searchFolder = new string[1] { BCFiles.SavedBuildings };
//
//			string[] jsonBpPaths = AssetDatabase.FindAssets("", searchFolder);
//
//			List<string> buildingStyles = new List<string>();
//
//			foreach(var bpPath in jsonBpPaths)
//			{
//				string jsonData = File.ReadAllText(AssetDatabase.GUIDToAssetPath(bpPath));
//
//				SerializedBlueprint serializedBp = JsonConvert.DeserializeObject<SerializedBlueprint>(jsonData);
//
//				if(AssetDatabase.FindAssets(serializedBp.BuildingStyle).Length > 0) // First check to see if this asset is in the system, if it is then also check more
//				{
//					if(buildingStyles.Contains(serializedBp.BuildingStyle) == false) // If the list of building styles does not include this, then add it
//					{
//						buildingStyles.Add(serializedBp.BuildingStyle);
//						Debug.Log("Added " + serializedBp.BuildingStyle);
//					}
//						
//				}
//			}
//
//			foreach(var style in buildingStyles)
//			{
//				// NOTE: Due to the searching, similarly named styles (BarOfficeBuildingStyle) or (OfficeBuildingStyle), so there may be some redunant movement. That is fine.
//
//				string[] guids = AssetDatabase.FindAssets(style);
//				for(int i = 0; i < guids.Length; i++)
//				{
//					string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]); // Finds the first copy of this
//					
//					if(assetPath.Contains(".prefab") == false) // Ensures any non prefab files files don't get moved
//					{
//						Debug.LogError("This is not a prefab file: " + assetPath + ". You have incorrect json in your blueprints under BuildingStyle string");
//						continue;
//					}
//					
//					GameObject stylePrefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
//					
//					MoveAsset(stylePrefab, BCFiles.BuildingStyles);
//				}
//			}
//
//			// ========================= Now move all the materials that are associated with those Furniture Styles ================
//
//			searchFolder = new string[1] { BCFiles.BuildingStyles };
//			string[] buildingStyleGuids = AssetDatabase.FindAssets("", searchFolder);
//
//			foreach(var stylePath in buildingStyleGuids)
//			{
//				GameObject prefab = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(stylePath), typeof(GameObject)) as GameObject;
//
//				BuildingStyle buildingStyle = prefab.GetComponent<BuildingStyle>();
//
//				// Creates all the proper folders for this information
//				// TODO - Transfer this to the BC files static method
//				if(AssetDatabase.IsValidFolder(BCFiles.ImportedMaterials + "/CeilingMaterials") == false)
//					AssetDatabase.CreateFolder(BCFiles.ImportedMaterials, "CeilingMaterials");
//
//				if(AssetDatabase.IsValidFolder(BCFiles.ImportedMaterials + "/FloorMaterials") == false)
//					AssetDatabase.CreateFolder(BCFiles.ImportedMaterials, "FloorMaterials");
//
//				if(AssetDatabase.IsValidFolder(BCFiles.ImportedMaterials + "/WallMaterials") == false)
//					AssetDatabase.CreateFolder(BCFiles.ImportedMaterials, "WallMaterials");
//
//				if(AssetDatabase.IsValidFolder(BCFiles.ImportedMaterials + "/FacadeMaterials") == false)
//					AssetDatabase.CreateFolder(BCFiles.ImportedMaterials, "FacadeMaterials");
//
//				if(AssetDatabase.IsValidFolder(BCFiles.ImportedMaterials + "/OpeningMaterials") == false)
//					AssetDatabase.CreateFolder(BCFiles.ImportedMaterials, "OpeningMaterials");
//
//				if(AssetDatabase.IsValidFolder(BCFiles.ImportedMaterials + "/Rooftops") == false)
//					AssetDatabase.CreateFolder(BCFiles.ImportedMaterials, "Rooftops");
//
//				string ceilingPath = BCFiles.ImportedMaterials + "/CeilingMaterials/";
//				string floorPath = BCFiles.ImportedMaterials + "/FloorMaterials/";
//				string wallPath = BCFiles.ImportedMaterials + "/WallMaterials/";
//				string facadePath = BCFiles.ImportedMaterials + "/FacadeMaterials/";
//				string openingPath = BCFiles.ImportedMaterials + "/OpeningMaterials/";
//				string roofPath = BCFiles.ImportedMaterials + "/Rooftops/";
//
//
//				FieldInfo[] fieldInfos = buildingStyle.GetType().GetFields();
//				for(int i = 0; i < fieldInfos.Length; i++)
//				{
//
//					// Moves all the room materials by reflection
//					if(fieldInfos[i].FieldType == typeof(RoomMaterials))
//					{
//						RoomMaterials roomMat = fieldInfos[i].GetValue(buildingStyle) as RoomMaterials;
//						MoveRoomMaterial(roomMat, ceilingPath, floorPath, wallPath);
//					}
//					if(fieldInfos[i].FieldType == typeof(GenerateRoomItems))
//					{
//	//					GenerateRoomItems filler = fieldInfos[i].GetValue(buildingStyle) as GenerateRoomItems;
//	//					MoveAsset(filler, BCFiles.RoomFillers + "/");
//					}
//				}
//
//				// Move the facades
//				MoveAsset(buildingStyle.FancySidings, facadePath);
//				MoveAsset(buildingStyle.PlainSiding, facadePath);
//
//				MoveAsset(buildingStyle.DoorWindowFrames, openingPath);
//				MoveAsset(buildingStyle.Window, openingPath);
//
//				MoveAsset(buildingStyle.Rooftop, roofPath);
//
//				MoveAsset(buildingStyle.StandardDoor, BCFiles.DoorPrefabs);
//				MoveAsset(buildingStyle.HeavyDoor, BCFiles.DoorPrefabs);
//
//				MoveAsset(buildingStyle.TwoByFourStairs, BCFiles.StairPrefabs);
//				MoveAsset(buildingStyle.StairsToRoof, BCFiles.StairPrefabs);
//
//			}
//
//			// ========================= MOVE ALL THE FILLER'S PREFABS INTO THE CORRECT SPOTS =====================
//
//	//		searchFolder = new string[1] { BCFiles.RoomFillers };
//			searchFolder = new string[0];
//			string[] fillerGuids = AssetDatabase.FindAssets("", searchFolder);
//
//			foreach(var guid in fillerGuids)
//			{
//				string path = AssetDatabase.GUIDToAssetPath(guid);
//
//				GameObject fillerObj = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
//
//				GenerateRoomItems filler = fillerObj.GetComponent<GenerateRoomItems>();
//
//				if(filler == null)
//					continue;
//
//				foreach(var furniture in filler.RoomFiller.AllFurnitureGenInfo)
//				{
//					foreach(var singleFurniture in furniture.FurnitureTypes)
//					{
//						if(singleFurniture == null)
//						{
//							Debug.Log("In the generator " + filler.name + " there is a missing Prefab");
//							continue;
//						}
//
//						FurnitureSetup furnSetup = singleFurniture.GetComponent<FurnitureSetup>();
//						if(furnSetup == null)
//							continue;
//
//	//					string furnMovePath = BCFiles.Furniture + "/" + furnSetup.FurnitureLayout.FurnitureRoomType.ToString();
//	//					MoveAsset(singleFurniture, furnMovePath);
//					}
//				}
//
//			}
//
//
//			// ========================= MOVE ALL THE MESHES INTO THE CORRECT PLACES =============================
//
//			searchFolder = new string[1] { BCFiles.Prefabs };
//			string[] prefabGUIDS = AssetDatabase.FindAssets("", searchFolder);
//
//			foreach(var guid in prefabGUIDS)
//			{
//				string path = AssetDatabase.GUIDToAssetPath(guid);
//
//				GameObject prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
//
//				if(prefab == null)
//					continue;
//
//				// Have to loop through all children to do things to each child
//				List<GameObject> prefabAndChildren = BCUtils.GetChildren(prefab);
//	//			prefabAndChildren.Add(prefab);
//	//			FurnitureSetup furnSetup = prefab.GetComponent<FurnitureSetup>();
//
//				// Move all the mesh FBX objects
//				for(int i = 0; i < prefabAndChildren.Count; i++)
//				{
//	//				MeshFilter meshFilter = prefabAndChildren[i].GetComponent<MeshFilter>();
//					MeshRenderer meshRenderer = prefabAndChildren[i].GetComponent<MeshRenderer>();
//
//	//				string additionalPath = "";
//	//				if(furnSetup != null)
//	//					additionalPath = "/" + furnSetup.FurnitureLayout.FurnitureRoomType.ToString();
//
//	//				if(meshFilter != null)
//	//					MoveAsset(meshFilter.sharedMesh, BCFiles.ImportedMeshes + additionalPath);
//						
//					if(meshRenderer != null)
//					{
//	//					for(int j = 0; j < meshRenderer.sharedMaterials.Length; j++)
//	//					{
//	//						MoveAsset(meshRenderer.sharedMaterials[j], BCFiles.FurnitureMaterials + additionalPath);
//	//					}
//					}
//						
//				}
//
//				// TODO - Move the mesh filters into sub folders
//
//
//			}
//
//			
//			// ========================= MOVE ALL THE TEXTURES INTO THE PACKAGE ================
//
//			searchFolder = new string[1] { BCFiles.ImportedMaterials };
//			string[] importedMaterialsGuids = AssetDatabase.FindAssets("", searchFolder);
//			
//			foreach(var guid in importedMaterialsGuids)
//			{
//				string path = AssetDatabase.GUIDToAssetPath(guid);
//				
//				Material mat = AssetDatabase.LoadAssetAtPath(path, typeof(Material)) as Material;
//				
//				if(mat == null)
//					continue;
//				
//				Texture[] textures = GetAllShaderTextures(mat);
//				
//				for(int i = 0; i < textures.Length; i++)
//				{
//					MoveAsset(textures[i], BCFiles.ImportedTextures);
//				}
//				
//			}
//
//			// ========================= CREATE MESHES TO EXPORT WITH THE SELECTION =======================
//
//
//
//			List<string> allAssetPaths = new List<string>();
//
//			// Adds the proper BuildingCrafter Assets
//			searchFolder = new string[1] { "Assets/BuildingCrafterAssets" };
//			string[] assetGuids = AssetDatabase.FindAssets("", searchFolder);
//
//			for(int i = 0; i < assetGuids.Length; i++)
//			{
//				allAssetPaths.Add(AssetDatabase.GUIDToAssetPath(assetGuids[i]));
//			}
//
//			// Adds all the files for the Building Crafter
//			searchFolder = new string[1] { "Assets/BuildingCrafter"};
//			string[] dataModelGuids = AssetDatabase.FindAssets("", searchFolder);
//
//			for(int i = 0; i < dataModelGuids.Length; i++)
//			{
//				allAssetPaths.Add(AssetDatabase.GUIDToAssetPath(dataModelGuids[i]));
//			}
//
//			AssetDatabase.ExportPackage(allAssetPaths.ToArray<string>(), "Assets/ExportedBuildings.unitypackage");
//
//			AssetDatabase.Refresh();
//
//			Debug.Log((System.Environment.TickCount - startTick) / 1000f + " seconds taken to save assets");
//			// TODO - Test the below for speed
//			Resources.UnloadUnusedAssets();
//
//
//		}
//
//		/// <summary>
//		/// Strips the full path of a file
//		/// </summary>
//		/// <returns>The file name.</returns>
//		/// <param name="path">Path.</param>
//		private static string GetFileName(string path)
//		{
//			string fileName = path.ToString();
//			fileName = fileName.Remove(0, fileName.LastIndexOf('/') + 1);
//
//			return fileName;
//		}
//
//		public static void MoveAsset(Object obj, string newFolderPath)
//		{
//			if(obj == null)
//				return;
//
//			string path = newFolderPath;
//
//			if(path[path.Length - 1] != '/')
//				path += "/";
//
//			// Now to test the path, we need to test without the trailing slash
//
//			string testPath = path.Remove(path.Length - 1);
//
//			if(AssetDatabase.IsValidFolder(testPath) == false)
//			{
//				Debug.LogError("This folder does not exist " + testPath);
//				return;
//			}
//
//			string materialPath =  AssetDatabase.GetAssetPath(obj);
//			string materialFileName = GetFileName(materialPath);
//			
//			if(AssetDatabase.ValidateMoveAsset(materialPath, path + materialFileName) == "")
//			{
//				AssetDatabase.MoveAsset(materialPath, path + materialFileName);
//			}
//		}
//
//		public static void MoveAsset(Object[] objs, string newFolderPath)
//		{
//			for(int i = 0; i < objs.Length; i++)
//				MoveAsset(objs[i], newFolderPath);
//		}
//
//		public static void MoveRoomMaterial(RoomMaterials roomMaterial, string ceilingPath, string floorPath, string wallPath)
//		{
//			MoveAsset(roomMaterial.CeilingMaterial, ceilingPath);
//			MoveAsset(roomMaterial.FloorMaterial, floorPath);
//			MoveAsset(roomMaterial.WallMaterial, wallPath);
//		}
//
//		/// <summary>
//		/// Grabs all textures from a both standard Physically Based Shaders
//		/// </summary>
//		/// <returns>The all texture.</returns>
//		/// <param name="material">Material.</param>
//		public static Texture[] GetAllShaderTextures(Material material)
//		{
//			List<Texture> textures = new List<Texture>();
//
//			if(material.shader.name == "Standard" || material.shader.name == "Standard (Specular setup)")
//			{
//
//				Texture mainTex = material.GetTexture("_MainTex");
//				if(mainTex != null)
//					textures.Add(mainTex);
//
//				if(material.shader.name == "Standard")
//				{
//					Texture metallicGloss = material.GetTexture("_MetallicGlossMap");
//					if(metallicGloss != null)
//						textures.Add(metallicGloss);
//				}
//
//				if(material.shader.name == "Standard (Specular setup)")
//				{
//					Texture specGloss = material.GetTexture("_SpecGlossMap");
//					if(specGloss != null)
//						textures.Add(specGloss);
//				}
//
//				Texture bumpMap = material.GetTexture("_BumpMap");
//				if(bumpMap != null)
//					textures.Add(bumpMap);
//				
//				Texture parallax = material.GetTexture("_ParallaxMap");
//				if(parallax != null)
//					textures.Add(parallax);
//				
//				Texture occlusionMap = material.GetTexture("_OcclusionMap");
//				if(occlusionMap != null)
//					textures.Add(occlusionMap);
//
//				Texture emissionMap = material.GetTexture("_EmissionMap");
//				if(emissionMap != null)
//					textures.Add(emissionMap);
//			
//				Texture detailMask = material.GetTexture("_DetailMask");
//				if(detailMask != null)
//					textures.Add(detailMask);
//
//				Texture detailAlbedoMap = material.GetTexture("_DetailAlbedoMap");
//				if(detailAlbedoMap != null)
//					textures.Add(detailAlbedoMap);
//
//				Texture detailedNormalMap = material.GetTexture("_DetailNormalMap");
//				if(detailedNormalMap != null)
//					textures.Add(detailedNormalMap);
//			}
//			else
//				Debug.LogError("WARNING: " + material.name + " is not a physically based shader, may not export to package correctly");
//
//			return textures.ToArray<Texture>();
//		}
//	}
//}